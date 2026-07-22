using MandateOfInk.Core.Events;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 작도 시전 요청 — 인식된 도면 + 판정 품질(약발동 여부·위력 배율).
    [System.Serializable]
    public struct DiagramCastRequest
    {
        public SpellDiagramSO Diagram;
        public bool IsWeak;           // 약발동(임계 미달) 여부 — 완전 불발은 없다(절대 규칙)
        public float PowerMultiplier; // 위력 배율 (정발동 1)
    }

    // 작도 인식 성공(Spellcraft) -> 시전(Combat) 채널.
    [CreateAssetMenu(menuName = "MandateOfInk/Events/Diagram Event Channel", fileName = "EC_NewDiagramEvent")]
    public sealed class DiagramEventChannelSO : EventChannelSO<DiagramCastRequest>
    {
    }
}
