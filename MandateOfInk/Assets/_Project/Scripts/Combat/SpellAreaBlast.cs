using System.Collections.Generic;
using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 광역 공격 진(ㅗ) — 시전자 중심으로 팽창하는 반투명 파동. 닿은 적마다 1회 피해(상극 배율 적용).
    public sealed class SpellAreaBlast : MonoBehaviour
    {
        private readonly HashSet<EnemyHealth> _alreadyHit = new HashSet<EnemyHealth>();
        private float _maxDiameter;
        private float _expandSeconds;
        private float _elapsed;
        private float _damage;
        private Element _element;
        private ElementRelationTableSO _relationTable;
        private Material _material;
        private Color _baseColor;
        private FinalModifier _modifier = FinalModifier.None;
        private FinalModifierConfigSO _config;
        private CombatConfigSO _combatConfig;
        private Polarity _polarity = Polarity.Yang;
        private bool _installOnly;

        public void Init(float radius, float expandSeconds, float damage, Element element,
            ElementRelationTableSO relationTable, Material material,
            FinalModifier modifier = FinalModifier.None, FinalModifierConfigSO config = null,
            bool installOnly = false, CombatConfigSO combatConfig = null, Polarity polarity = Polarity.Yang)
        {
            _modifier = modifier;
            _config = config;
            _installOnly = installOnly;
            _combatConfig = combatConfig;
            _polarity = polarity;
            _maxDiameter = radius * 2f;
            _expandSeconds = Mathf.Max(expandSeconds, 0.1f);
            _damage = damage;
            _element = element;
            _relationTable = relationTable;
            _material = material;
            _baseColor = material.color;

            var rb = gameObject.AddComponent<Rigidbody>(); // 트리거 이벤트용
            rb.isKinematic = true;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _expandSeconds);
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, _maxDiameter, Mathf.Sqrt(t));
            var c = _baseColor;
            c.a *= 1f - 0.7f * t; // 퍼질수록 옅어짐
            _material.color = c;
            if (_elapsed >= _expandSeconds + 0.15f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy == null || _alreadyHit.Contains(enemy)) return;
            _alreadyHit.Add(enemy);

            // ㅁ격발 설치(광역판) — 범위 내 모든 적에게 표식만 심는다
            if (_installOnly && _config != null)
            {
                EnemyStatus.GetOrAdd(enemy).InstallMark(_element, _config.MarkSeconds,
                    _config.TriggerPoiseFraction, _config.TriggerBurstDiameter, _baseColor, _config.MaxActiveMarks);
                return;
            }

            float multiplier = 1f;
            if (_relationTable != null && enemy.Definition != null)
                multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
            float damage = _damage * multiplier;

            var status = enemy.GetComponent<EnemyStatus>();
            if (status != null && _polarity == Polarity.Yang) status.TryDetonateMark(_combatConfig);
            if (status != null) damage *= status.GroggyDamageMultiplier;
            damage *= PlayerBuffLookup.DamageMultiplier; // 소환수 공격력 버프

            Debug.Log($"[Spell] 광역 {_element} -> {enemy.Definition?.Element} 배율 {multiplier:F2}");
            enemy.TakeDamage(damage);
            if (_combatConfig != null)
                EnemyStatus.GetOrAdd(enemy).AddPoise(
                    damage * _combatConfig.SpellPoiseFraction * PlayerBuffLookup.PoiseDamageMultiplier, _combatConfig);
            SpellVisuals.SpawnBurst(enemy.transform.position + Vector3.up * 1f, _baseColor, 0.9f);

            if (_config == null) return;
            switch (_modifier)
            {
                case FinalModifier.Bind:
                {
                    // 단일속박과 같은 오행별 방해 방식(사용자 결정 2026-07-24)
                    var bindCfg = _config.GetBindConfig(_element);
                    var bindTarget = EnemyStatus.GetOrAdd(enemy);
                    bindTarget.ApplyBind(bindCfg.MoveMultiplier, bindCfg.Seconds, _baseColor,
                        bindCfg.TelegraphMultiplier, bindCfg.AimJitterDegrees);
                    if (bindCfg.SustainTickFraction > 0f)
                        bindTarget.ApplyDot(damage * bindCfg.SustainTickFraction,
                            bindCfg.SustainTickInterval, bindCfg.SustainSeconds, _baseColor);
                    break;
                }
                case FinalModifier.Sustain:
                {
                    // 지속 오행별 부가효과(사용자 결정 2026-07-24) — 목=표준/화=강해짐/토=주변 확산/금=포이즈/수=먹 환급
                    var sustainCfg = _config.GetSustainConfig(_element);
                    var sustainTarget = EnemyStatus.GetOrAdd(enemy);
                    sustainTarget.ApplyDot(damage * sustainCfg.TickFraction, sustainCfg.TickInterval, sustainCfg.Seconds,
                        _baseColor, sustainCfg.TickFractionGrowthPerTick, sustainCfg.PoiseTickFraction,
                        _combatConfig, sustainCfg.InkRefundPerTick);
                    if (sustainCfg.SpreadRadius > 0f)
                        sustainTarget.ApplySustainSpread(sustainCfg.SpreadRadius, sustainCfg.SpreadInterval,
                            sustainCfg.TickFraction, sustainCfg.TickInterval, sustainCfg.Seconds, damage, _baseColor, _element);
                    break;
                }
                case FinalModifier.Pierce:
                {
                    // 광역판 관통 — 이미 범위 내 전원을 때리므로 "대상 수"는 무의미, 오행별 부가효과만 적용
                    var pierceCfg = _config.GetPierceConfig(_element);
                    var pierceTarget = EnemyStatus.GetOrAdd(enemy);
                    if (pierceCfg.BindMoveMultiplier > 0f)
                        pierceTarget.ApplyBind(pierceCfg.BindMoveMultiplier, pierceCfg.BindSeconds, _baseColor);
                    if (pierceCfg.SustainTickFraction > 0f)
                        pierceTarget.ApplyDot(damage * pierceCfg.SustainTickFraction,
                            pierceCfg.SustainTickInterval, pierceCfg.SustainSeconds, _baseColor);
                    if (pierceCfg.ExtraPoiseFraction > 0f && _combatConfig != null)
                        pierceTarget.AddPoise(damage * pierceCfg.ExtraPoiseFraction, _combatConfig);
                    // PullStrength(수=서로 끌어당김)는 투사체 전용 — 광역은 이미 한 지점에서 퍼지는 판정이라 의미가 옅어 생략
                    break;
                }
                case FinalModifier.Chain:
                {
                    // 광역판 연쇄 — "몇 번째로 맞았는지"를 점프 인덱스로 삼아 "퍼짐"을 표현(사용자 결정 2026-07-24)
                    var chainCfg = _config.GetChainConfig(_element);
                    int jumpIndex = Mathf.Max(0, _alreadyHit.Count - 1);
                    var chainTarget = EnemyStatus.GetOrAdd(enemy);
                    if (chainCfg.BindMoveMultiplier > 0f)
                        chainTarget.ApplyBind(chainCfg.BindMoveMultiplier, chainCfg.BindSeconds, _baseColor);
                    if (chainCfg.SustainTickFraction > 0f)
                    {
                        float seconds = chainCfg.SustainSeconds + chainCfg.SustainSecondsGrowthPerJump * jumpIndex;
                        chainTarget.ApplyDot(damage * chainCfg.SustainTickFraction, chainCfg.SustainTickInterval, seconds, _baseColor);
                    }
                    if (chainCfg.ExtraPoiseFraction > 0f && _combatConfig != null)
                    {
                        float fraction = chainCfg.ExtraPoiseFraction + chainCfg.ExtraPoiseGrowthPerJump * jumpIndex;
                        chainTarget.AddPoise(damage * fraction, _combatConfig);
                    }
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
