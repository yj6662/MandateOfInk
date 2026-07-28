using UnityEngine;

namespace MandateOfInk.Data
{
    // 마석 자동차 주행 수치 — 전부 [가정], 손맛 튜닝으로 확정.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Vehicle Config", fileName = "VehicleConfig")]
    public sealed class VehicleConfigSO : ScriptableObject
    {
        [Header("속도 (m/s) [가정]")]
        public float MaxForwardSpeed = 14f;
        public float MaxReverseSpeed = 4f;

        [Header("가감속 (m/s^2) [가정]")]
        public float Acceleration = 7f;
        [Tooltip("반대 방향 입력(브레이크) 시 감속")]
        public float BrakeDeceleration = 16f;
        [Tooltip("입력이 없을 때 자연 감속")]
        public float CoastDeceleration = 4f;

        [Header("조향 [가정]")]
        [Tooltip("최고 속도에서의 초당 회전량(도)")]
        public float TurnDegreesPerSecond = 80f;
        [Tooltip("이 속도(m/s) 이상부터 조향이 온전히 듣는다 — 정지 시 제자리 회전 방지")]
        public float MinSpeedForTurn = 0.5f;

        [Header("탑승 [가정]")]
        public float MountRange = 3.5f;
        [Tooltip("하차 시 차 왼쪽으로 내리는 거리(m)")]
        public float DismountSideOffset = 2.2f;
    }
}
