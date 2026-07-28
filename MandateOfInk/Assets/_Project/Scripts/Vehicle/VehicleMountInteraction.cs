using MandateOfInk.Core.Events;
using UnityEngine;

namespace MandateOfInk.Vehicle
{
    // 마석 자동차 탑승/하차 — 접근 + E로 토글(기존 화물 수령 패턴).
    // 상태 변화는 SO 이벤트 채널로 발신하고, 플레이어 쪽 처리(FPC 끄기·좌석 부착)는
    // VehicleMountGlue(ProtoGlue)가 구독해 중계한다(StarterAssets 직접 참조 금지 규칙).
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleMountInteraction : MonoBehaviour
    {
        [SerializeField] private BoolEventChannelSO _mountChanged;
        [Header("[가정]")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        private VehicleController _vehicle;
        private Transform _player;
        private bool _playerNear;
        private float _toggleCooldownUntil;

        private void Awake()
        {
            _vehicle = GetComponent<VehicleController>();
            useGUILayout = false; // OnGUI 레이아웃 패스 생략
        }

        private void Start()
        {
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) _player = cc.transform; // 탑승 중 CC가 꺼져도 쓸 수 있게 트랜스폼을 캐싱
        }

        private void Update()
        {
            if (_player == null || _vehicle.Config == null) return;

            bool driven = _vehicle.State == VehicleController.VehicleState.Driven;
            _playerNear = driven ||
                Vector3.Distance(_player.position, transform.position) <= _vehicle.Config.MountRange;

            if (_playerNear && Time.time >= _toggleCooldownUntil && Input.GetKeyDown(_interactKey))
            {
                _toggleCooldownUntil = Time.time + 0.3f;
                bool mounting = !driven;
                _vehicle.SetDriven(mounting);
                if (_mountChanged != null) _mountChanged.Raise(mounting);
                else Debug.LogWarning("[Vehicle] _mountChanged 채널 미배선 — 플레이어 제어 전환이 되지 않는다");
            }
        }

        private void OnGUI()
        {
            if (!_playerNear) return;
            bool driven = _vehicle.State == VehicleController.VehicleState.Driven;
            GUI.Label(new Rect(10, 84, 420, 24), driven ? "E: 하차" : "E: 탑승 — 마석 자동차");
        }
    }
}
