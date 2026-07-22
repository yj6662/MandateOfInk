using MandateOfInk.Data;
using MandateOfInk.Spellcraft;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 비상 수동 갈기 — R 홀드로 크게 충전하는 느리고 처벌 가능한 도박(에스투스식).
    // 예열 후 충전이 시작되며, 도중 피격 시 즉시 취소된다.
    public sealed class InkGrindController : MonoBehaviour
    {
        [SerializeField] private CombatConfigSO _config;
        [SerializeField] private InkPool _inkPool;

        private PlayerHealth _health;
        private float _holdTime;
        private bool _grinding;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDamaged += CancelGrind;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= CancelGrind;
        }

        private void Update()
        {
            if (_config == null || _inkPool == null) return;

            if (Input.GetKey(KeyCode.R))
            {
                _holdTime += Time.deltaTime;
                if (_holdTime >= _config.GrindStartDelay)
                {
                    if (!_grinding) { _grinding = true; Debug.Log("[Ink] 먹 갈기 시작"); }
                    _inkPool.Add(_config.GrindPerSecond * Time.deltaTime);
                }
            }
            else if (_holdTime > 0f)
            {
                _holdTime = 0f;
                _grinding = false;
            }
        }

        private void CancelGrind()
        {
            if (!_grinding && _holdTime <= 0f) return;
            _holdTime = -0.5f; // 피격 직후 잠깐은 재시작 불가 [가정]
            _grinding = false;
            Debug.Log("[Ink] 피격 — 먹 갈기 취소");
        }
    }
}
