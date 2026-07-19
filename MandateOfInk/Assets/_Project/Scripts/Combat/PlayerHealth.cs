using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 플레이어 체력 — 수치는 PlayerConfigSO(데이터). 사망 처리(조선통보 드롭·회수)는 M2.
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerConfigSO _config;

        public float CurrentHp { get; private set; }
        private float MaxHp => _config != null ? _config.MaxHp : 100f;

        private void Awake()
        {
            CurrentHp = MaxHp;
        }

        public void TakeDamage(float amount)
        {
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
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
