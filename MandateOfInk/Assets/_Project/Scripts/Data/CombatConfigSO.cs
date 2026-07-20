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

        [Header("그로기(포이즈) — 한계 도달 시 행동 불능 + 받는 피해 증가")]
        public float GroggySeconds = 3f;
        public float GroggyDamageMultiplier = 1.5f;
        [Tooltip("그로기 아닐 때 포이즈 자연 회복(초당) — turtling 처벌(전투코어루프 §4)")]
        public float PoiseRegenPerSecond = 6f;
        [Tooltip("술식 피해 -> 포이즈 축적 비율 — 그로기는 받아치기·상합이 주로 쌓는다(§4)")]
        [Range(0f, 2f)] public float SpellPoiseFraction = 0.1f;
        [Tooltip("평타 1회 포이즈 피해 — 거의 0(§4)")]
        public float MeleePoiseDamage = 1f;

        [Header("받아치기 — 상극 방어 진 3단 판정 (전투코어루프 §3)")]
        [Tooltip("선제 받아치기(기본) 시 발사한 적 MaxPoise 대비 축적 비율")]
        [Range(0f, 1f)] public float ParryPoiseFraction = 0.45f;
        [Tooltip("막 받아치기(저스트): 막 완성 후 이 시간 안에 받아내면 추가 보상")]
        public float JustParryWindowSeconds = 0.5f;
        [Tooltip("저스트 성공 시 MaxPoise 대비 축적 비율(선제보다 크게)")]
        [Range(0f, 1f)] public float JustParryPoiseFraction = 0.75f;
        [Tooltip("저스트 성공 시에만 먹 환급(§5 교전 충전)")]
        public float ParryInkRefund = 12f;
        [Tooltip("방어->공격 상성 배율이 이 이상이면 상극 = 완전 무효")]
        public float ParryAdvantageThreshold = 1.25f;
        [Tooltip("방어->공격 상성 배율이 이 이하면 정반대 = 실패, 막히지 않는다")]
        public float ShieldBreakThreshold = 0.75f;
        [Tooltip("상생·무관(중립) = 약한 막기 — 피해 중 이 비율이 관통한다")]
        [Range(0f, 1f)] public float NeutralBlockDamageThrough = 0.4f;
        [Tooltip("중립 막기 시 적 MaxPoise 대비 소량 축적")]
        [Range(0f, 1f)] public float NeutralBlockPoiseFraction = 0.1f;
    }
}
