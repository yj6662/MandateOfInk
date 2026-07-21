using UnityEngine;

namespace MandateOfInk.Core.Events
{
    // 문자열 페이로드 채널 — 적 처치 알림(적 Id) 등 식별자 전달용.
    [CreateAssetMenu(menuName = "MandateOfInk/Events/String Channel", fileName = "CH_String")]
    public sealed class StringEventChannelSO : EventChannelSO<string> { }
}
