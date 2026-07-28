using MandateOfInk.Core.Events;
using MandateOfInk.Vehicle;
using StarterAssets;
using UnityEngine;

// 프로토타입 글루: 마석 자동차 탑승 채널 신호에 따라 StarterAssets 플레이어를 좌석에 붙이고 뗀다.
// StarterAssets는 asmdef 없이 Assembly-CSharp 소속이라 Vehicle 모듈에서 직접 참조하지 않고
// 이 글루가 이벤트 채널을 구독해 중계한다(FirstPersonControllerModeGlue와 같은 결).
// 탑승 중에는 FPC·CharacterController를 끄고 플레이어를 좌석에 부착, 마우스 룩은 이 글루가
// 카메라 타깃을 직접 돌려 대신한다(요 ±120도 / 피치 ±40도 [가정]).
public sealed class VehicleMountGlue : MonoBehaviour
{
    [SerializeField] private BoolEventChannelSO _mountChanged;
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private FirstPersonController _fpc;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private StarterAssetsInputs _inputs;
    [Tooltip("StarterAssets CinemachineCameraTarget — 탑승 중 룩 회전 대상")]
    [SerializeField] private Transform _cameraTarget;

    [Header("[가정] 탑승 중 룩 제한(도)")]
    [SerializeField] private float _lookYawLimit = 120f;
    [SerializeField] private float _lookPitchLimit = 40f;
    [SerializeField] private float _lookSensitivity = 1f;

    private bool _mounted;
    private float _lookYaw;
    private float _lookPitch;

    private void OnEnable()
    {
        if (_mountChanged != null) _mountChanged.OnRaised += HandleMountChanged;
    }

    private void OnDisable()
    {
        if (_mountChanged != null) _mountChanged.OnRaised -= HandleMountChanged;
    }

    private void HandleMountChanged(bool mounted)
    {
        if (_vehicle == null || _fpc == null || _characterController == null) return;
        _mounted = mounted;

        if (mounted)
        {
            _fpc.enabled = false;
            _characterController.enabled = false;
            _fpc.transform.SetParent(_vehicle.Seat, false);
            _fpc.transform.localPosition = Vector3.zero;
            _fpc.transform.localRotation = Quaternion.identity;
            _lookYaw = 0f;
            _lookPitch = 0f;
        }
        else
        {
            _fpc.transform.SetParent(null);
            // 차 왼쪽으로 내린다 — 차체가 어느 방향이든 지면 높이는 차 바닥 기준
            var cfg = _vehicle.Config;
            float side = cfg != null ? cfg.DismountSideOffset : 2.2f;
            Vector3 exit = _vehicle.transform.position - _vehicle.transform.right * side;
            exit.y = _vehicle.transform.position.y + 0.1f;
            _fpc.transform.position = exit;
            _fpc.transform.rotation = Quaternion.Euler(0f, _vehicle.transform.eulerAngles.y, 0f);
            if (_cameraTarget != null) _cameraTarget.localRotation = Quaternion.identity;
            _characterController.enabled = true;
            _fpc.enabled = true;
        }
    }

    private void LateUpdate()
    {
        // 탑승 중 마우스 룩 — FPC가 꺼져 있으므로 카메라 타깃을 직접 돌린다(차체 회전에 얹힘)
        if (!_mounted || _cameraTarget == null || _inputs == null) return;
        _lookYaw = Mathf.Clamp(_lookYaw + _inputs.look.x * _lookSensitivity * 0.12f, -_lookYawLimit, _lookYawLimit);
        _lookPitch = Mathf.Clamp(_lookPitch + _inputs.look.y * _lookSensitivity * 0.12f, -_lookPitchLimit, _lookPitchLimit);
        _cameraTarget.localRotation = Quaternion.Euler(_lookPitch, _lookYaw, 0f);
    }
}
