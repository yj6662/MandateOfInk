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
        [SerializeField] private FinalModifierConfigSO _modifierConfig; // 종성(받침) 거동 수치
        [SerializeField] private CombatConfigSO _combatConfig;          // 포이즈·받아치기 수치
        [SerializeField] private SpellPatternSetSO _patternSet;         // 오행→문양 이펙트 프리팹 (없으면 구체 폴백)

        [Header("[가정] 투사체 (공격·단일)")]
        [SerializeField] private float _projectileSpeed = 25f;
        [SerializeField] private float _projectileLifetime = 5f;
        [SerializeField] private float _projectileDiameter = 0.5f;
        [Tooltip("발사 시작점 — 붓끝 근처(카메라 로컬). z=앞, y=아래로 살짝, x=오른쪽")]
        [SerializeField] private Vector3 _muzzleLocalOffset = new Vector3(0.25f, -0.15f, 0.9f);

        [Header("[가정] 광역 파동 (공격·영역)")]
        [SerializeField] private float _areaRadius = 4.5f;
        [SerializeField] private float _areaExpandSeconds = 0.45f;
        [Tooltip("바닥 진 문양이 지면에 남는 시간(파동 판정과 별개) — 끊김 방지")]
        [SerializeField] private float _groundPatternSeconds = 3.5f;

        [Header("[가정] 방어 진")]
        [SerializeField] private Vector3 _shieldWallSize = new Vector3(2.4f, 1.8f, 0.15f);
        [SerializeField] private float _shieldWallDistance = 1.6f;
        [SerializeField] private float _shieldDomeDiameter = 4.6f;
        [Tooltip("전방 막을 쌓는 진 층 수 — 여러 겹으로 벽처럼 완성감을 준다")]
        [SerializeField] private int _shieldWallLayers = 4;
        [Tooltip("층 사이 높이 간격")]
        [SerializeField] private float _shieldWallLayerHeight = 0.5f;

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
            if (_modifierConfig == null && diagram.Modifier != FinalModifier.None)
                Debug.LogWarning("[Spell] FinalModifierConfig 미배선 — 받침 거동이 무시됩니다");

            Color color = _palette != null ? _palette.GetColor(diagram.Element) : Color.white;
            if (request.IsWeak)
            {
                // 연한 먹: 색이 바래고(회백 쪽으로) 훨씬 흐릿하게 — 정발동과 한눈에 구분
                color = Color.Lerp(color, new Color(0.78f, 0.78f, 0.74f), 0.55f);
                color.a = _objectAlpha * 0.4f;
            }
            else color.a = _objectAlpha;

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
                    // ㅁ받침: 피해 대신 격발 표식을 심는다 — 표식이 있는 적을 다음 술식으로 때리면 격발
                    if (diagram.Scope == Scope.Area) CastAreaBlast(diagram, request, color, installOnly: true);
                    else CastProjectile(diagram, request, color, installOnly: true);
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

        // 공격·단일(ㅏ): 반투명 구 투사체 + 글자. installOnly면 피해 대신 격발 표식 설치.
        private void CastProjectile(SpellDiagramSO diagram, DiagramCastRequest request, Color color, bool installOnly = false)
        {
            float scale = request.IsWeak ? 0.5f : 1f;
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, color,
                Vector3.one * (_projectileDiameter * scale), keepColliderAsTrigger: true);
            go.name = $"Projectile_{diagram.Letter}";
            // 붓끝 근처에서 발사 — 카메라 로컬 오프셋을 월드로 변환
            Vector3 muzzle = transform.TransformPoint(_muzzleLocalOffset);
            go.transform.SetPositionAndRotation(muzzle, transform.rotation);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var projectile = go.AddComponent<SpellProjectile>();
            projectile.Init(_projectileSpeed, GetDamage(diagram, request),
                diagram.Element, _relationTable, _projectileLifetime, color,
                diagram.Modifier, _modifierConfig, installOnly,
                combatConfig: _combatConfig, polarity: diagram.Polarity);

            // 문양 발사체로 완전 교체 — 판정 구는 숨기고 비행 몸체(분해본 Projectile)만 얹는다(약발동은 색 폴백)
            var projPrefab = !request.IsWeak && _patternSet != null ? _patternSet.GetProjectile(diagram.Element) : null;
            if (projPrefab != null)
            {
                var tint = ElementTint(diagram.Element);
                SpellVisuals.HideRenderer(go);
                SpellVisuals.AttachProjectilePattern(go.transform, projPrefab, _projectileDiameter * 2.4f * scale, tint);
                // 명중·벽 충돌 시 그 지점에 폭발(분해본 Explosion) + 바닥 전통 문양(Bottom)이 함께 터진다
                projectile.SetExplosionPattern(
                    _patternSet.GetExplosion(diagram.Element),
                    _patternSet.GetGroundCircle(diagram.Element),
                    tint, _projectileDiameter * 4f * scale);
            }
            Log(diagram, request, installOnly ? "격발 표식 투사체" : "투사체");
        }

        // 공격·영역(ㅗ): 몸 중심 팽창 파동 + 글자. installOnly면 범위 내 적 전원에 격발 표식 설치.
        private void CastAreaBlast(SpellDiagramSO diagram, DiagramCastRequest request, Color color, bool installOnly = false)
        {
            var origin = PlayerRoot != null ? PlayerRoot.position + Vector3.up * 1f : transform.position;
            float radius = _areaRadius * (request.IsWeak ? 0.7f : 1f);
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, color, Vector3.one * 0.3f, keepColliderAsTrigger: true);
            go.name = $"AreaBlast_{diagram.Letter}";
            go.transform.position = origin;
            var mat = go.GetComponent<MeshRenderer>().material;
            go.AddComponent<SpellAreaBlast>().Init(radius, _areaExpandSeconds,
                GetDamage(diagram, request), diagram.Element, _relationTable, mat,
                diagram.Modifier, _modifierConfig, installOnly, _combatConfig, diagram.Polarity);

            // 바닥 문양 진 — 발밑에 깔린다(팽창 구는 판정만, 문양이 룩 담당)
            var groundPrefab = !request.IsWeak && _patternSet != null ? _patternSet.GetGroundCircle(diagram.Element) : null;
            if (groundPrefab != null)
            {
                SpellVisuals.HideRenderer(go);
                // 바닥 진을 부모(판정 구)에서 떼어내 지면에 남긴다 — 구가 사라져도 진은 자연 소멸
                var fx = SpellVisuals.AttachGroundPattern(go.transform, groundPrefab, radius * 2f, followParent: true, ElementTint(diagram.Element));
                if (fx != null)
                {
                    var groundPos = origin + Vector3.down * 0.9f;
                    fx.transform.SetParent(null, true);
                    fx.transform.position = groundPos;
                    fx.AddComponent<PatternFade>().Init(_groundPatternSeconds);
                }
            }
            Log(diagram, request, installOnly ? "격발 표식 파동" : "광역 파동");
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
                // 시야를 덜 가리게 더 앞·아래로 — 눈앞이 아니라 앞쪽 바닥에서 솟은 벽
                go.transform.localPosition = Vector3.up * 0.7f + Vector3.forward * _shieldWallDistance;
            }
            go.name = $"Shield_{diagram.Letter}";
            var mat = go.GetComponent<MeshRenderer>().material;
            go.AddComponent<SpellShield>().Init(duration, mat, diagram.Element, _relationTable, _combatConfig);

            // 방어 진 문양 — 돔은 발밑 바닥 진, 전방 막은 앞쪽에 여러 층으로 쌓은 진 벽. 페이드로 소멸.
            var shieldPrefab = !request.IsWeak && _patternSet != null ? _patternSet.GetGroundCircle(diagram.Element) : null;
            if (shieldPrefab != null)
            {
                SpellVisuals.HideRenderer(go);
                var tint = ElementTint(diagram.Element);
                if (diagram.Scope == Scope.Area)
                {
                    // 광역 돔: 발밑 바닥 진 한 장
                    var fx = SpellVisuals.AttachGroundPattern(go.transform, shieldPrefab, _shieldDomeDiameter, followParent: true, tint);
                    if (fx != null) { fx.transform.localPosition = Vector3.down * 0.9f; DetachAndFade(fx, duration); }
                }
                else
                {
                    // 전방 막: 앞쪽 지면에서 위로 여러 층 쌓아 벽처럼 — 층마다 조금씩 작아지고 높아진다.
                    // 판정 큐브(벽)는 그대로 서있고, 문양 층이 '앞을 막는 진 벽'으로 완성돼 보인다.
                    for (int layer = 0; layer < _shieldWallLayers; layer++)
                    {
                        float up = _shieldWallLayerHeight * layer;
                        float sizeFalloff = Mathf.Lerp(1f, 0.72f, (float)layer / Mathf.Max(1, _shieldWallLayers - 1));
                        var fx = SpellVisuals.AttachGroundPattern(go.transform, shieldPrefab,
                            _shieldWallSize.x * 1.5f * sizeFalloff, followParent: true, tint);
                        if (fx == null) continue;
                        // X축 90도로 세워 벽면으로, 플레이어 앞쪽에 층층이
                        fx.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                        fx.transform.localPosition = new Vector3(0f, up, _shieldWallDistance);
                        DetachAndFade(fx, duration);
                    }
                }
            }
            Log(diagram, request, diagram.Scope == Scope.Area ? "광역 돔" : "전방 막");
        }

        // 문양을 판정 오브젝트에서 떼어 PlayerRoot 자식으로 옮기고 페이드를 건다.
        // 방어막 판정(부모)이 파괴돼도 문양은 남아 자연 소멸(뚝 끊김 방지), 플레이어를 계속 따라다닌다.
        private void DetachAndFade(GameObject fx, float duration)
        {
            Vector3 worldPos = fx.transform.position;
            Quaternion worldRot = fx.transform.rotation;
            fx.transform.SetParent(PlayerRoot, worldPositionStays: true);
            fx.transform.SetPositionAndRotation(worldPos, worldRot);
            fx.AddComponent<PatternFade>().Init(duration);
        }

        // 문양 틴트용 오방색 — 팔레트 색을 불투명 순색으로(알파·명도 영향 제거). 팔레트 없으면 오방색 기본값.
        private Color ElementTint(Element element)
        {
            if (_palette != null)
            {
                var c = _palette.GetColor(element);
                c.a = 1f;
                return c;
            }
            return element switch
            {
                Element.Wood => new Color(0.25f, 0.85f, 0.45f),  // 청(청록 계열)
                Element.Fire => new Color(0.95f, 0.25f, 0.15f),  // 적
                Element.Earth => new Color(0.95f, 0.8f, 0.2f),   // 황
                Element.Metal => new Color(0.92f, 0.94f, 0.98f), // 백
                Element.Water => new Color(0.2f, 0.45f, 0.95f),  // 짙청
                _ => Color.white,
            };
        }

        private static void Log(SpellDiagramSO diagram, DiagramCastRequest request, string kind)
        {
            Debug.Log($"[Spell] 「{diagram.Letter}」 {kind} — {diagram.Element}/{diagram.Scope}{(request.IsWeak ? " (약발동)" : "")}");
        }
    }
}
