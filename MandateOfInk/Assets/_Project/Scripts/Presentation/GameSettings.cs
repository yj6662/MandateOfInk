using MandateOfInk.Combat;
using UnityEngine;

namespace MandateOfInk.Presentation
{
    // 설정 저장·적용 헬퍼(정적 유틸 — 매니저 아님). PlayerPrefs에 보존하고 부팅 시 재적용한다.
    // 아직 소비처가 없는 값(음악/효과 볼륨, 감도 등)은 키만 확보해두고 해당 시스템 구현 시 연결한다.
    public static class GameSettings
    {
        public const string KeyMasterVolume = "opt_master_volume";
        public const string KeyMusicVolume = "opt_music_volume";
        public const string KeySfxVolume = "opt_sfx_volume";
        public const string KeyMouseSensitivity = "opt_mouse_sensitivity";
        public const string KeyInvertY = "opt_invert_y";
        public const string KeyShakeScale = "opt_shake_scale";
        public const string KeyQuality = "opt_quality";
        public const string KeyFullscreen = "opt_fullscreen";
        public const string KeyVSync = "opt_vsync";

        public static float Get(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);

        public static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            ApplyOne(key, value);
        }

        // 저장된 설정 일괄 적용 — 메뉴/인게임 진입 시 호출
        public static void ApplyAll()
        {
            ApplyOne(KeyMasterVolume, Get(KeyMasterVolume, 1f));
            ApplyOne(KeyShakeScale, Get(KeyShakeScale, 1f));
            ApplyOne(KeyQuality, Get(KeyQuality, QualitySettings.GetQualityLevel()));
            ApplyOne(KeyVSync, Get(KeyVSync, QualitySettings.vSyncCount > 0 ? 1f : 0f));
            // 전체화면·해상도는 사용자가 명시 조작할 때만 변경(부팅 시 강제하지 않음)
        }

        private static void ApplyOne(string key, float value)
        {
            switch (key)
            {
                case KeyMasterVolume: AudioListener.volume = Mathf.Clamp01(value); break;
                case KeyShakeScale: CameraShake.GlobalScale = Mathf.Clamp(value, 0f, 2f); break;
                case KeyQuality: QualitySettings.SetQualityLevel(Mathf.Clamp((int)value, 0, QualitySettings.names.Length - 1)); break;
                case KeyFullscreen: Screen.fullScreen = value > 0.5f; break;
                case KeyVSync: QualitySettings.vSyncCount = value > 0.5f ? 1 : 0; break;
                // KeyMusicVolume/KeySfxVolume: 오디오 시스템 도입 시 연결
                // KeyMouseSensitivity/KeyInvertY: 시점 컨트롤러 연동 시 연결
            }
        }
    }
}
