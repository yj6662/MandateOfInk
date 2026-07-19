using MandateOfInk.Core.Events;
using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    public enum SpellcraftMode
    {
        Combat,  // 교전 — 정상 시간, 시점 조작
        Drawing, // 작도 — 시간 감속, 마우스는 붓
    }

    // 교전<->작도 모드 전환 상태머신.
    // 진입: 토글 키 -> 시간 감속 + 채널로 모드 변경 발신 (컨트롤러 잠금은 글루가 처리).
    // 해제: 현재는 같은 키로 토글. M1에서 자모 인식 성공(작도 완료) 이벤트가 해제를 대신한다.
    public sealed class SpellcraftModeController : MonoBehaviour
    {
        [SerializeField] private SpellcraftModeConfigSO _config;
        [SerializeField] private BoolEventChannelSO _drawingModeChanged;

        public SpellcraftMode Mode { get; private set; } = SpellcraftMode.Combat;

        private float _baseFixedDeltaTime;

        private void Awake()
        {
            _baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void Update()
        {
            if (_config != null && Input.GetKeyDown(_config.ToggleKey))
            {
                if (Mode == SpellcraftMode.Combat) EnterDrawing();
                else ExitDrawing();
            }
        }

        private void EnterDrawing()
        {
            Mode = SpellcraftMode.Drawing;
            Time.timeScale = _config.DrawingTimeScale;
            // 물리 스텝도 같이 줄여야 감속 중 물리가 뚝뚝 끊기지 않는다
            Time.fixedDeltaTime = _baseFixedDeltaTime * _config.DrawingTimeScale;
            _drawingModeChanged?.Raise(true);
            Debug.Log($"[Spellcraft] 작도 모드 진입 (timeScale={Time.timeScale:F2})");
        }

        // 작도 완료(인식 성공) 시 외부에서 호출 — 모드 해제 + 시간 복원
        public void CompleteDrawing()
        {
            if (Mode == SpellcraftMode.Drawing) ExitDrawing();
        }

        private void ExitDrawing()
        {
            Mode = SpellcraftMode.Combat;
            RestoreTime();
            _drawingModeChanged?.Raise(false);
            Debug.Log("[Spellcraft] 교전 모드 복귀 (timeScale=1)");
        }

        private void RestoreTime()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDeltaTime;
        }

        private void OnDestroy()
        {
            // 작도 중 씬 전환/파괴돼도 시간이 감속된 채 남지 않도록 안전장치
            if (Mode == SpellcraftMode.Drawing) RestoreTime();
        }
    }
}
