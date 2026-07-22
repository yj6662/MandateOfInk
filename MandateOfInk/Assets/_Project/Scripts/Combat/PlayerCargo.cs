using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 플레이어의 화물 상태 — 배달 도사의 짐. 피격당할수록 상한다(화물 상태는 최소 HUD 허용 항목).
    // 사망하면 화물은 부서진다(배달 실패) — 사망 루프의 무게를 배달에도 싣는다 [가정].
    public sealed class PlayerCargo : MonoBehaviour
    {
        public DeliveryMissionSO ActiveMission { get; private set; }
        public float Condition { get; private set; } // 0~100

        private PlayerHealth _health;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDamaged += OnDamaged;
                _health.OnDied += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDamaged -= OnDamaged;
                _health.OnDied -= OnDied;
            }
        }

        public bool IsCarrying => ActiveMission != null;

        public bool TryPickUp(DeliveryMissionSO mission)
        {
            if (IsCarrying || mission == null) return false;
            ActiveMission = mission;
            Condition = 100f;
            Debug.Log($"[배달] 「{mission.DisplayName}」 화물 수령 — {mission.Description}");
            return true;
        }

        // 배달 완료 — 화물 상태에 비례한 보상을 계산해 돌려주고 짐을 내린다
        public int CompleteDelivery()
        {
            if (!IsCarrying) return 0;
            var m = ActiveMission;
            float fraction = Mathf.Lerp(m.MinRewardFraction, 1f, Condition / 100f);
            int reward = Mathf.RoundToInt(m.BaseRewardCoins * fraction);
            Debug.Log($"[배달] 「{m.DisplayName}」 완료 — 상태 {Condition:F0}%, 보상 {reward}문");
            ActiveMission = null;
            return reward;
        }

        private void OnDamaged()
        {
            if (!IsCarrying) return;
            Condition = Mathf.Max(0f, Condition - ActiveMission.ConditionLossPerHit);
        }

        private void OnDied()
        {
            if (!IsCarrying) return;
            Debug.Log($"[배달] 「{ActiveMission.DisplayName}」 실패 — 화물이 부서졌다");
            ActiveMission = null;
        }

        // 화물 상태 표시는 HudController(캔버스)가 담당한다
    }
}
