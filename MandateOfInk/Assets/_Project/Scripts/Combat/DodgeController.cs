using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 회피 — Left Shift 짧게 탭: 이동 방향으로 무적 대시. 길게 누르면 기존 질주(StarterAssets)가 동작.
    // CharacterController.Move를 추가 호출하는 방식이라 컨트롤러를 직접 참조하지 않는다.
    [RequireComponent(typeof(CharacterController))]
    public sealed class DodgeController : MonoBehaviour
    {
        [SerializeField] private CombatConfigSO _config;

        private CharacterController _controller;
        private PlayerHealth _health;
        private float _keyDownTime = -1f;
        private float _dodgeRemaining;
        private float _cooldownRemaining;
        private Vector3 _dodgeDirection;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (_config == null) return;
            _cooldownRemaining -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.LeftShift)) _keyDownTime = Time.unscaledTime;
            if (Input.GetKeyUp(KeyCode.LeftShift) && _keyDownTime >= 0f)
            {
                bool tap = Time.unscaledTime - _keyDownTime <= _config.DodgeTapThreshold;
                _keyDownTime = -1f;
                if (tap && _cooldownRemaining <= 0f) StartDodge();
            }

            if (_dodgeRemaining > 0f)
            {
                _dodgeRemaining -= Time.deltaTime;
                _controller.Move(_dodgeDirection * (_config.DodgeSpeed * Time.deltaTime));
            }
        }

        private void StartDodge()
        {
            // 이동 입력 방향(없으면 뒤로 스텝) — 몸 기준
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            _dodgeDirection = input.sqrMagnitude > 0.01f
                ? transform.TransformDirection(input.normalized)
                : -transform.forward;

            _dodgeRemaining = _config.DodgeDuration;
            _cooldownRemaining = _config.DodgeCooldown;
            if (_health != null) _health.SetInvulnerable(_config.DodgeInvulnerableSeconds);
            Debug.Log("[Dodge] 회피 — 무적 " + _config.DodgeInvulnerableSeconds + "초");
        }
    }
}
