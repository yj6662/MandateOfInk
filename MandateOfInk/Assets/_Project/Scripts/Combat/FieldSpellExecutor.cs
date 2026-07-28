using MandateOfInk.Core.Events;
using MandateOfInk.Data;
using MandateOfInk.Vehicle;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 필드/퍼즐 술식 실행 — 음성(ㅓ/ㅜ)+받침 = Field 분류(사용자 결정 2026-07-27).
    // 구현 4종: 뭄(돌다리 설치)·굼(도약 기둥)·언(낙사 방어 수막)·넌(차량 화력 가속).
    // 나머지 41자는 예약(미구현) — 시전하면 로그+옅은 먹 퍼프만 낸다(완전 불발 금지 규칙과는 별개인 미구현 안내).
    public sealed class FieldSpellExecutor : MonoBehaviour
    {
        [SerializeField] private FieldSpellConfigSO _config;
        [Tooltip("굼 도약 — 글루(PlayerLeapGlue)가 구독해 수직 속도를 준다")]
        [SerializeField] private FloatEventChannelSO _playerLeap;

        private Transform _playerRoot;

        private Transform PlayerRoot
        {
            get
            {
                if (_playerRoot == null)
                {
                    var cc = FindFirstObjectByType<CharacterController>();
                    if (cc != null) _playerRoot = cc.transform;
                }
                return _playerRoot;
            }
        }

        // 시전 기준 수평 전방 — 카메라가 보는 방향(수직 성분 제거)
        private Vector3 CastForward
        {
            get
            {
                var cam = Camera.main;
                Vector3 fwd = cam != null ? cam.transform.forward : (PlayerRoot != null ? PlayerRoot.forward : Vector3.forward);
                fwd.y = 0f;
                return fwd.sqrMagnitude > 0.001f ? fwd.normalized : Vector3.forward;
            }
        }

        public void Cast(SpellDiagramSO diagram, float powerMultiplier, Color color)
        {
            if (_config == null) { Debug.LogWarning("[Field] FieldSpellConfig 미배선"); return; }
            if (PlayerRoot == null) return;

            // 구현 4종은 (오행, 받침) 조합으로 식별 — 뭄=토·설치 / 굼=목·설치 / 언=수·지속 / 넌=화·지속
            if (diagram.Element == Element.Earth && diagram.Modifier == FinalModifier.Trigger && diagram.Scope == Scope.Area)
                SpawnBridge(diagram, color);
            else if (diagram.Element == Element.Wood && diagram.Modifier == FinalModifier.Trigger && diagram.Scope == Scope.Area)
                LeapPillar(diagram, color);
            else if (diagram.Element == Element.Water && diagram.Modifier == FinalModifier.Sustain && diagram.Scope == Scope.Single)
                FallGuard();
            else if (diagram.Element == Element.Fire && diagram.Modifier == FinalModifier.Sustain && diagram.Scope == Scope.Single)
                BoostVehicle();
            else
            {
                Debug.Log($"[Field] 「{diagram.Letter}」 필드 예약(미구현) — {diagram.Element}/{diagram.Modifier}/{diagram.Scope}");
                SpellVisuals.SpawnBurst(PlayerRoot.position + Vector3.up * 1.2f, color, 0.8f, 0.4f);
            }
        }

        // 설치형 공통 — (오행, 받침)이 맞고 사거리 안인 가장 가까운 앵커를 찾는다(없으면 null=실패)
        private FieldSpellAnchor FindAnchor(Element element, FinalModifier modifier)
        {
            FieldSpellAnchor best = null;
            float bestSqr = float.MaxValue;
            foreach (var a in FindObjectsByType<FieldSpellAnchor>(FindObjectsSortMode.None))
            {
                if (!a.Accepts(element, modifier, PlayerRoot.position)) continue;
                float sqr = (a.transform.position - PlayerRoot.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = a; }
            }
            return best;
        }

        // 뭄 — 지정 앵커 위치에 돌다리를 놓는다(레벨 저작 위치 — 틈에 정확히 걸침).
        // 판정(BoxCollider)은 즉시 유효, 비주얼은 먹물 -> 실체 실체화(사용자 결정 2026-07-27).
        private void SpawnBridge(SpellDiagramSO diagram, Color color)
        {
            var anchor = FindAnchor(Element.Earth, FinalModifier.Trigger);
            if (anchor == null)
            {
                // 지정 위치 밖 — 먹물이 맺혔다가 흩어진다(먹 일부 환급)
                Vector3 fwd = CastForward;
                Vector3 failPos = PlayerRoot.position + fwd * (_config.BridgeLength * 0.5f + 0.5f)
                    + Vector3.down * (_config.BridgeThickness * 0.5f + 0.05f);
                SpawnFailure(diagram, failPos, Quaternion.LookRotation(fwd), _config.BridgePrefab,
                    _config.BridgeLength,
                    new Vector3(_config.BridgeWidth, _config.BridgeThickness, _config.BridgeLength), false, false);
                return;
            }

            var root = new GameObject("Field_돌다리");
            root.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(_config.BridgeWidth, _config.BridgeThickness, _config.BridgeLength);

            SpawnInstallVisual(root, _config.BridgePrefab, _config.BridgeLength,
                new Vector3(_config.BridgeWidth, _config.BridgeThickness, _config.BridgeLength),
                new Color(0.45f, 0.42f, 0.38f, 1f)); // 돌빛 폴백 [가정]

            root.AddComponent<InkMaterializer>().Play(_config.MaterializeSeconds);
            if (_config.BridgeSeconds > 0f) Destroy(root, _config.BridgeSeconds);
            SpellVisuals.SpawnBurst(root.transform.position + Vector3.up * 0.5f, color, 1.6f, 0.5f);
            Debug.Log($"[Field] 뭄 — 앵커 「{anchor.name}」에 돌다리 설치 ({_config.BridgeSeconds}s)");
        }

        // 굼 — 지정 앵커에서 나무 기둥이 솟으며 위로 쏘아 올린다(기둥은 연출 전용 — 판정 없음)
        private void LeapPillar(SpellDiagramSO diagram, Color color)
        {
            var anchor = FindAnchor(Element.Wood, FinalModifier.Trigger);
            if (anchor == null)
            {
                SpawnFailure(diagram, PlayerRoot.position + Vector3.down * 0.2f,
                    Quaternion.Euler(0f, PlayerRoot.eulerAngles.y, 0f), _config.PillarPrefab,
                    _config.PillarHeight, new Vector3(0.7f, _config.PillarHeight, 0.7f), true, true);
                return;
            }

            if (_playerLeap != null) _playerLeap.Raise(_config.LeapVelocity);
            else Debug.LogWarning("[Field] EC_PlayerLeap 채널 미배선 — 도약 무시");

            var root = new GameObject("Field_도약기둥");
            root.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);

            SpawnInstallVisual(root, _config.PillarPrefab, _config.PillarHeight,
                new Vector3(0.7f, _config.PillarHeight, 0.7f),
                new Color(0.42f, 0.33f, 0.2f, 1f), primitiveIsCylinder: true, sizeAxisIsHeight: true);

            root.AddComponent<InkMaterializer>().Play(_config.MaterializeSeconds);
            Destroy(root, _config.PillarSeconds);
            SpellVisuals.SpawnBurst(root.transform.position, color, 1.4f, 0.5f);
            Debug.Log($"[Field] 굼 — 앵커 「{anchor.name}」에서 도약 (초속 {_config.LeapVelocity}m/s)");
        }

        // 실패 연출 — 먹물 형상만 잠깐 맺혔다가 흩어져 사라진다. 판정 없음, 먹 일부 환급(사용자 결정).
        private void SpawnFailure(SpellDiagramSO diagram, Vector3 position, Quaternion rotation,
            GameObject prefab, float targetSize, Vector3 primitiveScale,
            bool primitiveIsCylinder, bool sizeAxisIsHeight)
        {
            var root = new GameObject("Field_실패먹물");
            root.transform.SetPositionAndRotation(position, rotation);
            SpawnInstallVisual(root, prefab, targetSize, primitiveScale,
                new Color(0.1f, 0.09f, 0.08f, 1f), primitiveIsCylinder, sizeAxisIsHeight);
            root.AddComponent<InkMaterializer>().PlayFailure(_config.FailHoldSeconds, _config.FailDissolveSeconds);
            Destroy(root, _config.FailHoldSeconds + _config.FailDissolveSeconds + 0.3f);

            float refund = diagram.InkCost * _config.FailInkRefundFraction;
            PlayerBuffLookup.RefundInk(refund);
            SpellVisuals.SpawnBurst(root.transform.position + Vector3.up * 0.5f,
                new Color(0.12f, 0.11f, 0.1f, 0.8f), 1.2f, 0.5f);
            Debug.Log($"[Field] 「{diagram.Letter}」 지정 위치 밖 — 먹이 흩어짐 (먹 {refund:F0} 환급)");
        }

        // 설치물 비주얼 스폰 — 프리팹이 배선돼 있으면 균등 스케일로 목표 치수에 맞추고(Meshy 축 보정
        // 회전이 있어도 균등 스케일이라 안전), 없으면 프리미티브 폴백(수묵 톤 머티리얼).
        private void SpawnInstallVisual(GameObject parent, GameObject prefab, float targetSize,
            Vector3 primitiveScale, Color fallbackColor,
            bool primitiveIsCylinder = false, bool sizeAxisIsHeight = false)
        {
            if (prefab != null)
            {
                var visual = Instantiate(prefab, parent.transform);
                var rends = visual.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    foreach (var r in rends) b.Encapsulate(r.bounds);
                    float unit = sizeAxisIsHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
                    float scale = unit > 0.001f ? targetSize / unit : 1f;
                    visual.transform.localScale *= scale;
                    // 스케일 반영 후 바운드 중심을 부모(판정) 중심에 정렬
                    Bounds b2 = rends[0].bounds;
                    foreach (var r in rends) b2.Encapsulate(r.bounds);
                    visual.transform.position += parent.transform.position - b2.center;
                }
                return;
            }

            var go = GameObject.CreatePrimitive(primitiveIsCylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = "Visual";
            Destroy(go.GetComponent<Collider>()); // 판정은 부모(또는 없음)가 담당
            go.transform.SetParent(parent.transform, false);
            go.transform.localScale = primitiveIsCylinder
                ? new Vector3(primitiveScale.x, primitiveScale.y * 0.5f, primitiveScale.z) // 실린더 높이=y*2
                : primitiveScale;
            var mat = new Material(Shader.Find("MandateOfInk/Temp/ToonLit"));
            mat.SetColor("_BaseColor", fallbackColor);
            mat.SetFloat("_Saturation", 0f); // 환경 취급 — 수묵 톤
            go.GetComponent<Renderer>().sharedMaterial = mat;
            RuntimeMaterialCleaner.Track(go, mat);
        }

        // 언 — 자신에게 수막(낙사 무효) 지속
        private void FallGuard()
        {
            var guard = PlayerRoot.GetComponent<PlayerFallDamage>();
            if (guard == null) { Debug.LogWarning("[Field] PlayerFallDamage 미부착 — 언 무시"); return; }
            guard.SetFallGuard(_config.FallGuardSeconds);
            SpellVisuals.SpawnBurst(PlayerRoot.position + Vector3.up * 1f,
                new Color(0.2f, 0.45f, 0.95f, 0.8f), 1.4f, 0.6f);
        }

        // 넌 — 사거리 내 가장 가까운 마석 자동차에 화력 가속
        private void BoostVehicle()
        {
            VehicleController best = null;
            float bestSqr = _config.BoostCastRange * _config.BoostCastRange;
            foreach (var v in FindObjectsByType<VehicleController>(FindObjectsSortMode.None))
            {
                float sqr = (v.transform.position - PlayerRoot.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = v; }
            }
            if (best == null)
            {
                Debug.Log($"[Field] 넌 — 사거리 {_config.BoostCastRange}m 안에 마석 자동차 없음");
                return;
            }
            best.ApplyBoost(_config.BoostMultiplier, _config.BoostSeconds);
            SpellVisuals.SpawnBurst(best.transform.position + Vector3.up * 1.5f,
                new Color(0.95f, 0.35f, 0.15f, 0.8f), 1.8f, 0.6f);
        }
    }
}
