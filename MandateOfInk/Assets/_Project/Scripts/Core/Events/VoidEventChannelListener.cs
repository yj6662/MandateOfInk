using UnityEngine;
using UnityEngine.Events;

namespace MandateOfInk.Core.Events
{
    // 페이로드 없는 채널의 리스너.
    public sealed class VoidEventChannelListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO _channel;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_channel != null) _channel.OnRaised += HandleRaised;
        }

        private void OnDisable()
        {
            if (_channel != null) _channel.OnRaised -= HandleRaised;
        }

        private void HandleRaised() => _response.Invoke();
    }
}
