using MandateOfInk.Core.Events;
using StarterAssets;
using UnityEngine;

// 프로토타입 글루: 해례본 서책(인벤토리) 열림 채널에 따라 커서를 풀고 시점 회전을 끊는다.
// FirstPersonControllerModeGlue(작도 모드)와 같은 결 — StarterAssets 직접 참조는 글루에서만.
public sealed class CodexCursorGlue : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO _codexOpened;
    [SerializeField] private StarterAssetsInputs _inputs;

    private void OnEnable()
    {
        if (_codexOpened != null) _codexOpened.OnRaised += HandleOpened;
    }

    private void OnDisable()
    {
        if (_codexOpened != null) _codexOpened.OnRaised -= HandleOpened;
    }

    private void HandleOpened(bool open)
    {
        if (_inputs == null) return;
        if (open)
        {
            _inputs.LookInput(Vector2.zero);
            _inputs.MoveInput(Vector2.zero);
            _inputs.cursorLocked = false;
            _inputs.cursorInputForLook = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            _inputs.cursorLocked = true;
            _inputs.cursorInputForLook = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
