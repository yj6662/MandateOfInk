namespace MandateOfInk.Data
{
    // 오행 속성 — 초성 자모가 결정한다. 아음(ㄱㅋ)=목, 설음(ㄴㄷㅌㄹ)=화, 순음(ㅁㅂㅍ)=토, 치음(ㅅㅈㅊ)=금, 후음(ㅇㅎ)=수.
    public enum Element
    {
        Wood,  // 목
        Fire,  // 화
        Earth, // 토
        Metal, // 금
        Water, // 수
    }

    // 음양 — 중성의 ㆍ 방향이 결정한다. 양성(ㅏㅗ)=양(발산·공세), 음성(ㅓㅜ)=음(수렴·방어/설치).
    public enum Polarity
    {
        Yang,    // 양 — 발산·공세
        Yin,     // 음 — 수렴·방어/지속
        Special, // 특수 (ㅣ·ㅡ) — 후속 확장
    }

    // 천지인(범위) — 중성의 기둥이 결정한다. ㅏㅓ(ㅣ사람)=단일, ㅗㅜ(ㅡ땅)=영역.
    public enum Scope
    {
        Single, // 단일 대상
        Area,   // 광역·영역
    }

    // 종성(받침) 거동 — 작도 글자 일람 v1.2 기준. 속성별 대표 자음 하나씩.
    public enum FinalModifier
    {
        None,    // 받침 없음 — 빠른 기본형
        Bind,    // ㄱ(목) — 속박: 적중 대상을 묶는다
        Sustain, // ㄴ(화) — 지속: 장(場)으로 남아 시간에 걸쳐 작용
        Trigger, // ㅁ(토) — 격발: 장전 설치, 양 진이 닿으면 격발(상합)
        Pierce,  // ㅅ(금) — 관통: 방어·다수를 뚫고 직선 진행
        Chain,   // ㅇ(수) — 연쇄: 인접 대상으로 순차 전파
    }

    // 분류 태그 — 일람의 「분류」 열.
    public enum DiagramCategory
    {
        Attack,         // 공격
        DefenseControl, // 방어·제어
        TriggerInstall, // 상합 설치
    }
}
