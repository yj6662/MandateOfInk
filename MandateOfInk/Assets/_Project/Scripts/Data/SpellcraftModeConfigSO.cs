using UnityEngine;

namespace MandateOfInk.Data
{
    // 작도 모드 전환 설정. 수치는 전부 [가정] 플레이스홀더 — 손맛 튜닝은 사람 영역.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Spellcraft Mode Config", fileName = "SpellcraftModeConfig")]
    public sealed class SpellcraftModeConfigSO : ScriptableObject
    {
        [Header("전환 입력")]
        [Tooltip("[가정] 작도 모드 토글 키 — M1에서 Input System 액션으로 이관 예정")]
        public KeyCode ToggleKey = KeyCode.Q;

        [Header("시간 감속")]
        [Tooltip("[가정] 작도 중 시간 배율 (1 = 정상 속도)")]
        [Range(0.05f, 1f)] public float DrawingTimeScale = 0.2f;
    }
}
