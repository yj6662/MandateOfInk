using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 플레이어 체력 — 수치는 PlayerConfigSO(데이터). 사망 처리(조선통보 드롭·회수)는 M2.
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerConfigSO _config;

        public float CurrentHp { get; private set; }
        public event System.Action OnDamaged; // 피격 알림 (갈기 취소 등)
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
            if (Time.time < _invulnerableUntil)
            {
                Debug.Log("[Player] 무적 프레임 — 회피 성공");
                return;
            }
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            OnDamaged?.Invoke();
            Debug.Log($"[Player] 피해 {amount:F1} -> HP {CurrentHp:F1}");
            if (CurrentHp <= 0f)
                Debug.Log("[Player] 사망 — 사망 루프(드롭·회수)는 M2에서 구현");
        }

        // 최소 HUD 허용 항목: HP (프로토 임시 표시)
        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 300, 24), $"HP {CurrentHp:F0} / {MaxHp:F0}");
        }
    }
}
