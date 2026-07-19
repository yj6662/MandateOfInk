using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 대필(붓) 뷰모델 — 교전 중엔 대기 자세, 작도 중엔 붓끝이 커서를 따라간다.
    // ViewModelCamera의 자식에 붙인다. 파라미터는 [가정] — 자세·무게감은 인스펙터 튜닝.
    public sealed class BrushViewModel : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private SpellcraftModeController _modeController;
        [SerializeField] private Camera _viewCamera;

        [Header("[가정] 움직임")]
        [Tooltip("붓이 커서를 따라오는 속도 — 낮을수록 무겁게 끌린다")]
        [SerializeField] private float _followLerpSpeed = 12f;
        [SerializeField] private float _drawPlaneDistance = 0.6f;
        [Tooltip("붓끝이 커서에 닿도록 하는 피벗 보정 (모델에 맞춰 조정)")]
        [SerializeField] private Vector3 _tipOffset = new Vector3(0.02f, -0.06f, 0.05f);

        [Header("[가정] 자세")]
        [SerializeField] private Vector3 _restLocalPosition = new Vector3(0.35f, -0.3f, 0.7f);
        [SerializeField] private Vector3 _restLocalEuler = new Vector3(-15f, 5f, 0f);
        [SerializeField] private Vector3 _drawLocalEuler = new Vector3(-65f, 0f, 0f); // 붓을 세운 작도 자세

        private void LateUpdate()
        {
            bool drawing = _modeController != null && _modeController.Mode == SpellcraftMode.Drawing;
            float k = 1f - Mathf.Exp(-_followLerpSpeed * Time.unscaledDeltaTime);

            if (drawing && _viewCamera != null)
            {
                Vector3 mouse = Input.mousePosition;
                Vector3 world = _viewCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _drawPlaneDistance));
                Vector3 local = _viewCamera.transform.InverseTransformPoint(world) + _tipOffset;
                transform.localPosition = Vector3.Lerp(transform.localPosition, local, k);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(_drawLocalEuler), k);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, _restLocalPosition, k);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(_restLocalEuler), k);
            }
        }
    }
}
