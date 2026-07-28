using System.Reflection;
using MandateOfInk.Core.Events;
using StarterAssets;
using UnityEngine;

// 프로토타입 글루: 도약 채널(EC_PlayerLeap) 신호를 받아 StarterAssets FPC의 수직 속도를 직접 준다.
// FPC의 _verticalVelocity는 private이라 리플렉션으로 접근(프로토 한정 — M1 자체 컨트롤러 도입 시 정리).
public sealed class PlayerLeapGlue : MonoBehaviour
{
    [SerializeField] private FloatEventChannelSO _playerLeap;
    [SerializeField] private FirstPersonController _fpc;

    private static readonly FieldInfo VerticalVelocityField =
        typeof(FirstPersonController).GetField("_verticalVelocity", BindingFlags.NonPublic | BindingFlags.Instance);

    private void OnEnable()
    {
        if (_playerLeap != null) _playerLeap.OnRaised += HandleLeap;
    }

    private void OnDisable()
    {
        if (_playerLeap != null) _playerLeap.OnRaised -= HandleLeap;
    }

    private void HandleLeap(float velocity)
    {
        if (_fpc == null || VerticalVelocityField == null)
        {
            Debug.LogWarning("[Leap] FPC/필드 리플렉션 실패 — 도약 무시");
            return;
        }
        VerticalVelocityField.SetValue(_fpc, velocity);
    }
}
