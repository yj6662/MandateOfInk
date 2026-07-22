using UnityEngine;

namespace MandateOfInk.Presentation
{
    /// <summary>
    /// 더미(기본 도형) 식별 라벨 — 항상 카메라를 바라본다. 프로토타입 전용, 정식 모델 교체 시 제거.
    /// </summary>
    public sealed class BillboardLabel : MonoBehaviour
    {
        private Transform _cam;

        private void LateUpdate()
        {
            if (_cam == null)
            {
                var main = Camera.main;
                if (main == null) return;
                _cam = main.transform;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }
    }
}
