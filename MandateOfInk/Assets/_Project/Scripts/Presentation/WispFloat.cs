using UnityEngine;

namespace MandateOfInk.Presentation
{
    /// <summary>
    /// 무정형 개체(도깨비불 등) 절차 부유 모션 — 리깅 없이 트랜스폼만 흔든다.
    /// 둥실거림(상하) + 표류(수평 펄린) + 화염 맥동(스케일) + 느린 자전 + 조명 일렁임. 수치는 전부 [가정].
    /// </summary>
    public sealed class WispFloat : MonoBehaviour
    {
        [Header("[가정] 부유")]
        [SerializeField] private float _bobAmplitude = 0.12f;
        [SerializeField] private float _bobFrequency = 0.9f;
        [SerializeField] private float _driftRadius = 0.08f;
        [SerializeField] private float _driftFrequency = 0.35f;

        [Header("[가정] 화염 맥동")]
        [SerializeField] private float _pulseAmplitude = 0.055f;
        [SerializeField] private float _pulseFrequency = 3.2f;
        [SerializeField] private float _yawDegreesPerSecond = 14f;

        [Header("[가정] 빌보드 — 납작한 화염 메시를 항상 카메라로")]
        [Tooltip("켜면 자전 대신 수평(요)으로만 카메라를 바라본다")]
        [SerializeField] private bool _faceCamera;

        [Header("[가정] 조명 일렁임(선택)")]
        [SerializeField] private Light _glowLight;
        [SerializeField] private float _lightFlicker = 0.35f;

        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private float _baseIntensity;
        private float _phase;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseScale = transform.localScale;
            _phase = Random.value * 100f;
            if (_glowLight != null) _baseIntensity = _glowLight.intensity;
        }

        private void Update()
        {
            float t = Time.time + _phase;
            float bob = Mathf.Sin(t * _bobFrequency * Mathf.PI * 2f) * _bobAmplitude;
            float dx = (Mathf.PerlinNoise(t * _driftFrequency, _phase) - 0.5f) * 2f * _driftRadius;
            float dz = (Mathf.PerlinNoise(_phase, t * _driftFrequency) - 0.5f) * 2f * _driftRadius;
            transform.localPosition = _basePosition + new Vector3(dx, bob, dz);

            // 맥동: 위로 길쭉하게 일렁임 + 미세 무작위 떨림
            float pulse = Mathf.Sin(t * _pulseFrequency * Mathf.PI * 2f) * _pulseAmplitude;
            float jitter = (Mathf.PerlinNoise(t * 7f, _phase + 33f) - 0.5f) * _pulseAmplitude;
            transform.localScale = new Vector3(
                _baseScale.x * (1f - pulse * 0.5f + jitter),
                _baseScale.y * (1f + pulse),
                _baseScale.z * (1f - pulse * 0.5f + jitter));

            if (_faceCamera)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 to = transform.position - cam.transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(to);
                }
            }
            else transform.Rotate(0f, _yawDegreesPerSecond * Time.deltaTime, 0f, Space.Self);

            if (_glowLight != null)
                _glowLight.intensity = _baseIntensity *
                    (1f - _lightFlicker * 0.5f + Mathf.PerlinNoise(t * 9f, _phase + 71f) * _lightFlicker);
        }
    }
}
