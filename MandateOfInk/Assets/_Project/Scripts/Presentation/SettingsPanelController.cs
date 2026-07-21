using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MandateOfInk.Presentation
{
    // 설정 패널 — 그래픽/음향/조작/접근성 4탭. 값은 GameSettings(PlayerPrefs)로 저장·즉시 적용.
    // 소비처가 아직 없는 항목(음악·효과 볼륨, 감도, Y반전)은 저장만 되고 해당 시스템 구현 시 연결된다.
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [Header("탭")]
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private GameObject[] _tabPanels;

        [Header("그래픽")]
        [SerializeField] private Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private Dropdown _qualityDropdown;
        [SerializeField] private Toggle _vsyncToggle;

        [Header("음향")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("조작")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Toggle _invertYToggle;

        [Header("접근성")]
        [SerializeField] private Slider _shakeSlider;

        private Resolution[] _resolutions;

        private void Awake()
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int idx = i;
                _tabButtons[i].onClick.AddListener(() => ShowTab(idx));
            }

            if (_resolutionDropdown != null)
            {
                _resolutions = Screen.resolutions;
                var options = new List<string>();
                foreach (var r in _resolutions)
                    options.Add($"{r.width} x {r.height} @{r.refreshRateRatio.value:F0}");
                _resolutionDropdown.ClearOptions();
                _resolutionDropdown.AddOptions(options);
                _resolutionDropdown.value = _resolutions.Length - 1;
                _resolutionDropdown.onValueChanged.AddListener(i =>
                {
                    var r = _resolutions[Mathf.Clamp(i, 0, _resolutions.Length - 1)];
                    Screen.SetResolution(r.width, r.height, Screen.fullScreen);
                });
            }
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = Screen.fullScreen;
                _fullscreenToggle.onValueChanged.AddListener(v => GameSettings.Set(GameSettings.KeyFullscreen, v ? 1f : 0f));
            }
            if (_qualityDropdown != null)
            {
                _qualityDropdown.ClearOptions();
                _qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
                _qualityDropdown.value = QualitySettings.GetQualityLevel();
                _qualityDropdown.onValueChanged.AddListener(i => GameSettings.Set(GameSettings.KeyQuality, i));
            }
            if (_vsyncToggle != null)
            {
                _vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
                _vsyncToggle.onValueChanged.AddListener(v => GameSettings.Set(GameSettings.KeyVSync, v ? 1f : 0f));
            }

            Bind(_masterSlider, GameSettings.KeyMasterVolume, 1f);
            Bind(_musicSlider, GameSettings.KeyMusicVolume, 0.8f);
            Bind(_sfxSlider, GameSettings.KeySfxVolume, 1f);
            Bind(_sensitivitySlider, GameSettings.KeyMouseSensitivity, 1f);
            Bind(_shakeSlider, GameSettings.KeyShakeScale, 1f);
            if (_invertYToggle != null)
            {
                _invertYToggle.isOn = GameSettings.Get(GameSettings.KeyInvertY, 0f) > 0.5f;
                _invertYToggle.onValueChanged.AddListener(v => GameSettings.Set(GameSettings.KeyInvertY, v ? 1f : 0f));
            }

            ShowTab(0);
        }

        private static void Bind(Slider slider, string key, float fallback)
        {
            if (slider == null) return;
            slider.value = GameSettings.Get(key, fallback);
            slider.onValueChanged.AddListener(v => GameSettings.Set(key, v));
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < _tabPanels.Length; i++)
                _tabPanels[i].SetActive(i == index);
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var colors = _tabButtons[i].colors;
                colors.normalColor = i == index ? new Color(0.30f, 0.24f, 0.16f) : new Color(0.55f, 0.47f, 0.34f);
                _tabButtons[i].colors = colors;
            }
        }
    }
}
