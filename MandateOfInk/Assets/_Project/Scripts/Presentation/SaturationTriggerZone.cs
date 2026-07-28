using UnityEngine;

namespace MandateOfInk.Presentation
{
    // 특정 지역/이벤트 연출용 트리거 — 플레이어가 콜라이더 볼륨에 들어오면 지정된
    // SaturationController들을 한꺼번에 목표 채도로 전환한다(사용자 결정 2026-07-24).
    // 예: 회상 장면 진입 시 주변 환경을 흑백->채색으로, 퇴장 시 도로 흑백으로.
    [RequireComponent(typeof(Collider))]
    public sealed class SaturationTriggerZone : MonoBehaviour
    {
        [Tooltip("진입 시 이 채도로 전환(0=흑백, 1=원색)")]
        [SerializeField, Range(0f, 1f)] private float _onEnterSaturation = 1f;
        [Tooltip("퇴장 시 이 채도로 되돌릴지 — 끄면 진입 전환은 편도(1회성 연출)")]
        [SerializeField] private bool _revertOnExit = true;
        [SerializeField, Range(0f, 1f)] private float _onExitSaturation = 0f;
        [SerializeField] private float _fadeSeconds = 1.5f;

        [Tooltip("전환 대상 — 이 구역 안의 환경 오브젝트들")]
        [SerializeField] private SaturationController[] _targets;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<CharacterController>() == null) return;
            SetAll(_onEnterSaturation);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_revertOnExit) return;
            if (other.GetComponentInParent<CharacterController>() == null) return;
            SetAll(_onExitSaturation);
        }

        private void SetAll(float saturation)
        {
            if (_targets == null) return;
            foreach (var t in _targets)
            {
                if (t != null) t.SetTarget(saturation, _fadeSeconds);
            }
        }
    }
}
