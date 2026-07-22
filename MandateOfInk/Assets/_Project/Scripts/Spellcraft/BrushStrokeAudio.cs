using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 작도 붓 소리 — 획을 긋는 동안(DrawingInputController.IsStroking) 붓이 종이를 긋는 루프를 재생한다.
    // 커서 속도에 따라 음량·피치가 살아 움직인다: 빠른 획=거세게, 느린 획=은은하게, 멈추면 잦아든다.
    // 클립은 ElevenLabs 생성(SFX_BrushLoop*·SFX_BrushSwish*) — 소리 톤 판정은 사람(룩과 동일 원칙).
    // 작도 모드는 시간 감속(timeScale 0.2) 상태이므로 전부 unscaled 시간 기준으로 처리한다.
    public sealed class BrushStrokeAudio : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private DrawingInputController _drawing;
        [Tooltip("긋는 동안 재생할 붓 마찰 루프")]
        [SerializeField] private AudioClip _strokeLoop;
        [Tooltip("획 시작 순간 가볍게 얹는 스치는 소리(비우면 루프만) — 기필 액센트")]
        [SerializeField] private AudioClip[] _strokeStartOneShots;

        [Header("[가정] 음량·피치")]
        [SerializeField, Range(0f, 1f)] private float _maxVolume = 0.55f;
        [Tooltip("붓을 멈춘 채 누르고 있을 때의 바닥 음량 — 0이면 정지 시 무음")]
        [SerializeField, Range(0f, 1f)] private float _idleVolume = 0.05f;
        [Tooltip("이 커서 속도(픽셀/초)에서 최대 음량·최고 피치에 도달")]
        [SerializeField] private float _fullVolumeSpeed = 1600f;
        [Tooltip("음량이 목표로 이동하는 속도(초당 음량) — 클수록 즉각 반응")]
        [SerializeField] private float _volumeMoveSpeed = 6f;
        [Tooltip("느린 획(x)~빠른 획(y)의 피치")]
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.92f, 1.12f);
        [SerializeField, Range(0f, 1f)] private float _oneShotVolume = 0.35f;

        private AudioSource _loopSource;
        private AudioSource _oneShotSource;
        private Vector3 _prevMouse;
        private bool _wasStroking;

        private void Awake()
        {
            // 소스는 코드 생성 — 씬에는 이 컴포넌트 하나만 붙이면 된다. 1인칭 손끝 소리라 2D(spatialBlend 0).
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = 0f;
            _loopSource.volume = 0f;

            _oneShotSource = gameObject.AddComponent<AudioSource>();
            _oneShotSource.playOnAwake = false;
            _oneShotSource.spatialBlend = 0f;
        }

        private void Update()
        {
            if (_drawing == null || _strokeLoop == null) return;
            bool stroking = _drawing.IsStroking;

            // 획 시작 — 루프를 매번 다른 지점부터 재생해 반복감을 줄이고, 기필 액센트를 얹는다
            if (stroking && !_wasStroking)
            {
                _loopSource.clip = _strokeLoop;
                _loopSource.time = Random.Range(0f, Mathf.Max(0f, _strokeLoop.length - 0.2f));
                _loopSource.volume = 0f;
                _loopSource.Play();
                if (_strokeStartOneShots != null && _strokeStartOneShots.Length > 0)
                {
                    var clip = _strokeStartOneShots[Random.Range(0, _strokeStartOneShots.Length)];
                    if (clip != null) _oneShotSource.PlayOneShot(clip, _oneShotVolume);
                }
                _prevMouse = Input.mousePosition;
            }
            _wasStroking = stroking;

            if (!_loopSource.isPlaying) return;

            // 목표 음량·피치: 긋는 중엔 커서 속도 비례, 뗐으면 0으로 잦아들었다가 정지
            float target = 0f;
            if (stroking)
            {
                float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
                float speed = (Input.mousePosition - _prevMouse).magnitude / dt;
                _prevMouse = Input.mousePosition;
                float t = Mathf.Clamp01(speed / Mathf.Max(_fullVolumeSpeed, 1f));
                target = Mathf.Lerp(_idleVolume, _maxVolume, t);
                _loopSource.pitch = Mathf.Lerp(_pitchRange.x, _pitchRange.y, t);
            }
            _loopSource.volume = Mathf.MoveTowards(_loopSource.volume, target, _volumeMoveSpeed * Time.unscaledDeltaTime);
            if (!stroking && _loopSource.volume <= 0.001f) _loopSource.Stop();
        }
    }
}
