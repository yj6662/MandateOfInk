using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 진(도면) 투사체 — 직선 비행, 적 명중 시 상극 배율을 적용해 피해를 준다.
    // 시전자(SpellCaster)가 Init으로 데이터를 주입한다.
    public sealed class SpellProjectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private Element _element;
        private ElementRelationTableSO _relationTable;
        private GameObject _hitVfxPrefab;
        private float _lifeRemaining;

        public void Init(float speed, float damage, Element element,
            ElementRelationTableSO relationTable, GameObject hitVfxPrefab, float lifetime)
        {
            _speed = speed;
            _damage = damage;
            _element = element;
            _relationTable = relationTable;
            _hitVfxPrefab = hitVfxPrefab;
            _lifeRemaining = lifetime;
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);
            _lifeRemaining -= Time.deltaTime;
            if (_lifeRemaining <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return; // 다른 트리거(투사체 등)는 무시
            if (other.GetComponentInParent<CharacterController>() != null) return; // 시전자(플레이어) 무시

            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                float multiplier = 1f;
                if (_relationTable != null && enemy.Definition != null)
                    multiplier = _relationTable.GetMultiplier(_element, enemy.Definition.Element);
                Debug.Log($"[Spell] {_element} -> {enemy.Definition?.Element} 상성 배율 {multiplier:F2}");
                enemy.TakeDamage(_damage * multiplier);
            }

            // 땅·벽·적 무엇이든 닿는 즉시 소멸 — 잔류 오브젝트를 남기지 않는다(최적화)
            if (_hitVfxPrefab != null)
            {
                var vfx = Instantiate(_hitVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f); // [가정] VFX 잔류 상한 — 파티클 자체 소멸과 별개의 안전장치
            }
            Destroy(gameObject);
        }
    }
}
