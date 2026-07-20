using MandateOfInk.Data;
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
        }

        // 적 투사체가 닿았을 때 (EnemyProjectile이 호출) — 상극 3단 판정
        public void ReceiveProjectile(Element attackElement, EnemyHealth attacker, Vector3 hitPosition)
        {
            float advantage = _relationTable != null
                ? _relationTable.GetMultiplier(_element, attackElement) : 1f;

            // 우세: 받아치기 성공 — 공격자를 크게 휘청이게 한다
            if (_combatConfig != null && advantage >= _combatConfig.ParryAdvantageThreshold)
            {
                SpellVisuals.SpawnBurst(hitPosition, new Color(1f, 0.9f, 0.4f, 0.7f), 1.6f, 0.3f); // 금빛 쳐내기
                if (attacker != null && attacker.Definition != null)
                {
                    float poise = attacker.Definition.MaxPoise * _combatConfig.ParryPoiseFraction;
                    EnemyStatus.GetOrAdd(attacker).AddPoise(poise, _combatConfig);
                }
                Debug.Log($"[Parry] 받아치기 성공! {_element} 극 {attackElement} (배율 {advantage:F2})");
                return;
            }

            // 열세: 막이 깨진다
            if (_combatConfig != null && advantage <= _combatConfig.ShieldBreakThreshold)
            {
                SpellVisuals.SpawnBurst(transform.position, _baseColor, 2.2f, 0.35f);
                Debug.Log($"[Parry] 막 파괴 — {attackElement} 극 {_element} (배율 {advantage:F2})");
                Destroy(gameObject);
                return;
            }

            // 중립: 그냥 차단
            SpellVisuals.SpawnBurst(hitPosition, _baseColor, 0.7f, 0.25f);
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
