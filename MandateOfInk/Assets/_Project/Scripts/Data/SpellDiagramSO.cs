using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 도면(진) 정의 — 한 글자 = 하나의 진. 작도는 한글이다(추상 도형 금지).
    // 초성=오행, 중성=음양, 종성=변조. 글자 일람 v1.2가 원본 데이터.
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Spell Diagram", fileName = "SD_NewDiagram")]
    public sealed class SpellDiagramSO : SerializedScriptableObject
    {
        [Title("글자")]
        [Tooltip("완성 글자 (예: 다)")] public string Letter;
        [Tooltip("초성 (예: ㄷ)")] public string Initial;
        [Tooltip("중성 (예: ㅏ)")] public string Medial;
        [Tooltip("종성 — 없으면 빈 문자열")] public string Final;

        [Title("파생 속성 (자모에서 유도)")]
        public Element Element;          // 초성 = 오행
        public Polarity Polarity;        // 중성 = 음양
        public Scope Scope;              // 중성 = 천지인(단일/영역)
        public FinalModifier Modifier;   // 종성 = 거동
        public DiagramCategory Category; // 분류 태그

        [Title("효과 설명 (일람 원문)")]
        [TextArea] public string Description;

        [Title("발동")]
        [Tooltip("[가정] 먹(마석) 소비량 — 마석은 연료이지 화폐가 아님")]
        public float InkCost = 10f;

        [Tooltip("효과 파라미터 — 추상 기반 클래스 다형성(Odin 직렬화)")]
        public List<EffectParam> Effects = new List<EffectParam>();
    }
}
