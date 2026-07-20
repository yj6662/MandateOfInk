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
        private bool _installOnly;

        public void Init(float radius, float expandSeconds, float damage, Element element,
            ElementRelationTableSO relationTable, Material material,
            FinalModifier modifier = FinalModifier.None, FinalModifierConfigSO config = null,
            bool installOnly = false)
        {
            _modifier = modifier;
            _config = config;
            _installOnly = installOnly;
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
                EnemyStatus.GetOrAdd(enemy).InstallMark(_element,
                    _config.MarkSeconds, _config.TriggerBonusMultiplier, _config.TriggerBurstDiameter, _baseColor);
                return;
            }

            float multiplier = 1f;
            if (_relationTable != null && enemy.Definition != null)
                multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
            float damage = _damage * multiplier;

            var status = enemy.GetComponent<EnemyStatus>();
            if (status != null && status.TryDetonateMark(damage, out float bonus)) damage += bonus;

            Debug.Log($"[Spell] 광역 {_element} -> {enemy.Definition?.Element} 배율 {multiplier:F2}");
            enemy.TakeDamage(damage);
            SpellVisuals.SpawnBurst(enemy.transform.position + Vector3.up * 1f, _baseColor, 0.9f);

            if (_config == null) return;
            if (_modifier == FinalModifier.Bind)
                EnemyStatus.GetOrAdd(enemy).ApplyBind(_config.BindMoveMultiplier, _config.BindSeconds, _baseColor);
            else if (_modifier == FinalModifier.Sustain)
                EnemyStatus.GetOrAdd(enemy).ApplyDot(_damage * _config.SustainTickFraction,
                    _config.SustainTickInterval, _config.SustainSeconds, _baseColor);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
