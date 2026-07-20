using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 대필(붓) 뷰모델 — 자루끝(손잡이) 축 겨누기 방식 (설계안 D14).
    // 손잡이 끝은 화면 오른쪽 아래에 고정된 축이고, 작도 중엔 붓끝이 마우스 커서(작도면)를
    // 겨누며 따라간다. 붓끝이 커서에 정확히 닿도록 길이를 약간 신축한다.
    // 이 컴포넌트는 BrushRig(ViewModelCamera의 자식)에 붙는다. 모델은 리그의 +z로 눕고
    // 손잡이 끝이 리그 원점에 오도록 배치한다.
    public sealed class BrushViewModel : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private SpellcraftModeController _modeController;
        [SerializeField] private Camera _viewCamera;

        [Header("[가정] 축·붓 규격")]
        [Tooltip("손잡이 끝이 고정되는 카메라 로컬 앵커")]
        [SerializeField] private Vector3 _anchorLocal = new Vector3(0.42f, -0.45f, 0.5f);
        [Tooltip("붓 기본 길이(m) — 모델 실측")]
        [SerializeField] private float _brushLength = 0.8f;
        [SerializeField] private float _drawPlaneDistance = 0.6f;
        [Tooltip("붓끝이 커서에 닿기 위한 길이 신축 허용 범위")]
        [SerializeField] private Vector2 _stretchClamp = new Vector2(0.55f, 1.8f);

        [Header("[가정] 움직임")]
        [Tooltip("붓끝이 커서를 따라오는 속도 — 낮을수록 무겁게 끌린다")]
        [SerializeField] private float _followLerpSpeed = 14f;

        [Header("[가정] 자세 — 교전 대기")]
        [SerializeField] private Vector3 _restLocalPosition = new Vector3(0.38f, -0.40f, 0.55f);
        [SerializeField] private Vector3 _restLocalEuler = new Vector3(-12f, 6f, 0f);

        private Vector3 _tipLocal; // 스무딩된 붓끝 목표(카메라 로컬)

        private void LateUpdate()
        {
            bool drawing = _modeController != null && _modeController.Mode == SpellcraftMode.Drawing;
            float k = 1f - Mathf.Exp(-_followLerpSpeed * Time.unscaledDeltaTime);

            if (drawing && _viewCamera != null)
            {
                // 커서의 작도면 위치(카메라 로컬) — 먹선과 같은 평면
                Vector3 mouse = Input.mousePosition;
                Vector3 world = _viewCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _drawPlaneDistance));
                Vector3 cursorLocal = _viewCamera.transform.InverseTransformPoint(world);
                _tipLocal = Vector3.Lerp(_tipLocal, cursorLocal, k);

                Vector3 dir = _tipLocal - _anchorLocal;
                float dist = Mathf.Max(dir.magnitude, 0.05f);

                transform.localPosition = Vector3.Lerp(transform.localPosition, _anchorLocal, k);
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    Quaternion.LookRotation(dir.normalized), k);
                float stretch = Mathf.Clamp(dist / _brushLength, _stretchClamp.x, _stretchClamp.y);
                var scale = transform.localScale;
                scale.z = Mathf.Lerp(scale.z, stretch, k);
                transform.localScale = new Vector3(1f, 1f, scale.z);
            }
            else
            {
                _tipLocal = _anchorLocal + Quaternion.Euler(_restLocalEuler) * (Vector3.forward * _brushLength);
                transform.localPosition = Vector3.Lerp(transform.localPosition, _restLocalPosition, k);
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    Quaternion.Euler(_restLocalEuler), k);
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, k);
            }
        }
    }
}
