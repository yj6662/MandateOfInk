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
        None,  // 무속성 — 기관술 병기(마석 아닌 압력·태엽 구동). 상극 테이블 밖 = 항상 중립 1.0,
               // 따라서 받아치기(상극 우세)가 성립하지 않는다. 봉인·물리 대응 요구(세계관설정집 §마물)
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

    // 적 공격 아키타입 — 속성이 데미지 타입이 아니라 「공격 동사」를 바꾼다(전투코어루프 §9).
    public enum EnemyArchetype
    {
        RangedBasic,  // 기본 원거리 — 텔레그래프 3종(느린/빠른/페인트) 투사체
        WoodBinder,   // 목 — 속박탄: 명중 시 이동 감속(덩굴)
        FireBomber,   // 화 — 지면 폭발: 발밑 예고 장판
        MetalSlasher, // 금 — 근접 쾌속 참격: 잽·연타
        WaterPuller,  // 수 — 끌물결: 명중 시 시전자 쪽으로 끌어당김
        EarthCharger, // 토 — 중장 돌진: 느리고 무겁게 밀어붙인다
        MoltenCycler, // 화마 전용 — 지면 폭발 + 경화(굳음: 정지·피해 급감)↔용융(끓음: 공격) 순환
        SwarmBurster, // 도깨비불 — 부유 표류 무리, 접근하면 달라붙어 작은 파열(자폭). 흩어짐 내구
        Thief,        // 앙괭이 — 공격하지 않는 도둑. 떨어진 통보를 집어 도주, 처치 시 전량 회수+웃돈
    }

    // 분류 태그 — 일람의 「분류」 열.
    public enum DiagramCategory
    {
        Attack,         // 공격
        DefenseControl, // 방어·제어
        TriggerInstall, // 상합 설치
    }
}
