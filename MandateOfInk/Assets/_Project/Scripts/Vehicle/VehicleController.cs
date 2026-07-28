using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Vehicle
{
    // 마석 자동차 주행 — 명시적 상태머신(Parked <-> Driven, CLAUDE.md 탑승↔도보 규칙).
    // Rigidbody 아케이드 주행: W/S 가감속, A/D 조향(속도 비례), 벽 충돌은 물리가 막는다.
    // 플레이어 쪽 제어(FPC 끄기·좌석 부착)는 VehicleMountGlue(ProtoGlue)가 이벤트 채널로 중계한다.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        public enum VehicleState { Parked, Driven }

        [SerializeField] private VehicleConfigSO _config;
        [Tooltip("탑승 시 플레이어가 붙는 좌석(운전석) — 자식 트랜스폼")]
        [SerializeField] private Transform _seat;

        private Rigidbody _rb;
        private float _speed; // 현재 전진 속도(음수=후진)

        public VehicleState State { get; private set; } = VehicleState.Parked;
        public Transform Seat => _seat;
        public VehicleConfigSO Config => _config;
        public float CurrentSpeed => _speed;

        // 퍼즐 작도(차량 가속 술식) 훅 — 1보다 크면 최고 속도·가속이 함께 늘어난다 [가정]
        public float SpeedMultiplier { get; set; } = 1f;
        private float _boostUntil;

        // 「넌」(화+ㅓ+ㄴ) — 마석 보일러에 화력을 지속 주입(시간 만료 시 자동 복귀)
        public void ApplyBoost(float multiplier, float seconds)
        {
            SpeedMultiplier = multiplier;
            _boostUntil = Time.time + seconds;
            Debug.Log($"[Vehicle] {name} 화력 가속 x{multiplier:F1} ({seconds:F0}s)");
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        public void SetDriven(bool driven)
        {
            State = driven ? VehicleState.Driven : VehicleState.Parked;
            if (!driven) _speed = 0f;
            Debug.Log($"[Vehicle] {name} 상태 -> {State}");
        }

        private void FixedUpdate()
        {
            if (_config == null) return;
            float dt = Time.fixedDeltaTime;

            if (SpeedMultiplier > 1f && Time.time >= _boostUntil) SpeedMultiplier = 1f; // 가속 만료

            if (State == VehicleState.Driven)
            {
                float throttle = Input.GetAxisRaw("Vertical");   // W=1, S=-1
                float steer = Input.GetAxisRaw("Horizontal");    // D=1, A=-1

                float maxF = _config.MaxForwardSpeed * SpeedMultiplier;
                float maxR = _config.MaxReverseSpeed;
                if (throttle > 0.01f)
                {
                    // 후진 중 전진 입력 = 브레이크부터
                    float rate = _speed < 0f ? _config.BrakeDeceleration : _config.Acceleration * SpeedMultiplier;
                    _speed = Mathf.MoveTowards(_speed, maxF, rate * dt);
                }
                else if (throttle < -0.01f)
                {
                    float rate = _speed > 0f ? _config.BrakeDeceleration : _config.Acceleration;
                    _speed = Mathf.MoveTowards(_speed, -maxR, rate * dt);
                }
                else
                {
                    _speed = Mathf.MoveTowards(_speed, 0f, _config.CoastDeceleration * dt);
                }

                // 조향 — 속도가 붙어야 돌고, 후진 시 반대로 꺾인다(자동차 감각)
                float speedFactor = Mathf.Clamp01(Mathf.Abs(_speed) / Mathf.Max(_config.MinSpeedForTurn, 0.01f));
                float direction = _speed >= 0f ? 1f : -1f;
                float yaw = steer * _config.TurnDegreesPerSecond * speedFactor * direction * dt;
                // 월드 Y축 기준(왼쪽 곱) — 모델에 축 보정 회전이 있어도 차가 옆으로 구르지 않는다
                _rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f) * _rb.rotation);
            }
            else
            {
                _speed = Mathf.MoveTowards(_speed, 0f, _config.CoastDeceleration * dt);
            }

            // 수평 속도는 차체가, 수직 속도는 중력이 담당
            var velocity = transform.forward * _speed;
            velocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = velocity;
        }
    }
}
