using UnityEngine;

namespace MandateOfInk.Combat
{
    // 조선통보 지갑 — 화폐. 마석(먹)과 절대 분리(절대 규칙).
    // 적 처치로 얻고, 사망 시 전액을 그 자리에 떨어뜨린다(회수 가능, 재사망 시 소실).
    public sealed class PlayerWallet : MonoBehaviour
    {
        public int Coins { get; private set; }

        /// <summary>잔액 변동 알림(증감량) — HUD 팽창 연출 등이 구독.</summary>
        public event System.Action<int> OnChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Debug.Log($"[통보] +{amount} -> {Coins}");
            OnChanged?.Invoke(amount);
        }

        // 사망 드롭용 — 전액을 꺼내고 0으로 만든다
        public int TakeAll()
        {
            int taken = Coins;
            Coins = 0;
            if (taken > 0) OnChanged?.Invoke(-taken);
            return taken;
        }
    }
}
