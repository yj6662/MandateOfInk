using Sirenix.OdinInspector;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 소환수 정의 DTO — ㅗ/ㅜ(영역) + ㄱ받침 조합 전용(FinalModifier.Summon, 사용자 결정 2026-07-23).
    // 오행 5장으로 확장 가능한 구조이나 이번 범위는 목(木) 1장. 수치는 전부 [가정] 플레이스홀더.
    [CreateAssetMenu(menuName = "MandateOfInk/Data/Summon Definition", fileName = "SM_NewSummon")]
    public sealed class SummonDefinitionSO : SerializedScriptableObject
    {
        public enum Role { Attacker, Buffer } // ㅗ+ㄱ=공격형 / ㅜ+ㄱ=버프형

        // 공격 소환수의 오행별 동사(전투코어루프 §9 "속성=공격 동사" 원칙을 소환수에도 적용, 사용자 결정 2026-07-23).
        //   목=지속 투사체 연사 / 화=제자리 화염 지속 피해장 / 토=굵고 느린 광역 고피해 타격 /
        //   금=긴 사거리 저격 관통탄 / 수=주기적 인접 전체 연쇄 파동.
        public enum AttackVerb { ProjectileVolley, FireZone, SlowSlam, SniperPierce, ChainPulse }

        [Title("식별")]
        public string Id;
        public Element Element;
        public Role Kind;

        [Title("공통 [가정]")]
        public float MaxHp = 30f;
        public float LifetimeSeconds = 12f; // 지속시간 — 다 되면 자연 소멸(사망 이펙트 없이)
        public float ModelScale = 0.6f;

        [Title("공격형(ㅗ+ㄱ) — 오행별 동사 [가정]")]
        [ShowIf("@Kind == Role.Attacker")]
        public AttackVerb Verb = AttackVerb.ProjectileVolley;
        [ShowIf("@Kind == Role.Attacker")]
        public float AttackDamage = 6f;
        [Tooltip("동사 실행 간격(초) — 연사는 짧게, 저격/파동은 길게")]
        [ShowIf("@Kind == Role.Attacker")]
        public float AttackInterval = 1.2f;

        [Title("목(木) 전용 — 투사체 연사(ProjectileVolley)")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.ProjectileVolley")]
        public float ProjectileSpeed = 14f;

        [Title("화(火) 전용 — 제자리 화염 지속 피해장(FireZone)")]
        [Tooltip("발밑에 계속 남는 화염 장판 반경")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.FireZone")]
        public float FireZoneRadius = 3f;

        [Title("토(土) 전용 — 굵고 느린 광역 고피해 타격(SlowSlam)")]
        [Tooltip("타격 판정 반경")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.SlowSlam")]
        public float SlamRadius = 3.5f;
        [Tooltip("타격 전 예비 동작(텔레그래프) 시간 — 느리고 무겁게")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.SlowSlam")]
        public float SlamTelegraphSeconds = 1f;

        [Title("금(金) 전용 — 긴 사거리 저격 관통탄(SniperPierce)")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.SniperPierce")]
        public float SniperRange = 20f;
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.SniperPierce")]
        public float SniperProjectileSpeed = 32f;
        [Tooltip("최대 관통 대상 수")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.SniperPierce")]
        public int SniperPierceCount = 3;

        [Title("수(水) 전용 — 주기적 인접 전체 연쇄 파동(ChainPulse)")]
        [Tooltip("파동이 닿는 반경 — 이 안의 적 전원이 동시에 맞는다")]
        [ShowIf("@Kind == Role.Attacker && Verb == AttackVerb.ChainPulse")]
        public float ChainPulseRadius = 5f;

        [Title("버프형(ㅜ+ㄱ) — 플레이어 추종, 범위 내 버프 [가정]")]
        [Tooltip("1보다 크면 공격력 증가")]
        [ShowIf("@Kind == Role.Buffer")]
        public float DamageMultiplier = 1f;
        [Tooltip("1보다 크면 이동속도 증가")]
        [ShowIf("@Kind == Role.Buffer")]
        public float MoveSpeedMultiplier = 1f;
        [Tooltip("1보다 작으면 방어력 증가(받는 피해 감소)")]
        [ShowIf("@Kind == Role.Buffer")]
        public float DefenseMultiplier = 1f;
        [Tooltip("1보다 크면 적에게 포이즈를 더 빨리 축적시켜 그로기를 유도하기 쉬워짐(금 전용 [가정])")]
        [ShowIf("@Kind == Role.Buffer")]
        public float PoiseDamageMultiplier = 1f;
        [Tooltip("초당 체력 회복량 — 0이면 없음")]
        [ShowIf("@Kind == Role.Buffer")]
        public float HealPerSecond = 0f;
    }
}
