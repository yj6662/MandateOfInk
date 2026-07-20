using UnityEngine;

namespace MandateOfInk.Data
{
    // 종성(받침) 거동 수치 — 전부 [가정], 손맛 튜닝으로 확정.
    // 격발(상합) 판정 매트릭스는 문서 미결 — 현재는 「아무 술식 적중 시 격발」 단순 규칙이며,
    // 5x5 상합 매트릭스가 확정되면 이 SO에 테이블로 추가한다.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Final Modifier Config", fileName = "FinalModifierConfig")]
    public sealed class FinalModifierConfigSO : ScriptableObject
    {
        [Header("ㄱ(목) 속박 — 적중 대상 감속")]
        [Tooltip("속박 중 이동 속도 배율 (0.25 = 75% 감속)")]
        [Range(0f, 1f)] public float BindMoveMultiplier = 0.25f;
        public float BindSeconds = 2.5f;

        [Header("ㄴ(화) 지속 — 시간에 걸친 추가 피해")]
        public float SustainSeconds = 3f;
        public float SustainTickInterval = 0.5f;
        [Tooltip("틱당 피해 = 본 피해 x 이 비율")]
        [Range(0f, 1f)] public float SustainTickFraction = 0.3f;

        [Header("ㅁ(토) 격발 — 표식 설치, 다음 술식 적중 시 격발")]
        public float MarkSeconds = 8f;
        [Tooltip("격발 보너스 = 격발시킨 타격 피해 x 이 배율")]
        public float TriggerBonusMultiplier = 2.2f;
        [Tooltip("격발 폭발 표현 지름")]
        public float TriggerBurstDiameter = 3.5f;

        [Header("ㅅ(금) 관통 — 다수를 뚫고 직선 진행")]
        [Tooltip("최대 관통 대상 수")]
        public int PierceMaxTargets = 3;

        [Header("ㅇ(수) 연쇄 — 인접 대상으로 전파")]
        public int ChainMaxJumps = 3;
        public float ChainRadius = 8f;
        [Tooltip("점프마다 피해 배율 (0.7 = 30%씩 감쇠)")]
        [Range(0f, 1f)] public float ChainDamageFalloff = 0.7f;
    }
}
