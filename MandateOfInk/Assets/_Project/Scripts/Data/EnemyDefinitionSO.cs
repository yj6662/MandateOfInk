using Sirenix.OdinInspector;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 적 정의 DTO. 수치는 전부 [가정] 플레이스홀더 — 밸런스는 데이터에서만 조정한다.
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Enemy Definition", fileName = "EN_NewEnemy")]
    public sealed class EnemyDefinitionSO : SerializedScriptableObject
    {
        [Title("식별")]
        public string Id;
        [Tooltip("표기 명칭 — 관보식/민간 이중 명칭은 미결 사항")] public string DisplayName;

        [Title("속성·능력치")]
        public Element Element;
        [Tooltip("[가정]")] public float MaxHp = 100f;
        [Tooltip("[가정]")] public float MoveSpeed = 3f;
        [Tooltip("[가정]")] public float AttackDamage = 10f;
        [Tooltip("[가정] 그로기 한계치")] public float MaxPoise = 50f;
        [Tooltip("[가정] 처치 시 조선통보 — 통보는 화폐, 마석(먹)과 절대 분리")] public int CoinDrop = 15;

        [Title("공격 아키타입 (전투코어루프 §9)")]
        public EnemyArchetype Archetype = EnemyArchetype.RangedBasic;
        [Tooltip("[가정] 아키타입 주 수치 — 목:감속배율 / 화:폭발반경 / 금:참격반경 / 수:끌거리 / 토:돌진속도")]
        public float ArchetypePrimary = 1f;
        [Tooltip("[가정] 아키타입 부 수치 — 목:감속시간 / 화:퓨즈 / 금:미사용 / 수:끌시간 / 토:돌진시간")]
        public float ArchetypeSecondary = 1f;

        [Title("경화 순환 — 화마(MoltenCycler) 전용 [가정]")]
        [Tooltip("끓는(공격 가능) 시간")] public float MoltenSeconds = 4f;
        [Tooltip("굳은(정지·피해 급감) 시간")] public float HardenSeconds = 2f;
        [Tooltip("굳은 동안 받는 피해 배율")] [Range(0f, 1f)] public float HardenDamageMultiplier = 0.25f;

        [Title("텔레그래프 어휘 3종 (전투코어루프 v0.3 — 기준 0.8~1.2초)")]
        [Tooltip("[가정] 느린 예비 동작 — 강타. 받아치기 유도")]
        [Range(0.3f, 3f)] public float SlowTelegraphSeconds = 1.2f;
        [Tooltip("[가정] 빠른 예비 동작 — 회피 강제")]
        [Range(0.3f, 3f)] public float FastTelegraphSeconds = 0.8f;
        [Tooltip("[가정] 페인트 예비 동작 — 느린 것처럼 보이다가 멈칫")]
        [Range(0.3f, 3f)] public float FeintTelegraphSeconds = 0.7f;
        [Tooltip("[가정] 페인트 멈칫 길이 — 이 사이 성급한 회피를 처벌")]
        [Range(0.1f, 2f)] public float FeintPauseSeconds = 0.5f;
        [Tooltip("[가정] 느린 강타 피해 배율")] public float SlowDamageMultiplier = 1.6f;
        [Tooltip("[가정] 빠른 공격 피해 배율")] public float FastDamageMultiplier = 0.7f;
    }
}
