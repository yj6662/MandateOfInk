using UnityEngine;

namespace MandateOfInk.Data
{
    // 사냥 방(榜) 한 장 — 거점 게시판에 붙는 현상 의뢰. 보상은 조선통보(화폐)만.
    [CreateAssetMenu(menuName = "MandateOfInk/Bounty", fileName = "BT_")]
    public sealed class BountySO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Header("목표")]
        [Tooltip("EnemyDefinitionSO.Id와 일치해야 처치가 집계된다")]
        public string TargetEnemyId;
        public int TargetCount = 1;

        [Header("보상 [가정]")]
        public int RewardCoins = 10;
    }
}
