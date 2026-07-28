using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 플레이어 체력 — 수치는 PlayerConfigSO(데이터). 사망 처리(조선통보 드롭·회수)는 M2.
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerConfigSO _config;
        [Tooltip("디버그 — 켜면 피해를 받지 않는다(피격 알림은 유지). 테스트 전용")]
        [SerializeField] private bool _godMode;

        public float CurrentHp { get; private set; }
        public float MaxHpValue => MaxHp; // HUD 게이지용
        public bool IsDead { get; private set; }
        public event System.Action OnDamaged; // 피격 알림 (갈기 취소 등)
        public event System.Action OnDied;    // 사망 알림 — 사망 루프(DeathRespawn)가 받는다
        private float MaxHp => _config != null ? _config.MaxHp : 100f;
        private float _invulnerableUntil;

        private void Awake()
        {
            CurrentHp = MaxHp;
        }

        // 회피 무적 프레임
        public void SetInvulnerable(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return; // 죽은 뒤 추가 피격 무시 (사망 스팸 방지)
            if (_godMode)
            {
                OnDamaged?.Invoke(); // 피격 반응(갈기 취소 등)은 살려서 감각 테스트 유지
                Debug.Log($"[Player] 무적(디버그) — 피해 {amount:F1} 무시");
                return;
            }
            if (Time.time < _invulnerableUntil)
            {
                Debug.Log("[Player] 무적 프레임 — 회피 성공");
                return;
            }
            amount *= PlayerBuffLookup.DefenseMultiplier; // 소환수 방어력 버프(1보다 작으면 경감)
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            OnDamaged?.Invoke();
            Debug.Log($"[Player] 피해 {amount:F1} -> HP {CurrentHp:F1}");
            if (CurrentHp <= 0f)
            {
                IsDead = true;
                Debug.Log("[Player] 사망");
                OnDied?.Invoke();
            }
        }

        // 부활 — 사망 루프(DeathRespawn)가 호출
        public void ResetFull(float invulnerableSeconds)
        {
            CurrentHp = MaxHp;
            IsDead = false;
            SetInvulnerable(invulnerableSeconds);
        }

        // 지속 회복(초당) — 버프 소환수(수 속성 등)가 매 프레임 호출한다.
        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        // HP 표시는 HudController(캔버스 먹획 게이지)가 담당한다
    }
}
