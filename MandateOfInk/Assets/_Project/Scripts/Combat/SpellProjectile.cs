using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 단일 공격 진(ㅏ) 투사체 — 직선 비행, 적 명중 시 상극 배율 피해. 무엇이든 닿으면 즉시 소멸.
    public sealed class SpellProjectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private Element _element;
        private ElementRelationTableSO _relationTable;
        private float _lifeRemaining;
        private Color _elementColor = Color.white;

        public void Init(float speed, float damage, Element element,
            ElementRelationTableSO relationTable, float lifetime, Color elementColor)
        {
            _speed = speed;
            _damage = damage;
            _element = element;
            _relationTable = relationTable;
            _lifeRemaining = lifetime;
            _elementColor = elementColor;
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
                float multiplier = 1f;
                if (_relationTable != null && enemy.Definition != null)
                    multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
                Debug.Log($"[Spell] {_element} -> {enemy.Definition?.Element} 상성 배율 {multiplier:F2}");
                enemy.TakeDamage(_damage * multiplier);
            }

            SpellVisuals.SpawnBurst(transform.position, _elementColor, 0.9f); // 명중 파열(속성 색)
            Destroy(gameObject);
        }
    }
}
