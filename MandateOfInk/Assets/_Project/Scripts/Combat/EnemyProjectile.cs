using UnityEngine;

namespace MandateOfInk.Combat
{
    // 적 투사체 — 직선 비행, 플레이어 명중 시 피해. 무엇이든 닿으면 즉시 소멸(잔류 금지).
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private float _lifeRemaining;

        public void Init(float speed, float damage, float lifetime)
        {
            _speed = speed;
            _damage = damage;
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
            if (other.isTrigger) return;                                  // 다른 트리거 무시
            if (other.GetComponentInParent<EnemyHealth>() != null) return; // 아군(적) 무시

            var player = other.GetComponentInParent<PlayerHealth>();
            if (player == null && other.GetComponent<CharacterController>() != null)
                player = other.GetComponent<PlayerHealth>();
            if (player != null) player.TakeDamage(_damage);

            Destroy(gameObject); // 땅·벽·플레이어 무엇이든 접촉 즉시 소멸
        }
    }
}
