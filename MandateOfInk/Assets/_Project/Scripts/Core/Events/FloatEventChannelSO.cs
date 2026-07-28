using UnityEngine;

namespace MandateOfInk.Core.Events
{
    // float 페이로드 이벤트 채널 — 도약 속도(EC_PlayerLeap) 등 수치 하나를 전달할 때 쓴다.
    [CreateAssetMenu(menuName = "MandateOfInk/Events/Float Event Channel", fileName = "EC_NewFloatEvent")]
    public sealed class FloatEventChannelSO : EventChannelSO<float>
    {
    }
}
