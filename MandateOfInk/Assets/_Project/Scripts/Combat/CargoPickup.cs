using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 화물 수령처 — 접근 + E로 배달 미션 화물을 받는다. 배달 완료 후 다시 받을 수 있다(반복 벌이).
    public sealed class CargoPickup : MonoBehaviour
    {
        [SerializeField] private DeliveryMissionSO _mission;
        [Header("[가정]")]
        [SerializeField] private float _interactRange = 3.5f;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        private PlayerCargo _cargo;
        private bool _playerNear;

        private void Start()
        {
            useGUILayout = false; // OnGUI 레이아웃 패스 생략
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) _cargo = cc.GetComponent<PlayerCargo>();
        }

        private void Update()
        {
            if (_cargo == null) return;
            _playerNear = Vector3.Distance(_cargo.transform.position, transform.position) <= _interactRange;
            if (_playerNear && !_cargo.IsCarrying && Input.GetKeyDown(_interactKey))
                _cargo.TryPickUp(_mission);
        }

        private void OnGUI()
        {
            if (!_playerNear || _cargo == null || _cargo.IsCarrying) return;
            GUI.Label(new Rect(10, 60, 420, 24),
                $"E: 화물 수령 — {(_mission != null ? _mission.DisplayName : "?")}");
        }
    }
}
