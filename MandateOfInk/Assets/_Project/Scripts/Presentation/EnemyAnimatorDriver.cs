using MandateOfInk.Combat;
using UnityEngine;

namespace MandateOfInk.Presentation
{
    /// <summary>
    /// 적 애니메이터 구동 — 이동 속도를 재서 Idle/Walk를 섞고,
    /// 전투(Combat) 이벤트(훔치기·사망)를 Animator 트리거로 옮긴다.
    /// AI 로직은 건드리지 않는 관찰자: Combat -> Presentation 단방향.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimatorDriver : MonoBehaviour
    {
        [Header("[가정] 보행 재생 배속 기준")]
        [Tooltip("클립이 상정한 이동 속도(m/s) — 실제 속도/기준으로 Walk 재생 배속을 정한다")]
        [SerializeField] private float _walkClipSpeed = 1.4f;
        [SerializeField] private float _speedSmoothing = 12f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int WalkScaleId = Animator.StringToHash("WalkScale");
        private static readonly int PickUpId = Animator.StringToHash("PickUp");
        private static readonly int DieId = Animator.StringToHash("Die");

        private Animator _animator;
        private EnemyAI _ai;
        private EnemyHealth _health;
        private Vector3 _lastPosition;
        private float _smoothedSpeed;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _ai = GetComponentInParent<EnemyAI>();
            _health = GetComponentInParent<EnemyHealth>();
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_ai != null) _ai.StoleCoins += OnStoleCoins;
            if (_health != null) _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_ai != null) _ai.StoleCoins -= OnStoleCoins;
            if (_health != null) _health.Died -= OnDied;
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f) return;
            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;
            float rawSpeed = delta.magnitude / Time.deltaTime;
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, _speedSmoothing * Time.deltaTime);

            _animator.SetFloat(SpeedId, _smoothedSpeed);
            _animator.SetFloat(WalkScaleId,
                Mathf.Clamp(_smoothedSpeed / Mathf.Max(_walkClipSpeed, 0.1f), 0.7f, 2.4f));
        }

        private void OnStoleCoins() => _animator.SetTrigger(PickUpId);
        private void OnDied() => _animator.SetTrigger(DieId);
    }
}
