using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 배달 목적지 — 맞는 화물을 지니고 접근 + E로 납품. 화물 상태에 비례해 조선통보를 준다.
    public sealed class DeliveryPoint : MonoBehaviour
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

        private bool CanDeliver =>
            _cargo != null && _cargo.IsCarrying && _cargo.ActiveMission == _mission;

        private void Update()
        {
            if (_cargo == null) return;
            _playerNear = Vector3.Distance(_cargo.transform.position, transform.position) <= _interactRange;
            if (!_playerNear || !CanDeliver || !Input.GetKeyDown(_interactKey)) return;

            int reward = _cargo.CompleteDelivery();
            if (reward > 0)
            {
                var wallet = FindFirstObjectByType<PlayerWallet>();
                if (wallet != null) wallet.Add(reward);
            }
        }

        private void OnGUI()
        {
            if (!_playerNear || !CanDeliver) return;
            GUI.Label(new Rect(10, 60, 420, 24), "E: 화물 납품");
        }
    }
}
