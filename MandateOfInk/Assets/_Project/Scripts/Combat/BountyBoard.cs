using System.Collections.Generic;
using MandateOfInk.Core.Events;
using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 사냥 방(榜) 게시판 — 거점에 세워두는 현상 의뢰판. 오픈월드 기조라 강제 게이트가 아니라
    // 들르면 조선통보 벌이가 되는 부가 루프다. 프로토 UI는 OnGUI(디제틱 게시판은 후속).
    // 흐름: 접근 + E = 수주 -> 대상 처치(적 처치 채널 집계) -> 게시판 복귀 + E = 보상 수령.
    public sealed class BountyBoard : MonoBehaviour
    {
        private enum SlotState { Available, Active, ReadyToClaim, Done }

        [SerializeField] private BountySO[] _bounties;
        [SerializeField] private StringEventChannelSO _enemyKilledChannel;
        [Header("[가정]")]
        [SerializeField] private float _interactRange = 3.5f;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        private readonly List<SlotState> _states = new List<SlotState>();
        private readonly List<int> _killCounts = new List<int>();
        private Transform _player;
        private bool _playerNear;

        private void Awake()
        {
            foreach (var _ in _bounties) { _states.Add(SlotState.Available); _killCounts.Add(0); }
        }

        private void Start()
        {
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) _player = cc.transform;
        }

        private void OnEnable()
        {
            if (_enemyKilledChannel != null) _enemyKilledChannel.OnRaised += OnEnemyKilled;
        }

        private void OnDisable()
        {
            if (_enemyKilledChannel != null) _enemyKilledChannel.OnRaised -= OnEnemyKilled;
        }

        private void OnEnemyKilled(string enemyId)
        {
            for (int i = 0; i < _bounties.Length; i++)
            {
                if (_states[i] != SlotState.Active || _bounties[i].TargetEnemyId != enemyId) continue;
                _killCounts[i]++;
                if (_killCounts[i] >= _bounties[i].TargetCount)
                {
                    _states[i] = SlotState.ReadyToClaim;
                    Debug.Log($"[방] 「{_bounties[i].DisplayName}」 목표 달성 — 게시판에서 보상 수령");
                }
            }
        }

        private void Update()
        {
            if (_player == null) return;
            _playerNear = Vector3.Distance(_player.position, transform.position) <= _interactRange;
            if (!_playerNear || !Input.GetKeyDown(_interactKey)) return;

            // 우선순위: 보상 수령 -> 신규 수주 (한 번의 E로 하나씩)
            for (int i = 0; i < _bounties.Length; i++)
            {
                if (_states[i] != SlotState.ReadyToClaim) continue;
                var wallet = FindFirstObjectByType<PlayerWallet>();
                if (wallet != null) wallet.Add(_bounties[i].RewardCoins);
                _states[i] = SlotState.Done;
                Debug.Log($"[방] 「{_bounties[i].DisplayName}」 완수 — 조선통보 {_bounties[i].RewardCoins} 수령");
                return;
            }
            for (int i = 0; i < _bounties.Length; i++)
            {
                if (_states[i] != SlotState.Available) continue;
                _states[i] = SlotState.Active;
                Debug.Log($"[방] 「{_bounties[i].DisplayName}」 수주 — {_bounties[i].Description}");
                return;
            }
        }

        // 프로토 표시 — 접근 시에만 게시판 내용을 띄운다(최소 HUD: 상호작용 프롬프트 허용 범위)
        private void OnGUI()
        {
            if (!_playerNear) return;
            var sb = new System.Text.StringBuilder("[사냥 방(榜)]  E: 수주/수령\n");
            for (int i = 0; i < _bounties.Length; i++)
            {
                string state = _states[i] switch
                {
                    SlotState.Available => "모집",
                    SlotState.Active => $"진행 {_killCounts[i]}/{_bounties[i].TargetCount}",
                    SlotState.ReadyToClaim => "완료 — 보상 수령 가능",
                    _ => "완수",
                };
                sb.AppendLine($"- {_bounties[i].DisplayName} ({state})  상금 {_bounties[i].RewardCoins}문");
            }
            GUI.Label(new Rect(10, 90, 460, 30 + _bounties.Length * 22), sb.ToString());
        }
    }
}
