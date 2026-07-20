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

        public void Init(float radius, float expandSeconds, float damage, Element element,
            ElementRelationTableSO relationTable, Material material)
        {
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

            float multiplier = 1f;
            if (_relationTable != null && enemy.Definition != null)
                multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
            Debug.Log($"[Spell] 광역 {_element} -> {enemy.Definition?.Element} 배율 {multiplier:F2}");
            enemy.TakeDamage(_damage * multiplier);
            SpellVisuals.SpawnBurst(enemy.transform.position + Vector3.up * 1f, _baseColor, 0.9f);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
