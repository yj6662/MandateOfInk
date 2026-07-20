using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 시전기 — 작도 인식 채널(EC_DiagramDrawn)을 구독해 도면의 분류·범위에 따라 진을 발동한다.
    //   공격+단일(ㅏ) = 투사체 / 공격+영역(ㅗ) = 팽창 파동
    //   방어(ㅓ) = 전방 막 / 방어(ㅜ) = 광역 돔
    //   상합 설치(ㅁ받침) = 종성 인식(D12) 후 구현 — 현재 스텁
    // 표현: 반투명 프리미티브 + 속성 색 + 인식 글자 텍스트 (VFX 에셋 미사용, 수묵 연출은 M1).
    public sealed class SpellCaster : MonoBehaviour
    {
        [Header("데이터")]
        [SerializeField] private ElementRelationTableSO _relationTable;
        [SerializeField] private DiagramEventChannelSO _diagramDrawn;
        [SerializeField] private ElementPaletteSO _palette;

        [Header("[가정] 투사체 (공격·단일)")]
        [SerializeField] private float _projectileSpeed = 25f;
        [SerializeField] private float _projectileLifetime = 5f;
        [SerializeField] private float _projectileDiameter = 0.5f;

        [Header("[가정] 광역 파동 (공격·영역)")]
        [SerializeField] private float _areaRadius = 4.5f;
        [SerializeField] private float _areaExpandSeconds = 0.45f;

        [Header("[가정] 방어 진")]
        [SerializeField] private Vector3 _shieldWallSize = new Vector3(2.4f, 1.8f, 0.15f);
        [SerializeField] private float _shieldWallDistance = 1.3f;
        [SerializeField] private float _shieldDomeDiameter = 4.6f;

        [Header("[가정] 표현")]
        [SerializeField, Range(0f, 1f)] private float _objectAlpha = 0.35f;
        [SerializeField] private Color _letterColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        private Transform _playerRoot; // 방어 진·광역의 기준 (몸)

        private void OnEnable()
        {
            if (_diagramDrawn != null) _diagramDrawn.OnRaised += Cast;
        }

        private void OnDisable()
        {
            if (_diagramDrawn != null) _diagramDrawn.OnRaised -= Cast;
        }

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

        private void Cast(DiagramCastRequest request)
        {
            var diagram = request.Diagram;
            if (diagram == null) return;

            Color color = _palette != null ? _palette.GetColor(diagram.Element) : Color.white;
            color.a = _objectAlpha * (request.IsWeak ? 0.6f : 1f); // 약발동은 더 흐릿하게

            switch (diagram.Category)
            {
                case DiagramCategory.Attack:
                    if (diagram.Scope == Scope.Area) CastAreaBlast(diagram, request, color);
                    else CastProjectile(diagram, request, color);
                    break;
                case DiagramCategory.DefenseControl:
                    CastShield(diagram, request, color);
                    break;
                case DiagramCategory.TriggerInstall:
                    // 종성 인식(D12) 도입 전에는 발동 경로가 없다 — 스텁
                    Debug.Log($"[Spell] 「{diagram.Letter}」 상합 설치는 종성 인식(D12) 후 구현 예정");
                    break;
            }
        }

        private float GetDamage(SpellDiagramSO diagram, DiagramCastRequest request)
        {
            foreach (var effect in diagram.Effects)
                if (effect is DamageEffect dmg) return dmg.Damage * request.PowerMultiplier;
            return 0f;
        }

        private float GetShieldDuration(SpellDiagramSO diagram)
        {
            foreach (var effect in diagram.Effects)
                if (effect is ShieldEffect shield) return shield.Duration;
            return 5f; // [가정]
        }

        // 공격·단일(ㅏ): 반투명 구 투사체 + 글자
        private void CastProjectile(SpellDiagramSO diagram, DiagramCastRequest request, Color color)
        {
            float scale = request.IsWeak ? 0.6f : 1f;
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, color,
                Vector3.one * (_projectileDiameter * scale), keepColliderAsTrigger: true);
            go.name = $"Projectile_{diagram.Letter}";
            go.transform.SetPositionAndRotation(transform.position + transform.forward * 0.8f, transform.rotation);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            go.AddComponent<SpellProjectile>().Init(_projectileSpeed, GetDamage(diagram, request),
                diagram.Element, _relationTable, _projectileLifetime, color);
            SpellVisuals.AttachLetter(go.transform, diagram.Letter, 0.4f * scale, _letterColor);
            Log(diagram, request, "투사체");
        }

        // 공격·영역(ㅗ): 몸 중심 팽창 파동 + 글자
        private void CastAreaBlast(SpellDiagramSO diagram, DiagramCastRequest request, Color color)
        {
            var origin = PlayerRoot != null ? PlayerRoot.position + Vector3.up * 1f : transform.position;
            float radius = _areaRadius * (request.IsWeak ? 0.7f : 1f);
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, color, Vector3.one * 0.3f, keepColliderAsTrigger: true);
            go.name = $"AreaBlast_{diagram.Letter}";
            go.transform.position = origin;
            var mat = go.GetComponent<MeshRenderer>().material;
            go.AddComponent<SpellAreaBlast>().Init(radius, _areaExpandSeconds,
                GetDamage(diagram, request), diagram.Element, _relationTable, mat);
            SpellVisuals.AttachLetter(go.transform, diagram.Letter, 0.6f, _letterColor);
            Log(diagram, request, "광역 파동");
        }

        // 방어(ㅓ=전방 막 / ㅜ=광역 돔): 플레이어를 따라다니는 반투명 방벽 + 글자
        private void CastShield(SpellDiagramSO diagram, DiagramCastRequest request, Color color)
        {
            if (PlayerRoot == null) return;
            float duration = GetShieldDuration(diagram) * request.PowerMultiplier; // 약발동은 지속 절반 [가정]

            GameObject go;
            if (diagram.Scope == Scope.Area)
            {
                go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, color,
                    Vector3.one * _shieldDomeDiameter, keepColliderAsTrigger: true);
                go.transform.SetParent(PlayerRoot, false);
                go.transform.localPosition = Vector3.up * 1f;
            }
            else
            {
                go = SpellVisuals.CreateTranslucent(PrimitiveType.Cube, color, _shieldWallSize, keepColliderAsTrigger: true);
                go.transform.SetParent(PlayerRoot, false);
                go.transform.localPosition = Vector3.up * 1.1f + Vector3.forward * _shieldWallDistance;
            }
            go.name = $"Shield_{diagram.Letter}";
            var mat = go.GetComponent<MeshRenderer>().material;
            go.AddComponent<SpellShield>().Init(duration, mat);
            SpellVisuals.AttachLetter(go.transform, diagram.Letter, 0.5f, _letterColor);
            Log(diagram, request, diagram.Scope == Scope.Area ? "광역 돔" : "전방 막");
        }

        private static void Log(SpellDiagramSO diagram, DiagramCastRequest request, string kind)
        {
            Debug.Log($"[Spell] 「{diagram.Letter}」 {kind} — {diagram.Element}/{diagram.Scope}{(request.IsWeak ? " (약발동)" : "")}");
        }
    }
}
