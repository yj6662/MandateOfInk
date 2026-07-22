using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 오행 상생상극 5x5 테이블 (Odin Dictionary 직렬화).
    // 값 = 공격 속성 -> 방어 속성 대미지 배율. 전투 3단계 판정과 대응:
    // 우세(내가 상대를 극함) / 중립 / 열세(상대가 나를 극함). 배율 수치는 [가정].
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Element Relation Table", fileName = "ElementRelationTable")]
    public sealed class ElementRelationTableSO : SerializedScriptableObject
    {
        [Tooltip("[가정] 우세 1.5 / 중립 1.0 / 열세 0.5")]
        public Dictionary<Element, Dictionary<Element, float>> Multiplier =
            new Dictionary<Element, Dictionary<Element, float>>();

        public float GetMultiplier(Element attacker, Element defender)
        {
            if (Multiplier != null
                && Multiplier.TryGetValue(attacker, out var row)
                && row != null
                && row.TryGetValue(defender, out float value))
                return value;
            return 1f; // 테이블에 없으면 중립
        }
    }
}
