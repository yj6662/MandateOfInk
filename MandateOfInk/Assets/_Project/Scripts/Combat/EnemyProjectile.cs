using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 적 투사체 — 직선 비행, 플레이어 명중 시 피해. 무엇이든 닿으면 즉시 소멸(잔류 금지).
    // 속성을 실어 나른다 — 방어 진과의 상극 판정(받아치기/차단/막 파괴)은 SpellShield가 한다.
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private float _speed;
        private float _damage;
        private float _lifeRemaining;
        private Element _element;
        private EnemyHealth _owner; // 받아치기 성공 시 포이즈 반격 대상
        private float _slowMultiplier = 1f; // 목 속박탄: 1 미만이면 명중 시 감속
        private float _slowSeconds;
        private float _pullDistance;        // 수 끌물결: 0 초과면 명중 시 시전자 쪽으로 끌어당김
        private float _pullSeconds;

        public void Init(float speed, float damage, float lifetime,
            Element element = Element.Metal, EnemyHealth owner = null)
        {
            _speed = speed;
            _damage = damage;
            _lifeRemaining = lifetime;
            _element = element;
            _owner = owner;
        }

        // 아키타입 탑재물 — 목(감속) / 수(끌어당김)
        public void SetBindPayload(float slowMultiplier, float slowSeconds)
        {
            _slowMultiplier = slowMultiplier;
            _slowSeconds = slowSeconds;
        }

        public void SetPullPayload(float distance, float seconds)
        {
            _pullDistance = distance;
            _pullSeconds = seconds;
        }

        private void Update()
        {
            transform.position += transform.forward * (_speed * Time.deltaTime);
            _lifeRemaining -= Time.deltaTime;
            if (_lifeRemaining <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            // 플레이어 방어 진 — 상극 3단 판정은 방어 진의 몫.
            // 정반대 속성이면 막히지 않고(false) 그대로 뚫고 지나간다.
            var shield = other.GetComponentInParent<SpellShield>();
            if (shield != null)
            {
                if (shield.ReceiveProjectile(_element, _owner, transform.position, _damage))
                    Destroy(gameObject);
                return;
            }
            if (other.isTrigger) return;                                  // 다른 트리거 무시
            if (other.GetComponentInParent<EnemyHealth>() != null) return; // 아군(적) 무시

            var player = other.GetComponentInParent<PlayerHealth>();
            if (player == null && other.GetComponent<CharacterController>() != null)
                player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(_damage);
                // 아키타입 탑재물 발동 — 목: 감속 / 수: 투사체 진행 반대(=시전자) 쪽으로 끌어당김
                if (_slowMultiplier < 1f)
                    PlayerStatus.GetOrAdd(player.gameObject).ApplySlow(_slowMultiplier, _slowSeconds);
                if (_pullDistance > 0f)
                    PlayerStatus.GetOrAdd(player.gameObject).ApplyPull(-transform.forward, _pullDistance, _pullSeconds);
            }

            Destroy(gameObject); // 땅·벽·플레이어 무엇이든 접촉 즉시 소멸
        }
    }
}
