using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 강화 정의 DTO. 비용은 마석(작도 연료이자 강화/해금 재료).
    // 마석은 화폐가 아니다 — 화폐(조선통보) 비용과 절대 섞지 말 것.
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Upgrade Definition", fileName = "UP_NewUpgrade")]
    public sealed class UpgradeDefinitionSO : SerializedScriptableObject
    {
        [Title("식별")]
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;

        [Title("비용")]
        [Tooltip("[가정] 마석 비용 — 재료이지 화폐가 아님")] public int MagicStoneCost = 1;

        [Title("효과")]
        [Tooltip("효과 파라미터 — 추상 기반 클래스 다형성(Odin 직렬화)")]
        public List<EffectParam> Effects = new List<EffectParam>();
    }
}
