using System;
using UnityEngine;

namespace MandateOfInk.Core.Events
{
    // 페이로드 없는 신호용 이벤트 채널 (예: 플레이어 사망, 작도 모드 진입).
    [CreateAssetMenu(menuName = "MandateOfInk/Events/Void Event Channel", fileName = "EC_NewVoidEvent")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        [SerializeField, TextArea] private string _description;

#if UNITY_EDITOR
        [NonSerialized] public int DebugRaiseCount;
#endif

        public event Action OnRaised;

        public void Raise()
        {
#if UNITY_EDITOR
            DebugRaiseCount++;
#endif
            if (OnRaised == null)
            {
                Debug.LogWarning($"[EventChannel] 리스너가 없는 채널이 발신됨: {name}", this);
                return;
            }
            OnRaised.Invoke();
        }

#if UNITY_EDITOR
        // 인스펙터 우클릭으로 발신 테스트
        [ContextMenu("Raise (디버그)")]
        private void DebugRaise() => Raise();
#endif
    }
}
