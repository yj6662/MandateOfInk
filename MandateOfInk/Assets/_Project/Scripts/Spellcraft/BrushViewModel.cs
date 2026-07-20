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
        [Tooltip("붓끝 스프링 진동수 — 클수록 빠르게 따라붙는다")]
        [SerializeField] private float _springFrequency = 9f;
        [Tooltip("감쇠(0~1) — 1 미만이면 살짝 지나쳤다 돌아오는 관성(대필의 무게)이 생긴다")]
        [SerializeField, Range(0.1f, 1.2f)] private float _springDamping = 0.55f;

        [Header("[가정] 자세 — 교전 대기 (붓끝 전방)")]
        [SerializeField] private Vector3 _restLocalPosition = new Vector3(0.38f, -0.40f, 0.55f);
        [SerializeField] private Vector3 _restLocalEuler = new Vector3(-12f, 6f, 0f);

        [Header("[가정] 자세 — 뒤집힘 (자루끝 전방, 평타 후 유지)")]
        [SerializeField] private Vector3 _reverseLocalPosition = new Vector3(0.40f, -0.42f, 0.50f);
        [SerializeField] private Vector3 _reverseLocalEuler = new Vector3(-16f, 186f, 0f);

        [Header("[가정] 평타 휘두르기 — 자루로 후려치는 스윙(전투코어루프 §10)")]
        [SerializeField] private float _swingSeconds = 0.3f;
        [SerializeField] private float _swingArcDegrees = 100f; // 좌우 스윙 폭(요)
        [SerializeField] private float _swingDipDegrees = 25f;  // 스윙 정점에서 아래로 파고드는 피치
        [SerializeField] private float _swingPunch = 0.22f;     // 스윙 중 전방 밀림

        private Vector3 _tipLocal;    // 붓끝 목표(카메라 로컬) — 스프링 적분 결과
        private Vector3 _tipVelocity; // 스프링 속도
        private float _swingTimer = -1f;  // 0 이상이면 휘두르기 진행 중
        private bool _handleForward;      // 토글: 평타 후엔 자루끝이 전방을 향한 채 유지된다
        private Vector3 _smoothPos;       // 자세 보간 상태 — 스윙 오버레이와 분리해 모션이 뭉개지지 않게
        private Quaternion _smoothRot = Quaternion.identity;

        private void Awake()
        {
            _smoothPos = transform.localPosition;
            _smoothRot = transform.localRotation;
        }

        // 평타가 나갈 때 호출 — 붓을 뒤집어 자루로 후려치고, 그대로 뒤집힌 채 든다(토글)
        public void PlayMeleeSwing()
        {
            _swingTimer = 0f;
            _handleForward = true;
        }

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
                // 스프링 적분(반음감쇠) — 목표를 살짝 지나쳤다 돌아오는 관성이 대필의 무게로 읽힌다.
                // 표현 전용이다: 인식·먹선은 날것 마우스 점에서 별도로 간다(§11).
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                Vector3 accel = (cursorLocal - _tipLocal) * (_springFrequency * _springFrequency)
                    - _tipVelocity * (2f * _springDamping * _springFrequency);
                _tipVelocity += accel * dt;
                _tipLocal += _tipVelocity * dt;

                Vector3 dir = _tipLocal - _anchorLocal;
                float dist = Mathf.Max(dir.magnitude, 0.05f);

                _handleForward = false; // 작도 진입 = 붓을 다시 돌려 붓끝(먹)이 앞으로

                _smoothPos = Vector3.Lerp(_smoothPos, _anchorLocal, k);
                _smoothRot = Quaternion.Slerp(_smoothRot, Quaternion.LookRotation(dir.normalized), k);
                transform.localPosition = _smoothPos;
                transform.localRotation = _smoothRot;
                float stretch = Mathf.Clamp(dist / _brushLength, _stretchClamp.x, _stretchClamp.y);
                var scale = transform.localScale;
                scale.z = Mathf.Lerp(scale.z, stretch, k);
                transform.localScale = new Vector3(1f, 1f, scale.z);
            }
            else
            {
                _tipLocal = _anchorLocal + Quaternion.Euler(_restLocalEuler) * (Vector3.forward * _brushLength);
                _tipVelocity = Vector3.zero;

                // 평타 휘두르기 오버레이 — 자루가 좌->우 호를 그리며 후려친다.
                // 보간을 거치지 않고 직접 가산해 스윙이 뭉개지지 않는다.
                Quaternion swingRot = Quaternion.identity;
                Vector3 swingOffset = Vector3.zero;
                if (_swingTimer >= 0f)
                {
                    _swingTimer += Time.deltaTime;
                    float p = _swingTimer / Mathf.Max(_swingSeconds, 0.05f);
                    if (p >= 1f) _swingTimer = -1f;
                    else
                    {
                        float eased = Mathf.SmoothStep(0f, 1f, p);
                        float yaw = Mathf.Lerp(_swingArcDegrees * 0.5f, -_swingArcDegrees * 0.5f, eased);
                        float dip = Mathf.Sin(p * Mathf.PI) * _swingDipDegrees; // 정점에서 파고들었다 빠진다
                        swingRot = Quaternion.Euler(dip, yaw, 0f);
                        swingOffset = Vector3.forward * (Mathf.Sin(p * Mathf.PI) * _swingPunch);
                    }
                }

                // 토글 자세: 평타 후엔 자루끝 전방(뒤집힘) 유지, 아니면 붓끝 전방
                Vector3 basePos = _handleForward ? _reverseLocalPosition : _restLocalPosition;
                Vector3 baseEuler = _handleForward ? _reverseLocalEuler : _restLocalEuler;
                _smoothPos = Vector3.Lerp(_smoothPos, basePos, k);
                _smoothRot = Quaternion.Slerp(_smoothRot, Quaternion.Euler(baseEuler), k);
                transform.localPosition = _smoothPos + swingOffset;
                transform.localRotation = _smoothRot * swingRot;
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, k);
            }
        }
    }
}
