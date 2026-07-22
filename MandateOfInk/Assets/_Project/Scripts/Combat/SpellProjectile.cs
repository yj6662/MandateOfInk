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
        private Polarity _polarity = Polarity.Yang;
        private bool _installOnly;
        private int _pierceLeft = 1;
        private int _chainJumpsLeft;
        private HashSet<EnemyHealth> _alreadyHit; // 관통·연쇄가 같은 적을 두 번 때리지 않게 (연쇄 계보가 공유)
        private float _armDelay = 0.06f; // 스폰 직후 충돌 유예 — 붓끝·시전자 근처 벽 오탐 방지 [가정]
        private GameObject _explosionPrefab; // 명중·충돌 시 그 지점에 재생할 폭발 문양(Fly Explosion 분해본)
        private GameObject _explosionBottomPrefab; // 폭발과 함께 명중 지점에 각인될 바닥 문양(Bottom)
        private Color _explosionTint = Color.white;
        private float _explosionDiameter = 1.5f;

        // 폭발 문양 배선 — 명중·벽 충돌 시 폭발(Explosion) + 바닥 문양(Bottom)이 그 지점에 함께 재생된다.
        public void SetExplosionPattern(GameObject explosionPrefab, GameObject bottomPrefab, Color tint, float diameter)
        {
            _explosionPrefab = explosionPrefab;
            _explosionBottomPrefab = bottomPrefab;
            _explosionTint = tint;
            _explosionDiameter = diameter;
        }

        // 폭발 이펙트 재생 — 폭발 문양 + 바닥 전통 문양이 함께 터진다(배선 없으면 팽창 구 폴백).
        private void PlayImpact(Vector3 at)
        {
            if (_explosionPrefab != null)
            {
                SpellVisuals.SpawnPatternExplosion(_explosionPrefab, at, _explosionDiameter, _explosionTint);
                // 명중 지점 바닥에 전통 문양 각인 — 폭발과 함께 터지며 잠깐 남았다 사라진다
                if (_explosionBottomPrefab != null)
                    SpellVisuals.SpawnGroundStamp(_explosionBottomPrefab, at, _explosionDiameter * 1.3f, _explosionTint);
            }
            else
                SpellVisuals.SpawnBurst(at, _elementColor, 0.9f);
        }

        public void Init(float speed, float damage, Element element,
            ElementRelationTableSO relationTable, float lifetime, Color elementColor,
            FinalModifier modifier = FinalModifier.None, FinalModifierConfigSO config = null,
            bool installOnly = false, HashSet<EnemyHealth> visited = null, int chainJumpsLeft = -1,
            CombatConfigSO combatConfig = null, Polarity polarity = Polarity.Yang)
        {
            _combatConfig = combatConfig;
            _polarity = polarity;
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
            if (_armDelay > 0f) _armDelay -= Time.deltaTime;
            if (_lifeRemaining <= 0f) Destroy(gameObject); // 수명만료(허공)는 폭발 없이 조용히 소멸
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_armDelay > 0f) return;                                                    // 스폰 직후 유예 — 붓끝·근처 벽 오탐 무시
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
                    PlayImpact(transform.position); // 관통 중 각 적중 지점에도 폭발
                    return;
                }
            }

            PlayImpact(transform.position); // 명중·벽 충돌 지점에 폭발 문양
            Destroy(gameObject);
        }

        private void HitEnemy(EnemyHealth enemy)
        {
            // ㅁ격발 설치 — 피해 대신 표식만 심는다 (C4식 부착)
            if (_installOnly && _config != null)
            {
                EnemyStatus.GetOrAdd(enemy).InstallMark(_element, _config.MarkSeconds,
                    _config.TriggerPoiseFraction, _config.TriggerBurstDiameter, _elementColor, _config.MaxActiveMarks);
                return;
            }

            float multiplier = 1f;
            if (_relationTable != null && enemy.Definition != null)
                multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
            float damage = _damage * multiplier;

            // 양(陽) 진만 표식을 격발한다 — 보너스는 그로기 대폭(작도설계안 §5)
            var status = enemy.GetComponent<EnemyStatus>();
            if (status != null && _polarity == Polarity.Yang) status.TryDetonateMark(_combatConfig);
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
                FinalModifier.Chain, _config, false, _alreadyHit, _chainJumpsLeft - 1, _combatConfig, _polarity);
            Debug.Log($"[Spell] 연쇄 점프 -> {next.name} (남은 점프 {_chainJumpsLeft - 1})");
        }
    }
}
