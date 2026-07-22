using MandateOfInk.Core.Events;
using StarterAssets;
using UnityEngine;

// 프로토타입 글루: 작도 모드 채널 신호에 따라 StarterAssets 컨트롤러의 입력·커서를 잠그고 푼다.
// StarterAssets는 asmdef 없이 Assembly-CSharp 소속이라 본 모듈(asmdef)에서 직접 참조할 수 없다.
// 그래서 같은 Assembly-CSharp에 속한 이 글루가 이벤트 채널을 구독해 중계한다. (M1 자체 컨트롤러로 대체 예정)
public sealed class FirstPersonControllerModeGlue : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO _drawingModeChanged;
    [SerializeField] private StarterAssetsInputs _inputs;

    private void OnEnable()
    {
        if (_drawingModeChanged != null) _drawingModeChanged.OnRaised += HandleDrawingMode;
    }

    private void OnDisable()
    {
        if (_drawingModeChanged != null) _drawingModeChanged.OnRaised -= HandleDrawingMode;
    }

    private void Start()
    {
        // StarterAssets는 OnApplicationFocus에서만 커서를 잠그는데, 부트 씬을 거쳐 스폰되면
        // 그 이벤트를 놓친다. 게임은 항상 교전 모드로 시작하므로 여기서 명시적으로 잠근다.
        HandleDrawingMode(false);
    }

    private void HandleDrawingMode(bool drawing)
    {
        if (drawing)
        {
            // 시점 회전만 끊고 커서를 풀어 마우스를 붓으로 쓴다.
            // 이동(WASD)은 살려 둔다 — 느려진 시간 속에서 움직이며 작도 가능(사용자 결정).
            _inputs.LookInput(Vector2.zero);
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
