using UnityEngine;

namespace MandateOfInk.Combat
{
    // 사망 시 떨어진 조선통보 더미 — 닿으면 회수. 다음 사망까지 필드에 하나만 존재한다.
    // 표현: 청동빛 반투명 엽전 더미 + 「통보」 글자 (기존 프리미티브+글자 문법).
    public sealed class DroppedCoins : MonoBehaviour
    {
        public int Amount { get; private set; }

        private PlayerWallet _wallet; // 회수 시 지연 조회

        public static DroppedCoins Spawn(Vector3 position, int amount, float pickupRadius)
        {
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Cylinder,
                new Color(0.55f, 0.42f, 0.2f, 0.55f), new Vector3(0.5f, 0.06f, 0.5f), keepColliderAsTrigger: true);
            go.name = "DroppedCoins";
            position.y = 0.1f;
            go.transform.position = position;
            var col = (CapsuleCollider)go.GetComponent<Collider>();
            col.radius = Mathf.Max(pickupRadius / go.transform.localScale.x, 1f); // 회수 판정 반경
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            SpellVisuals.AttachLetter(go.transform, "통보", 0.35f, new Color(0.95f, 0.85f, 0.5f, 0.95f));

            var drop = go.AddComponent<DroppedCoins>();
            drop.Amount = amount;
            return drop;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() == null) return;
            if (_wallet == null) _wallet = other.GetComponentInParent<PlayerWallet>();
            if (_wallet == null) _wallet = FindFirstObjectByType<PlayerWallet>();
            if (_wallet == null) return;

            _wallet.Add(Amount);
            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.5f,
                new Color(0.95f, 0.85f, 0.5f, 0.6f), 1.4f, 0.35f);
            Debug.Log($"[통보] 드롭 회수 +{Amount}");
            Destroy(gameObject);
        }
    }
}
