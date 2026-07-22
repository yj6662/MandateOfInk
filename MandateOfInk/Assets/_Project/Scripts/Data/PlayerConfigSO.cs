using UnityEngine;

namespace MandateOfInk.Data
{
    // 플레이어 기본 수치. 전부 [가정] — M1 실측으로 확정.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfigSO : ScriptableObject
    {
        [Tooltip("[가정]")] public float MaxHp = 100f;

        [Header("사망 루프 — 조선통보 드롭·회수 (M2)")]
        [Tooltip("[가정] 부활 직후 무적(초)")]
        public float RespawnInvulnerableSeconds = 2f;
        [Tooltip("[가정] 떨어진 통보 회수 반경(m)")]
        public float CoinPickupRadius = 1.6f;
    }
}
