using MandateOfInk.Data;
using MandateOfInk.Spellcraft;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 사망 루프(M2) — 소울라이크 문법:
    //   사망 -> 소지 통보 전액을 그 자리에 드롭 -> 부활 지점으로 복귀(HP·먹 회복 + 짧은 무적)
    //   -> 드롭 지점을 다시 밟으면 회수. 회수 전에 또 죽으면 이전 드롭은 소실된다.
    // 통보(화폐)만 떨어진다 — 마석(먹)은 연료라 사망과 무관(절대 규칙).
    public sealed class DeathRespawn : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerWallet _wallet;
        [SerializeField] private InkPool _inkPool;
        [SerializeField] private PlayerConfigSO _config;
        [SerializeField] private Transform _respawnPoint;

        private CharacterController _controller;
        private DroppedCoins _currentDrop; // 필드에 남아 있는 유일한 드롭

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDied += HandleDeath;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= HandleDeath;
        }

        private void HandleDeath()
        {
            // 1) 통보 드롭 — 이전 드롭이 남아 있었다면 소실
            if (_currentDrop != null)
            {
                Debug.Log($"[사망] 회수하지 못한 통보 {_currentDrop.Amount} 소실");
                Destroy(_currentDrop.gameObject);
            }
            int coins = _wallet != null ? _wallet.TakeAll() : 0;
            if (coins > 0)
            {
                _currentDrop = DroppedCoins.Spawn(transform.position, coins,
                    _config != null ? _config.CoinPickupRadius : 1.6f);
                Debug.Log($"[사망] 통보 {coins} 드롭 — 회수하러 돌아가라");
            }

            // 2) 부활 지점으로 복귀 (CharacterController는 끄고 옮겨야 순간이동이 된다)
            Vector3 target = _respawnPoint != null ? _respawnPoint.position : Vector3.up;
            if (_controller != null) _controller.enabled = false;
            transform.position = target;
            if (_controller != null) _controller.enabled = true;

            // 3) 회복 + 짧은 무적
            float invuln = _config != null ? _config.RespawnInvulnerableSeconds : 2f;
            if (_health != null) _health.ResetFull(invuln);
            if (_inkPool != null) _inkPool.Add(float.MaxValue); // 도착하면 풀 — 벼루 원리 그대로
            Debug.Log("[사망] 부활 — 광장으로 복귀");
        }
    }
}
