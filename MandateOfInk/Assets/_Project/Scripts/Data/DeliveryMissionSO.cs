using UnityEngine;

namespace MandateOfInk.Data
{
    // 배달 미션 — 주인공(배달 도사)의 핵심 루프. 화물을 받아 목적지까지, 상하지 않게.
    // 화물 상태는 최소 HUD 허용 항목(「화물 상태」)이다.
    [CreateAssetMenu(menuName = "MandateOfInk/Delivery Mission", fileName = "DM_")]
    public sealed class DeliveryMissionSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("보상 [가정]")]
        public int BaseRewardCoins = 30;
        [Tooltip("피격 1회당 화물 상태 하락(0~100 기준)")]
        public float ConditionLossPerHit = 15f;
        [Tooltip("만신창이 화물이라도 받는 최소 보상 비율")]
        [Range(0f, 1f)] public float MinRewardFraction = 0.3f;
    }
}
