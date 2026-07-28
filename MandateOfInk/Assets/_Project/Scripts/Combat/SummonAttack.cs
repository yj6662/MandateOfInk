using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 공격 소환수(ㅗ+ㄱ) — 설치된 자리에 고정, 오행별로 다른 동사를 실행한다(사용자 결정 2026-07-23).
    //   목=투사체 연사 / 화=제자리 화염 지속 피해장 / 토=굵고 느린 광역 고피해 타격(예비 동작 있음) /
    //   금=긴 사거리 저격 관통탄 / 수=주기적 인접 전체 연쇄 파동.
    // 개수 제한 없음(먹 비용으로만 억제).
    public sealed class SummonAttack : MonoBehaviour
    {
        private SummonDefinitionSO _def;
        private ElementRelationTableSO _relationTable;
        private Color _color;
        private float _range;
        private float _nextActionAt;
        private float _despawnAt;

        // 토(SlowSlam) 전용 — 예비 동작 중인지
        private bool _slamTelegraphing;
        private float _slamTelegraphUntil;
        private Vector3 _slamTargetPos;

        public void Init(SummonDefinitionSO def, ElementRelationTableSO relationTable, Color color, float range)
        {
            _def = def;
            _relationTable = relationTable;
            _color = color;
            _range = range;
            _despawnAt = Time.time + def.LifetimeSeconds;
            _nextActionAt = Time.time + def.AttackInterval * 0.5f; // 소환 직후 살짝의 유예
        }

        private void Update()
        {
            if (Time.time >= _despawnAt) { Destroy(gameObject); return; }

            // 토(SlowSlam) 예비 동작 진행 중이면 그것만 처리
            if (_slamTelegraphing)
            {
                if (Time.time >= _slamTelegraphUntil) ResolveSlowSlam();
                return;
            }

            if (Time.time < _nextActionAt) return;

            EnemyHealth nearest = FindNearestEnemy();
            switch (_def.Verb)
            {
                case SummonDefinitionSO.AttackVerb.ProjectileVolley:
                    if (nearest == null) return; // 적 없으면 대기(간격 유지)
                    FireProjectile(nearest);
                    _nextActionAt = Time.time + _def.AttackInterval;
                    break;

                case SummonDefinitionSO.AttackVerb.FireZone:
                    PulseFireZone();
                    _nextActionAt = Time.time + _def.AttackInterval;
                    break;

                case SummonDefinitionSO.AttackVerb.SlowSlam:
                    if (nearest == null) return;
                    _slamTargetPos = nearest.transform.position;
                    _slamTelegraphing = true;
                    _slamTelegraphUntil = Time.time + _def.SlamTelegraphSeconds;
                    SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.3f,
                        new Color(_color.r, _color.g, _color.b, 0.35f), _def.SlamRadius * 0.6f, _def.SlamTelegraphSeconds);
                    break;

                case SummonDefinitionSO.AttackVerb.SniperPierce:
                    if (nearest == null) return;
                    FireSniper(nearest);
                    _nextActionAt = Time.time + _def.AttackInterval;
                    break;

                case SummonDefinitionSO.AttackVerb.ChainPulse:
                    PulseChain();
                    _nextActionAt = Time.time + _def.AttackInterval;
                    break;
            }
        }

        private EnemyHealth FindNearestEnemy()
        {
            EnemyHealth nearest = null;
            float bestSqr = _range * _range;
            foreach (var candidate in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate.IsDead) continue;
                float sqr = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; nearest = candidate; }
            }
            return nearest;
        }

        // 목 — 근처 적에게 투사체 연사
        private void FireProjectile(EnemyHealth target)
        {
            Vector3 origin = transform.position + Vector3.up * 0.6f;
            Vector3 aim = (target.transform.position + Vector3.up * 1f) - origin;

            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, _color, Vector3.one * 0.3f, keepColliderAsTrigger: true);
            go.name = $"SummonBolt_{_def.Element}";
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(aim));
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            go.AddComponent<SpellProjectile>().Init(_def.ProjectileSpeed, _def.AttackDamage, _def.Element,
                _relationTable, 4f, _color, combatConfig: null, polarity: Polarity.Yang);
        }

        // 화 — 제자리 화염 장판. 주기마다 범위 내 적 전원에게 틱 피해(장을 새로 깔지 않고 직접 판정).
        private void PulseFireZone()
        {
            bool anyHit = false;
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead) continue;
                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr > _def.FireZoneRadius * _def.FireZoneRadius) continue;
                anyHit = true;
                DealSummonDamage(enemy);
            }
            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.1f,
                new Color(_color.r, _color.g, _color.b, 0.5f), _def.FireZoneRadius * 2f, _def.AttackInterval * 0.8f);
            if (!anyHit) return; // 로그 스팸 방지용 분기(피해는 이미 없음)
        }

        // 토 — 예비 동작 종료 후 굵고 느린 광역 타격을 그 지점에 떨어뜨린다
        private void ResolveSlowSlam()
        {
            _slamTelegraphing = false;
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead) continue;
                float sqr = (enemy.transform.position - _slamTargetPos).sqrMagnitude;
                if (sqr > _def.SlamRadius * _def.SlamRadius) continue;
                DealSummonDamage(enemy);
            }
            SpellVisuals.SpawnBurst(_slamTargetPos + Vector3.up * 0.1f, _color, _def.SlamRadius * 2f, 0.3f);
            _nextActionAt = Time.time + _def.AttackInterval;
        }

        // 관통 개수만 지정하면 되는 임시 config — 이 소환수 생애 동안 하나만 만들어 재사용(매 발사마다 새로 안 만듦)
        private FinalModifierConfigSO _sniperConfig;

        // 금 — 긴 사거리 저격, 관통(Pierce)으로 여러 적을 뚫는다
        private void FireSniper(EnemyHealth target)
        {
            if (_sniperConfig == null)
            {
                _sniperConfig = ScriptableObject.CreateInstance<FinalModifierConfigSO>();
                _sniperConfig.PierceMaxTargets = _def.SniperPierceCount;
            }

            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 aim = (target.transform.position + Vector3.up * 1f) - origin;

            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, _color, Vector3.one * 0.22f, keepColliderAsTrigger: true);
            go.name = $"SummonSniper_{_def.Element}";
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(aim));
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            go.AddComponent<SpellProjectile>().Init(_def.SniperProjectileSpeed, _def.AttackDamage, _def.Element,
                _relationTable, 3f, _color, FinalModifier.Pierce, _sniperConfig, combatConfig: null, polarity: Polarity.Yang);
        }

        private void OnDestroy()
        {
            if (_sniperConfig != null) Destroy(_sniperConfig);
        }

        // 수 — 주기마다 인접 전체 적에게 동시 파동(연쇄처럼 한 번에 퍼진다)
        private void PulseChain()
        {
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead) continue;
                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr > _def.ChainPulseRadius * _def.ChainPulseRadius) continue;
                DealSummonDamage(enemy);
            }
            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.2f, _color, _def.ChainPulseRadius * 2f, 0.35f);
        }

        // 공통 피해 처리(상성 배율 + 소환수 자체는 그로기/버프 배율에 영향받지 않음 — 소환수는 플레이어 화력과 별개)
        private void DealSummonDamage(EnemyHealth enemy)
        {
            float multiplier = 1f;
            if (_relationTable != null && enemy.Definition != null)
                multiplier = _relationTable.GetMultiplier(_def.Element, enemy.Definition.Element);
            enemy.TakeDamage(_def.AttackDamage * multiplier);
        }
    }
}
