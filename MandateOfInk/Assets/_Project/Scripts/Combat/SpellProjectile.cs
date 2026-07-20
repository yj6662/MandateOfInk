using System.Collections.Generic;
using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 단일 공격 진(ㅏ) 투사체 — 직선 비행, 적 명중 시 상극 배율 피해.
    // 종성(받침) 거동:
    //   ㅅ관통 = 여러 적을 뚫고 직선 진행 / ㅇ연쇄 = 인접 적으로 감쇠 점프
    //   ㄱ속박·ㄴ지속 = 적중 대상에 상태 부여 / ㅁ격발(TriggerInstall 분류) = 피해 대신 표식 설치.
    // 표식이 있는 적을 때리면 격발(보너스 폭발) — 조건은 [가정] 단순 규칙(EnemyStatus 참조).
    public sealed class SpellProjectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private Element _element;
        private ElementRelationTableSO _relationTable;
        private float _lifeRemaining;
        private Color _elementColor = Color.white;
        private FinalModifier _modifier = FinalModifier.None;
        private FinalModifierConfigSO _config;
        private CombatConfigSO _combatConfig;
        private bool _installOnly;
        private int _pierceLeft = 1;
        private int _chainJumpsLeft;
        private HashSet<EnemyHealth> _alreadyHit; // 관통·연쇄가 같은 적을 두 번 때리지 않게 (연쇄 계보가 공유)

        public void Init(float speed, float damage, Element element,
            ElementRelationTableSO relationTable, float lifetime, Color elementColor,
            FinalModifier modifier = FinalModifier.None, FinalModifierConfigSO config = null,
            bool installOnly = false, HashSet<EnemyHealth> visited = null, int chainJumpsLeft = -1,
            CombatConfigSO combatConfig = null)
        {
            _combatConfig = combatConfig;
            _speed = speed;
            _damage = damage;
            _element = element;
            _relationTable = relationTable;
            _lifeRemaining = lifetime;
            _elementColor = elementColor;
            _modifier = modifier;
            _config = config;
            _installOnly = installOnly;
            _alreadyHit = visited ?? new HashSet<EnemyHealth>();
            _pierceLeft = modifier == FinalModifier.Pierce && config != null ? config.PierceMaxTargets : 1;
            _chainJumpsLeft = chainJumpsLeft >= 0 ? chainJumpsLeft
                : (modifier == FinalModifier.Chain && config != null ? config.ChainMaxJumps : 0);
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);
            _lifeRemaining -= Time.deltaTime;
            if (_lifeRemaining <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<SpellShield>() != null) return;                 // 아군 방어막 통과
            if (other.isTrigger) return;                                                  // 다른 트리거 무시
            if (other.GetComponentInParent<CharacterController>() != null) return;        // 시전자 무시

            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                if (_alreadyHit.Contains(enemy)) return; // 이미 관통한 적 — 그냥 지나간다
                _alreadyHit.Add(enemy);
                HitEnemy(enemy);
                // ㅅ관통: 소멸하지 않고 계속 직진
                if (_modifier == FinalModifier.Pierce && --_pierceLeft > 0)
                {
                    SpellVisuals.SpawnBurst(transform.position, _elementColor, 0.5f, 0.2f);
                    return;
                }
            }

            SpellVisuals.SpawnBurst(transform.position, _elementColor, 0.9f); // 명중 파열(속성 색)
            Destroy(gameObject);
        }

        private void HitEnemy(EnemyHealth enemy)
        {
            // ㅁ격발 설치 — 피해 대신 표식만 심는다
            if (_installOnly && _config != null)
            {
                EnemyStatus.GetOrAdd(enemy).InstallMark(_element,
                    _config.MarkSeconds, _config.TriggerBonusMultiplier, _config.TriggerBurstDiameter, _elementColor);
                return;
            }

            float multiplier = 1f;
            if (_relationTable != null && enemy.Definition != null)
                multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
            float damage = _damage * multiplier;

            // 표식이 있으면 격발 — 보너스 피해 합산
            var status = enemy.GetComponent<EnemyStatus>();
            if (status != null && status.TryDetonateMark(damage, out float bonus)) damage += bonus;
            // 그로기 중이면 받는 피해 증가
            if (status != null) damage *= status.GroggyDamageMultiplier;

            Debug.Log($"[Spell] {_element} -> {enemy.Definition?.Element} 상성 배율 {multiplier:F2}");
            enemy.TakeDamage(damage);
            // 술식 적중 = 포이즈 축적
            if (_combatConfig != null)
                EnemyStatus.GetOrAdd(enemy).AddPoise(damage * _combatConfig.SpellPoiseFraction, _combatConfig);

            if (_config == null) return;
            switch (_modifier)
            {
                case FinalModifier.Bind:
                    EnemyStatus.GetOrAdd(enemy).ApplyBind(_config.BindMoveMultiplier, _config.BindSeconds, _elementColor);
                    break;
                case FinalModifier.Sustain:
                    EnemyStatus.GetOrAdd(enemy).ApplyDot(_damage * _config.SustainTickFraction,
                        _config.SustainTickInterval, _config.SustainSeconds, _elementColor);
                    break;
                case FinalModifier.Chain:
                    TryChain(enemy);
                    break;
            }
        }

        // ㅇ연쇄: 가장 가까운 미적중 적에게 감쇠된 투사체를 새로 쏜다 (계보가 방문 집합 공유)
        private void TryChain(EnemyHealth from)
        {
            if (_chainJumpsLeft <= 0) return;
            EnemyHealth next = null;
            float bestSqr = _config.ChainRadius * _config.ChainRadius;
            foreach (var candidate in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (candidate == null || _alreadyHit.Contains(candidate)) continue;
                float sqr = (candidate.transform.position - from.transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; next = candidate; }
            }
            if (next == null) return;

            Vector3 origin = from.transform.position + Vector3.up * 1.2f;
            Vector3 aim = (next.transform.position + Vector3.up * 1f) - origin;
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, _elementColor,
                transform.localScale * 0.85f, keepColliderAsTrigger: true);
            go.name = $"{name}_연쇄";
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(aim));
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            go.AddComponent<SpellProjectile>().Init(_speed, _damage * _config.ChainDamageFalloff,
                _element, _relationTable, 3f, _elementColor,
                FinalModifier.Chain, _config, false, _alreadyHit, _chainJumpsLeft - 1, _combatConfig);
            Debug.Log($"[Spell] 연쇄 점프 -> {next.name} (남은 점프 {_chainJumpsLeft - 1})");
        }
    }
}
