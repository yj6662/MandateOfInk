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

    // 음양 — 중성 모음이 결정한다. 양성모음(ㅏㅗㅑㅛ…)=양, 음성모음(ㅓㅜㅕㅠ…)=음, ㅣㅡ=특수.
    public enum Polarity
    {
        Yang,    // 양 — 발산·공세
        Yin,     // 음 — 수렴·방어/지속
        Special, // 특수 (ㅣ·ㅡ)
    }

    // 종성(받침) 변조. 없음=빠른 기본형.
    public enum FinalModifier
    {
        None,    // 받침 없음 — 빠른 기본형
        Area,    // ㅇ — 광역
        Sustain, // ㄴㄹ — 지속·연쇄
        Amplify, // ㅁㅂ — 강화
        Pierce,  // ㅅ — 관통
        Bind,    // ㄱ — 속박
    }
}
