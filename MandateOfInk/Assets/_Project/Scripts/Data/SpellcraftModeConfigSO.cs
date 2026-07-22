using UnityEngine;

namespace MandateOfInk.Data
{
    // 작도 모드 전환 설정. 수치는 전부 [가정] 플레이스홀더 — 손맛 튜닝은 사람 영역.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Spellcraft Mode Config", fileName = "SpellcraftModeConfig")]
    public sealed class SpellcraftModeConfigSO : ScriptableObject
    {
        [Header("전환 입력")]
        [Tooltip("[가정] 작도 홀드 키 — 누르는 동안 작도, 떼면 판정. M1에서 Input System 액션으로 이관 예정")]
        public KeyCode ToggleKey = KeyCode.Q;

        [Header("시간 감속")]
        [Tooltip("[가정] 작도 중 시간 배율 (1 = 정상 속도)")]
        [Range(0.05f, 1f)] public float DrawingTimeScale = 0.2f;

        [Header("[가정] 약발동 — 완전 불발 금지(절대 규칙), 임계 미달 시 위력만 깎는다")]
        [Tooltip("자모당 인식 거리가 이 값을 넘으면 약발동")]
        public float WeakCastDistanceThreshold = 1.8f;
        [Tooltip("약발동 위력 배율")]
        [Range(0f, 1f)] public float WeakCastPowerMultiplier = 0.5f;

        [Header("[가정] 획 품질 3축 — 형태·구조·속도 (D13, 축 정의는 [제안])")]
        [Tooltip("형태축: 최악 자모 거리가 이 값 이하면 만점")]
        public float FormPerfectDistance = 0.8f;
        [Tooltip("구조축: 자모 평균 거리가 이 값 이하면 만점 (임계는 약발동 값 공유)")]
        public float StructurePerfectDistance = 0.7f;
        [Tooltip("속도축: 기대 시간 = 기본 + 획당 가산 (실제 경과가 기대 이하면 만점)")]
        public float SpeedBaseSeconds = 0.3f;
        public float SpeedPerStrokeSeconds = 0.18f;
        [Tooltip("기대 시간의 이 배수에서 속도 점수 0")]
        public float SpeedZeroMultiplier = 2.5f;
        [Tooltip("축 가중치 (형태/구조/속도) — 합으로 정규화된다")]
        public Vector3 QualityWeights = new Vector3(0.5f, 0.25f, 0.25f);
        [Tooltip("품질 0 -> 1일 때 위력 배율 범위 — 정확할수록 보상(전투코어루프 §3)")]
        public float QualityPowerMin = 0.85f;
        public float QualityPowerMax = 1.1f;
    }
}
