using System;
using UnityEngine;

namespace MandateOfInk.Core.Events
{
    // 페이로드가 있는 ScriptableObject 이벤트 채널의 공용 베이스.
    // 시스템끼리 서로를 직접 참조하는 대신 채널 에셋을 통해 느슨하게 통신한다.
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        [SerializeField, TextArea] private string _description;

#if UNITY_EDITOR
        // 에디터 디버그 표시용 — 빌드에는 포함되지 않는다
        [NonSerialized] public int DebugRaiseCount;
        [NonSerialized] public T DebugLastPayload;
#endif

        public event Action<T> OnRaised;

        public void Raise(T payload)
        {
#if UNITY_EDITOR
            DebugRaiseCount++;
            DebugLastPayload = payload;
#endif
            if (OnRaised == null)
            {
                Debug.LogWarning($"[EventChannel] 리스너가 없는 채널이 발신됨: {name}", this);
                return;
            }
            OnRaised.Invoke(payload);
        }
    }
}
