using UnityEngine;

namespace MandateOfInk.Combat
{
    // 조선통보 지갑 — 화폐. 마석(먹)과 절대 분리(절대 규칙).
    // 적 처치로 얻고, 사망 시 전액을 그 자리에 떨어뜨린다(회수 가능, 재사망 시 소실).
    public sealed class PlayerWallet : MonoBehaviour
    {
        public int Coins { get; private set; }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Debug.Log($"[통보] +{amount} -> {Coins}");
        }

        // 사망 드롭용 — 전액을 꺼내고 0으로 만든다
        public int TakeAll()
        {
            int taken = Coins;
            Coins = 0;
            return taken;
        }

        // 임시 표시 — 통보는 최소 HUD 허용 목록에 없어 정식 표시는 미결(차패 상태창 후보).
        // 프로토 디버그 용도로만 띄운다.
        private void OnGUI()
        {
            GUI.Label(new Rect(10, 58, 300, 24), $"통보 {Coins}");
        }
    }
}
