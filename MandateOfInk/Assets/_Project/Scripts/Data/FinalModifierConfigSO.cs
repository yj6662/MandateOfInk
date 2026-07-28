using UnityEngine;

namespace MandateOfInk.Data
{
    // 종성(받침) 거동 수치 — 전부 [가정], 손맛 튜닝으로 확정.
    // 격발(상합) 판정 매트릭스는 문서 미결 — 현재는 「아무 술식 적중 시 격발」 단순 규칙이며,
    // 5x5 상합 매트릭스가 확정되면 이 SO에 테이블로 추가한다.
    [CreateAssetMenu(menuName = "MandateOfInk/Config/Final Modifier Config", fileName = "FinalModifierConfig")]
    public sealed class FinalModifierConfigSO : ScriptableObject
    {
        [Header("ㄱ(목) 속박 — 단일 대상 행동 방해. 오행별 방해 방식 차별화(사용자 결정 2026-07-24)")]
        [Tooltip("기본값(오행 배열 미배선 시 폴백) — 속박 중 이동 속도 배율")]
        [Range(0f, 1f)] public float BindMoveMultiplier = 0.25f;
        public float BindSeconds = 2.5f;
        [Tooltip("오행별 속박 방해 방식. 5개(목/화/토/금/수) 모두 채워야 한다 — 비면 위 기본값으로 폴백")]
        public BindElementConfig[] BindByElement = new BindElementConfig[5]
        {
            new BindElementConfig { Element = Element.Wood,  MoveMultiplier = 0.25f, Seconds = 2.5f }, // 목=표준 이동 감속(원형)
            new BindElementConfig { Element = Element.Fire,  MoveMultiplier = 0.35f, Seconds = 2f,
                TelegraphMultiplier = 1.5f }, // 화=이동 감속+공격(예비 동작)도 느려짐
            new BindElementConfig { Element = Element.Earth, MoveMultiplier = 0.15f, Seconds = 2.5f,
                TelegraphMultiplier = 1.6f }, // 토=강한 이동 감속+공격도 느려짐(전신 방해)
            new BindElementConfig { Element = Element.Metal, MoveMultiplier = 0.5f, Seconds = 2f,
                SustainTickFraction = 0.15f, SustainTickInterval = 0.5f, SustainSeconds = 2f }, // 금=약한 속박+지속피해
            new BindElementConfig { Element = Element.Water, MoveMultiplier = 0.5f, Seconds = 2.5f,
                AimJitterDegrees = 12f }, // 수=약한 속박+조준 흔들림
        };

        public BindElementConfig GetBindConfig(Element element)
        {
            if (BindByElement != null)
                foreach (var b in BindByElement)
                    if (b.Element == element) return b;
            return new BindElementConfig { Element = element, MoveMultiplier = BindMoveMultiplier, Seconds = BindSeconds };
        }

        [Header("ㄴ(화) 지속 — 시간에 걸친 추가 피해. 오행별 지속 방식 차별화(사용자 결정 2026-07-24)")]
        [Tooltip("기본값(오행 배열 미배선 시 폴백)")]
        public float SustainSeconds = 3f;
        public float SustainTickInterval = 0.5f;
        [Tooltip("틱당 피해 = 본 피해 x 이 비율")]
        [Range(0f, 1f)] public float SustainTickFraction = 0.3f;
        [Tooltip("오행별 지속 방식. 5개(목/화/토/금/수) 모두 채워야 한다 — 비면 위 기본값으로 폴백")]
        public SustainElementConfig[] SustainByElement = new SustainElementConfig[5]
        {
            new SustainElementConfig { Element = Element.Wood,  TickFraction = 0.3f, TickInterval = 0.5f, Seconds = 3f }, // 목=표준 DoT(원형)
            new SustainElementConfig { Element = Element.Fire,  TickFraction = 0.2f, TickInterval = 0.5f, Seconds = 3f,
                TickFractionGrowthPerTick = 0.05f }, // 화=틱마다 피해가 점점 세짐(번지듯)
            new SustainElementConfig { Element = Element.Earth, TickFraction = 0.25f, TickInterval = 0.5f, Seconds = 3f,
                SpreadRadius = 3f, SpreadInterval = 1f }, // 토=지속 동안 주기적으로 주변 적도 새로 끌어들임(광역 판정)
            new SustainElementConfig { Element = Element.Metal, TickFraction = 0.25f, TickInterval = 0.5f, Seconds = 3f,
                PoiseTickFraction = 0.1f }, // 금=틱마다 포이즈도 같이 깎임(그로기 유도)
            new SustainElementConfig { Element = Element.Water, TickFraction = 0.15f, TickInterval = 0.5f, Seconds = 3f,
                InkRefundPerTick = 1f }, // 수=틱 피해는 약하지만 시전자에게 먹을 조금씩 환급
        };

        public SustainElementConfig GetSustainConfig(Element element)
        {
            if (SustainByElement != null)
                foreach (var s in SustainByElement)
                    if (s.Element == element) return s;
            return new SustainElementConfig { Element = element, TickFraction = SustainTickFraction,
                TickInterval = SustainTickInterval, Seconds = SustainSeconds };
        }

        [Header("ㅁ(토) 격발 — 음 진으로 설치, 양(陽) 진이 닿으면 격발 (작도설계안 §5)")]
        public float MarkSeconds = 8f;
        [Tooltip("격발 보너스 = 대상 MaxPoise 대비 그로기 축적 비율 — M1은 그로기 대폭")]
        [Range(0f, 1f)] public float TriggerPoiseFraction = 0.65f;
        [Tooltip("동시에 유지되는 설치 상한 — 사방 도배 방지")]
        public int MaxActiveMarks = 3;
        [Tooltip("격발 폭발 표현 지름")]
        public float TriggerBurstDiameter = 3.5f;

        [Header("ㅅ(금) 관통 — 다수를 뚫고 직선 진행. 오행별 수치·부가효과 차별화(사용자 결정 2026-07-24)")]
        [Tooltip("기본값(오행 배열 미배선 시 폴백) — 최대 관통 대상 수")]
        public int PierceMaxTargets = 3;
        [Tooltip("오행별 관통 수치+부가효과. 5개(목/화/토/금/수) 모두 채워야 한다 — 비면 위 기본값으로 폴백")]
        public PierceElementConfig[] PierceByElement = new PierceElementConfig[5]
        {
            new PierceElementConfig { Element = Element.Wood,  MaxTargets = 3, SpeedMultiplier = 1f,
                BindMoveMultiplier = 0.4f, BindSeconds = 1.2f }, // 목=관통한 대상 짧게 속박
            new PierceElementConfig { Element = Element.Fire,  MaxTargets = 2, SpeedMultiplier = 1f,
                SustainTickFraction = 0.2f, SustainTickInterval = 0.5f, SustainSeconds = 2f }, // 화=관통 경로에 화상(짧은 지속)
            new PierceElementConfig { Element = Element.Earth, MaxTargets = 2, SpeedMultiplier = 0.8f,
                ExtraPoiseFraction = 0.2f }, // 토=적게 뚫지만 관통마다 포이즈 추가 축적
            new PierceElementConfig { Element = Element.Metal, MaxTargets = 5, SpeedMultiplier = 1.3f }, // 금=부가효과 없이 관통력 자체를 극대화
            new PierceElementConfig { Element = Element.Water, MaxTargets = 3, SpeedMultiplier = 1f,
                PullStrength = 2.5f }, // 수=관통한 대상들을 경로 쪽으로 살짝 끌어당김(WaterPuller 선례)
        };

        public PierceElementConfig GetPierceConfig(Element element)
        {
            if (PierceByElement != null)
                foreach (var p in PierceByElement)
                    if (p.Element == element) return p;
            return new PierceElementConfig { Element = element, MaxTargets = PierceMaxTargets, SpeedMultiplier = 1f };
        }

        [Header("ㅇ(수) 연쇄 — 인접 대상으로 전파. 오행 공통(동사 유지) + 부가효과가 함께 퍼짐(사용자 결정 2026-07-24)")]
        [Tooltip("기본값(오행 배열 미배선 시 폴백) — 최대 점프 수")]
        public int ChainMaxJumps = 3;
        public float ChainRadius = 8f;
        [Tooltip("점프마다 피해 배율 (0.7 = 30%씩 감쇠)")]
        [Range(0f, 1f)] public float ChainDamageFalloff = 0.7f;
        [Tooltip("오행별 연쇄 부가효과 — 부가효과 자체가 연쇄를 타고 함께 퍼진다(\"연쇄=퍼짐\" 원칙). 5개 모두 채워야 한다")]
        public ChainElementConfig[] ChainByElement = new ChainElementConfig[5]
        {
            new ChainElementConfig { Element = Element.Wood,  BindMoveMultiplier = 0.6f, BindSeconds = 1f }, // 목=속박이 연쇄 전원에게 전염(약하게)
            new ChainElementConfig { Element = Element.Fire,  SustainTickFraction = 0.15f, SustainTickInterval = 0.5f,
                SustainSeconds = 1.5f, SustainSecondsGrowthPerJump = 0.4f }, // 화=화상이 옮겨붙되 나중 대상일수록 더 오래감
            new ChainElementConfig { Element = Element.Earth, ExtraPoiseFraction = 0.08f, ExtraPoiseGrowthPerJump = 0.06f }, // 토=그로기 축적이 누적(마지막이 가장 크게)
            new ChainElementConfig { Element = Element.Metal }, // 금=부가효과 없음(연쇄 자체가 더 멀리·많이 퍼짐 — ChainMaxJumps/Radius로 표현)
            new ChainElementConfig { Element = Element.Water, BindMoveMultiplier = 0.7f, BindSeconds = 1.5f }, // 수=감속이 연쇄 전체에 고르게 유지
        };

        public ChainElementConfig GetChainConfig(Element element)
        {
            if (ChainByElement != null)
                foreach (var c in ChainByElement)
                    if (c.Element == element) return c;
            return new ChainElementConfig { Element = element };
        }

        [Header("ㄱ+영역(ㅗ/ㅜ) 소환 — 개체 수 제한 없음, 먹(잉크) 비용으로만 억제(사용자 결정)")]
        [Tooltip("공격 소환수(ㅗ+ㄱ) 자동 타겟팅 반경")]
        public float SummonAttackRange = 10f;
        [Tooltip("버프 소환수(ㅜ+ㄱ) 플레이어 추종 시 유지 거리")]
        public float SummonFollowDistance = 2.5f;
        [Tooltip("버프 소환수가 플레이어와 이 거리 안에 있어야 버프가 적용된다")]
        public float SummonBuffRadius = 6f;
    }

    // ㄱ(단일속박) 오행별 방해 방식 — 사용자 결정(2026-07-24): "속박=행동을 방해한다"는 컨셉으로,
    //   목=표준 이동 감속 / 화·토=이동 감속+공격(예비 동작) 느려짐 / 금=약한 감속+지속피해 / 수=약한 감속+조준 흔들림.
    [System.Serializable]
    public struct BindElementConfig
    {
        public Element Element;
        [Range(0f, 1f)] public float MoveMultiplier;
        public float Seconds;

        [Header("화·토 — 공격(예비 동작) 느려짐(1이면 미적용)")]
        [Tooltip("1보다 크면 텔레그래프(예비 동작) 시간이 이만큼 늘어난다")]
        public float TelegraphMultiplier;

        [Header("금 — 약한 속박 대신 지속 피해(0이면 미적용)")]
        [Range(0f, 1f)] public float SustainTickFraction;
        public float SustainTickInterval;
        public float SustainSeconds;

        [Header("수 — 약한 속박 대신 조준 흔들림(0이면 미적용)")]
        [Tooltip("적 투사체 조준에 더해지는 무작위 오차(도)")]
        public float AimJitterDegrees;
    }

    // ㅇ(연쇄) 오행별 부가효과 — 사용자 결정(2026-07-24): "연쇄=퍼진다"는 동사에서 자연스럽게 유추되도록,
    //   부가효과 자체가 연쇄를 타고 함께 퍼진다. 목=속박 전염/화=화상 전염(점점 길어짐)/토=그로기 누적(점점 커짐)/
    //   금=부가효과 없음(연쇄 범위·횟수 자체가 특화)/수=감속이 고르게 유지.
    [System.Serializable]
    public struct ChainElementConfig
    {
        public Element Element;

        [Header("목·수 — 연쇄 대상에게 속박 전염(0이면 미적용)")]
        [Range(0f, 1f)] public float BindMoveMultiplier;
        public float BindSeconds;

        [Header("화 — 화상이 옮겨붙되 점프마다 지속시간이 늘어남(0이면 미적용)")]
        [Range(0f, 1f)] public float SustainTickFraction;
        public float SustainTickInterval;
        public float SustainSeconds;
        [Tooltip("점프 한 번마다 SustainSeconds에 더해지는 시간 — 나중에 맞을수록 더 오래 탄다")]
        public float SustainSecondsGrowthPerJump;

        [Header("토 — 그로기 축적이 점프마다 누적(0이면 미적용)")]
        [Range(0f, 1f)] public float ExtraPoiseFraction;
        [Tooltip("점프 한 번마다 ExtraPoiseFraction에 더해지는 비율 — 마지막 대상이 가장 크게 휘청인다")]
        public float ExtraPoiseGrowthPerJump;
    }

    // ㅅ(관통) 오행별 수치+부가효과 — 사용자 결정(2026-07-24): 목=속박/화=화상/토=포이즈 추가/
    //   금=부가효과 없이 관통력(대상 수·속도)만 극대화/수=끌어당김. 값 0인 필드는 "그 효과 없음"으로 취급.
    [System.Serializable]
    public struct PierceElementConfig
    {
        public Element Element;
        [Tooltip("최대 관통 대상 수")]
        public int MaxTargets;
        [Tooltip("투사체 속도 배율(1=변화 없음) — 금은 빠르게, 토는 느리게")]
        public float SpeedMultiplier;

        [Header("목 — 관통 대상 속박(0이면 미적용)")]
        [Range(0f, 1f)] public float BindMoveMultiplier;
        public float BindSeconds;

        [Header("화 — 관통 경로에 짧은 지속 피해(0이면 미적용)")]
        [Range(0f, 1f)] public float SustainTickFraction;
        public float SustainTickInterval;
        public float SustainSeconds;

        [Header("토 — 관통마다 포이즈 추가 축적(0이면 미적용)")]
        [Tooltip("본 피해 대비 추가 포이즈 축적 비율(기본 포이즈 축적에 더해짐)")]
        [Range(0f, 1f)] public float ExtraPoiseFraction;

        [Header("수 — 관통 대상들을 서로 끌어당김(0이면 미적용)")]
        public float PullStrength;
    }

    // ㄴ(지속) 오행별 방식 — 사용자 결정(2026-07-24): 목=표준 DoT(원형)/화=틱마다 강해짐/
    //   토=주변 적을 새로 끌어들이는 광역 판정/금=포이즈 동반 축적/수=시전자에게 먹 환급.
    [System.Serializable]
    public struct SustainElementConfig
    {
        public Element Element;
        [Tooltip("틱당 피해 = 본 피해 x 이 비율")]
        [Range(0f, 1f)] public float TickFraction;
        public float TickInterval;
        public float Seconds;

        [Header("화 — 틱마다 피해 비율이 누적 증가(0이면 미적용)")]
        [Tooltip("틱 한 번마다 TickFraction에 더해지는 비율 — 불이 번지듯 갈수록 세짐")]
        public float TickFractionGrowthPerTick;

        [Header("토 — 지속 동안 주기적으로 주변 적을 새로 끌어들임(0이면 미적용)")]
        [Tooltip("이 반경 안의 아직 안 걸린 적에게도 지속 피해가 옮겨붙는다")]
        public float SpreadRadius;
        [Tooltip("주변 재판정 주기")]
        public float SpreadInterval;

        [Header("금 — 틱마다 포이즈도 함께 축적(0이면 미적용)")]
        [Tooltip("틱 피해 대비 포이즈 축적 비율")]
        [Range(0f, 1f)] public float PoiseTickFraction;

        [Header("수 — 틱마다 시전자에게 먹(잉크) 환급(0이면 미적용)")]
        public float InkRefundPerTick;
    }
}
