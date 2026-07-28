using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 낙사 피해 — 착지 순간의 낙하 속도가 임계를 넘으면 피해(수치는 FieldSpellConfigSO [가정]).
    // 「언」(수+ㅓ+ㄴ) 수막 버프가 걸려 있으면 무효. CharacterController만 읽으므로 StarterAssets 무참조.
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFallDamage : MonoBehaviour
    {
        [SerializeField] private FieldSpellConfigSO _config;

        private CharacterController _controller;
        private PlayerHealth _health;
        private bool _wasGrounded = true;
        private float _peakFallSpeed;
        private float _guardUntil;

        public bool IsGuarded => Time.time < _guardUntil;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<PlayerHealth>();
        }

        // 「언」 — 수막 낙사 방어 버프
        public void SetFallGuard(float seconds)
        {
            _guardUntil = Time.time + seconds;
            Debug.Log($"[Field] 수막 낙사 방어 {seconds:F0}s");
        }

        private void Update()
        {
            if (_config == null || !_controller.enabled) return;

            bool grounded = _controller.isGrounded;
            float verticalSpeed = _controller.velocity.y;

            if (!grounded && verticalSpeed < -_peakFallSpeed)
                _peakFallSpeed = -verticalSpeed; // 공중에서 최대 낙하 속도 기록

            if (grounded && !_wasGrounded)
            {
                float excess = _peakFallSpeed - _config.SafeFallSpeed;
                if (excess > 0f)
                {
                    if (IsGuarded)
                    {
                        Debug.Log($"[Field] 수막이 낙하를 받아냄 ({_peakFallSpeed:F1}m/s)");
                        SpellVisuals.SpawnBurst(transform.position, new Color(0.2f, 0.45f, 0.95f, 0.8f), 1.6f, 0.5f);
                    }
                    else if (_health != null)
                    {
                        float damage = excess * _config.DamagePerExcessSpeed;
                        Debug.Log($"[Field] 낙사 피해 {damage:F0} (낙하 {_peakFallSpeed:F1}m/s)");
                        _health.TakeDamage(damage);
                    }
                }
                _peakFallSpeed = 0f;
            }
            _wasGrounded = grounded;
        }
    }
}
