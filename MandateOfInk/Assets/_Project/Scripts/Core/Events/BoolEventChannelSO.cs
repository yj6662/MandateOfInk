using UnityEngine;

namespace MandateOfInk.Core.Events
{
    // bool 페이로드 이벤트 채널 (예: 작도 모드 on/off).
    [CreateAssetMenu(menuName = "MandateOfInk/Events/Bool Event Channel", fileName = "EC_NewBoolEvent")]
    public sealed class BoolEventChannelSO : EventChannelSO<bool>
    {
    }
}
