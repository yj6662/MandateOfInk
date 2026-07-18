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

        [Title("텔레그래프")]
        [Tooltip("[가정] 예비 동작 시간(초) — 기준 0.8~1.2")]
        [Range(0.3f, 3f)] public float TelegraphSeconds = 1f;
    }
}
