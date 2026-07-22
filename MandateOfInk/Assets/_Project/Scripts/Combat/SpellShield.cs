using MandateOfInk.Data;
using MandateOfInk.Spellcraft;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 방어 진(ㅓ=전방 막, ㅜ=광역 돔) — 지속 시간 동안 적 투사체를 받아낸다.
    // 받아치기(사용자 설계): 버튼 타이밍이 아니라 「상극」으로 판정한다.
    //   막 속성이 공격 속성을 극함(우세) = 받아치기 성공 — 무효 + 발사한 적에게 포이즈 대타격
    //   중립 = 그냥 차단 / 공격이 막을 극함(열세) = 막이 깨진다
    // 트리거 콜라이더라 이동을 방해하지 않는다. 임계·수치는 CombatConfigSO.
    public sealed class SpellShield : MonoBehaviour
    {
        private float _duration;
        private float _elapsed;
        private Material _material;
        private Color _baseColor;
        private Element _element;
        private ElementRelationTableSO _relationTable;
        private CombatConfigSO _combatConfig;
        private InkPool _inkPool;         // 저스트 먹 환급 — 첫 성공 시 지연 조회
        private PlayerHealth _player;     // 중립 관통 피해 — 지연 조회
        private float _spawnTime;         // 저스트 창 판정 기준(막 완성 시각)

        public void Init(float duration, Material material,
            Element element = Element.Water, ElementRelationTableSO relationTable = null,
            CombatConfigSO combatConfig = null)
        {
            _duration = Mathf.Max(duration, 0.5f);
            _material = material;
            _baseColor = material.color;
            _element = element;
            _relationTable = relationTable;
            _combatConfig = combatConfig;
            _spawnTime = Time.time;
        }

        // 적 투사체가 닿았을 때 (EnemyProjectile이 호출) — 상극 3단 판정(전투코어루프 §3).
        // 반환 = 막았는가. false면 투사체가 막히지 않고 그대로 통과한다(정반대 속성 = 실패).
        public bool ReceiveProjectile(Element attackElement, EnemyHealth attacker, Vector3 hitPosition, float damage)
        {
            float advantage = _relationTable != null
                ? _relationTable.GetMultiplier(_element, attackElement) : 1f;

            // 상극: 완전 무효 — 깨끗이 받아낸다. 막 완성 직후면 「막 받아치기(저스트)」로 추가 보상.
            if (_combatConfig != null && advantage >= _combatConfig.ParryAdvantageThreshold)
            {
                bool just = Time.time - _spawnTime <= _combatConfig.JustParryWindowSeconds;
                float fraction = just ? _combatConfig.JustParryPoiseFraction : _combatConfig.ParryPoiseFraction;
                SpellVisuals.SpawnBurst(hitPosition, new Color(1f, 0.9f, 0.4f, 0.7f), just ? 2.4f : 1.6f, 0.3f);
                if (attacker != null && attacker.Definition != null)
                    EnemyStatus.GetOrAdd(attacker).AddPoise(attacker.Definition.MaxPoise * fraction, _combatConfig);
                if (just)
                {
                    // 저스트 전용 먹 환급 — 정확+과감한 타이밍의 보상(§5 교전 충전)
                    if (_inkPool == null) _inkPool = FindFirstObjectByType<InkPool>();
                    if (_inkPool != null) _inkPool.Add(_combatConfig.ParryInkRefund);
                }
                Debug.Log($"[Parry] {(just ? "막 받아치기(저스트)!" : "받아치기 성공")} {_element} 극 {attackElement}" +
                    $"{(just ? $" 먹 +{_combatConfig.ParryInkRefund}" : "")}");
                return true;
            }

            // 정반대(적이 강한 속성): 실패 — 막히지 않는다. 투사체가 그대로 뚫고 지나간다.
            if (_combatConfig != null && advantage <= _combatConfig.ShieldBreakThreshold)
            {
                Debug.Log($"[Parry] 실패 — {attackElement}은(는) {_element} 막에 막히지 않는다");
                return false;
            }

            // 상생·무관(중립): 약한 막기 — 피해 일부가 관통하고, 그로기는 작게 오른다.
            SpellVisuals.SpawnBurst(hitPosition, _baseColor, 0.7f, 0.25f);
            if (_combatConfig != null)
            {
                float through = damage * _combatConfig.NeutralBlockDamageThrough;
                if (through > 0f)
                {
                    if (_player == null) _player = FindFirstObjectByType<PlayerHealth>();
                    if (_player != null) _player.TakeDamage(through);
                }
                if (attacker != null && attacker.Definition != null)
                    EnemyStatus.GetOrAdd(attacker).AddPoise(
                        attacker.Definition.MaxPoise * _combatConfig.NeutralBlockPoiseFraction, _combatConfig);
                Debug.Log($"[Parry] 약한 막기 — 피해 {through:F1} 관통 ({_element} vs {attackElement})");
            }
            return true;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            // 만료가 다가올수록 옅어진다 — 남은 시간이 읽히게
            var c = _baseColor;
            c.a *= Mathf.Lerp(1f, 0.25f, t);
            _material.color = c;
            if (_elapsed >= _duration) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
