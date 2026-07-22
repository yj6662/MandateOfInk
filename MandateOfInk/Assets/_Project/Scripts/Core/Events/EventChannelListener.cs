using UnityEngine;
using UnityEngine.Events;

namespace MandateOfInk.Core.Events
{
    // 제네릭 채널 리스너 베이스. 구체 페이로드 타입별 리스너는 이 클래스를 상속해 만든다.
    // 인스펙터에서 채널 에셋과 반응(UnityEvent)을 연결한다.
    public abstract class EventChannelListener<T> : MonoBehaviour
    {
        [SerializeField] private EventChannelSO<T> _channel;
        [SerializeField] private UnityEvent<T> _response;

        private void OnEnable()
        {
            if (_channel != null) _channel.OnRaised += HandleRaised;
        }

        private void OnDisable()
        {
            if (_channel != null) _channel.OnRaised -= HandleRaised;
        }

        private void HandleRaised(T payload) => _response.Invoke(payload);
    }
}
