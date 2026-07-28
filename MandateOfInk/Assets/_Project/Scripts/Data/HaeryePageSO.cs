using UnityEngine;

namespace MandateOfInk.Data
{
    // 고서 낱장 — 세계관 수집 요소(사용자 결정 2026-07-27).
    // 해례본 제자해에서 시작해 주역 계사전·서경 홍범·태극도설 등 음양오행을 다룬
    // 조선 초기 유통 고서 전반으로 확장(2026-07-27). 클래스명은 첫 도입 시점의 잔재 — 후속 정리 후보.
    // 본문 텍스트는 실제 원전 구절 기반, 의역·발췌는 [가정] — 감수는 사람 영역.
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Haerye Page", fileName = "HP_NewPage")]
    public sealed class HaeryePageSO : ScriptableObject
    {
        [Tooltip("수집 목록 정렬 순서")]
        public int SortOrder;
        public string Title;
        [Tooltip("출전 서명 — 예: 訓民正音 解例本 / 周易 繫辭傳")]
        public string SourceBook;
        [Tooltip("관련 오행 — 중성 장 등 공통 내용은 None")]
        public Element RelatedElement = Element.None;

        [TextArea(2, 4)]
        [Tooltip("한문 원문(발췌)")]
        public string HanjaQuote;

        [TextArea(3, 8)]
        [Tooltip("한글 풀이")]
        public string KoreanText;

        [TextArea(2, 5)]
        [Tooltip("세계관 연결 — 작도 체계와 이 구절의 관계")]
        public string Lore;
    }
}
