using UnityEngine;
using UnityEngine.SceneManagement;

namespace MandateOfInk.Presentation
{
    // 메인 메뉴 — 시작/이어하기/설정/크레딧/종료. 임시 UI(정식 룩은 셰이더 에셋 도입 후).
    // 이어하기는 세이브 시스템 도입 전까지 비활성. 버튼 배선은 에디터 스크립트가 한다.
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _creditsPanel;

        private void Start()
        {
            GameSettings.ApplyAll();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_creditsPanel != null) _creditsPanel.SetActive(false);
        }

        public void OnStart() => SceneManager.LoadScene("InGame");
        public void OnContinue() { /* 세이브 시스템 도입 후 연결 */ }
        public void OnOpenSettings() { if (_settingsPanel != null) _settingsPanel.SetActive(true); }
        public void OnOpenCredits() { if (_creditsPanel != null) _creditsPanel.SetActive(true); }
        public void OnCloseSubPanels()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_creditsPanel != null) _creditsPanel.SetActive(false);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
