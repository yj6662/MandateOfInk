using MandateOfInk.Core.Events;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 도면(진) 페이로드 이벤트 채널 — 작도 인식 성공(Spellcraft) -> 시전(Combat) 통신용.
    [CreateAssetMenu(menuName = "MandateOfInk/Events/Diagram Event Channel", fileName = "EC_NewDiagramEvent")]
    public sealed class DiagramEventChannelSO : EventChannelSO<SpellDiagramSO>
    {
    }
}
