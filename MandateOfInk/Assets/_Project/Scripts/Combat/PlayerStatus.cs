using UnityEngine;

namespace MandateOfInk.Combat
{
    // 플레이어 상태 이상 수신 — 적 아키타입 효과의 대상측.
    //   속박(목): 이동 감속 / 끌림(수): 시전자 쪽으로 당겨짐.
    // [프로토 절충] StarterAssets 컨트롤러를 직접 만지지 않고, CharacterController에
    // 반대 방향 보정 이동을 더해 감속을 흉내낸다(컨트롤러 독립·asmdef 무접촉).
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerStatus : MonoBehaviour
    {
        private CharacterController _controller;

        private float _slowUntil;
        private float _slowMultiplier = 1f;
        private Vector3 _pullPerSecond;
        private float _pullUntil;

        public bool IsSlowed => Time.time < _slowUntil;

        public static PlayerStatus GetOrAdd(GameObject playerObject)
        {
            var status = playerObject.GetComponent<PlayerStatus>();
            if (status == null) status = playerObject.AddComponent<PlayerStatus>();
            return status;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        // 목(속박탄): moveMultiplier = 남는 이동 비율 (0.45 = 55% 감속)
        public void ApplySlow(float moveMultiplier, float seconds)
        {
            _slowMultiplier = Mathf.Clamp01(moveMultiplier);
            _slowUntil = Time.time + seconds;
            Debug.Log($"[Player] 속박 — 이동 x{_slowMultiplier:F2}, {seconds:F1}s");
        }

        // 수(끌물결): direction 쪽으로 distance만큼 seconds에 걸쳐 끌려간다
        public void ApplyPull(Vector3 direction, float distance, float seconds)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f || seconds <= 0.05f) return;
            _pullPerSecond = direction.normalized * (distance / seconds);
            _pullUntil = Time.time + seconds;
            Debug.Log($"[Player] 끌림 — {distance:F1}m / {seconds:F1}s");
        }

        private void Update()
        {
            if (_controller == null || !_controller.enabled) return;

            // 감속: 이번 프레임 수평 이동량의 (1-배율)만큼 반대로 밀어 상쇄
            if (IsSlowed)
            {
                Vector3 v = _controller.velocity;
                v.y = 0f;
                _controller.Move(-v * ((1f - _slowMultiplier) * Time.deltaTime));
            }
            // 끌림
            if (Time.time < _pullUntil)
                _controller.Move(_pullPerSecond * Time.deltaTime);
        }
    }
}
