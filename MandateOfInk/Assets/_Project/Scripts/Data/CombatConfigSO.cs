using UnityEngine;

namespace MandateOfInk.Data
{
    // 교전 동사·먹 경제 수치. 전부 [가정] — M1 실측·손맛 튜닝으로 확정.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Combat Config", fileName = "CombatConfig")]
    public sealed class CombatConfigSO : ScriptableObject
    {
        [Header("회피 — Left Shift 짧게 탭(길게 누르면 기존 질주)")]
        [Tooltip("이 시간(초) 안에 떼면 회피, 넘기면 질주로 간주")]
        public float DodgeTapThreshold = 0.25f;
        public float DodgeDuration = 0.18f;
        public float DodgeSpeed = 14f;
        [Tooltip("무적 프레임 길이(초)")]
        public float DodgeInvulnerableSeconds = 0.35f;
        public float DodgeCooldown = 0.6f;

        [Header("평타 — 대필 자루끝(먹 버는 동작, 딜링 수단 아님)")]
        public float MeleeDamage = 3f;
        public float MeleeRange = 2.2f;
        public float MeleeRadius = 0.6f;
        public float MeleeCooldown = 0.5f;
        [Tooltip("적중 시 먹 충전량")]
        public float MeleeInkRefund = 6f;

        [Header("먹 풀")]
        public float MaxInk = 100f;
        [Tooltip("베이스라인 트리클(초당) — 교전 소비를 못 따라갈 만큼 느리게")]
        public float TricklePerSecond = 0.3f;

        [Header("쥐어짜기 — 잔량이 비용 미달이면 비율만큼 약해진 채 발동(불발 금지)")]
        [Tooltip("쥐어짜기 위력 하한")]
        [Range(0.05f, 1f)] public float SqueezeMinPower = 0.2f;

        [Header("수동 갈기 — R 홀드(도중 피격 시 취소)")]
        public float GrindPerSecond = 30f;
        [Tooltip("홀드 시작 후 충전이 시작되기까지 예열(초)")]
        public float GrindStartDelay = 0.35f;
    }
}
