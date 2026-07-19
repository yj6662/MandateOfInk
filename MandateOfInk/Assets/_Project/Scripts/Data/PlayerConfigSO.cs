using UnityEngine;

namespace MandateOfInk.Data
{
    // 플레이어 기본 수치. 전부 [가정] — M1 실측으로 확정.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfigSO : ScriptableObject
    {
        [Tooltip("[가정]")] public float MaxHp = 100f;
    }
}
