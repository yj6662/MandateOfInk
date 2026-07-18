using System;
using UnityEngine;

namespace MandateOfInk.Data
{
    // 진 효과 파라미터의 추상 기반 클래스.
    // 도면 SO가 이 타입의 리스트를 담고, Odin이 파생 타입까지 다형성으로 직렬화한다.
    [Serializable]
    public abstract class EffectParam
    {
    }

    // 피해 효과 (예: 「다」 화염 폭발). 수치는 [가정].
    [Serializable]
    public sealed class DamageEffect : EffectParam
    {
        [Tooltip("[가정] 기본 피해량")] public float Damage = 10f;
        [Tooltip("[가정] 반경 (0 = 단일 대상)")] public float Radius = 0f;
    }

    // 방어막 효과 (예: 「우」 수막 방어). 수치는 [가정].
    [Serializable]
    public sealed class ShieldEffect : EffectParam
    {
        [Tooltip("[가정] 흡수량")] public float Absorb = 20f;
        [Tooltip("[가정] 지속 시간(초)")] public float Duration = 5f;
    }

    // 지속 피해/상태 효과. 수치는 [가정].
    [Serializable]
    public sealed class DotEffect : EffectParam
    {
        [Tooltip("[가정] 초당 피해")] public float DamagePerSecond = 3f;
        [Tooltip("[가정] 지속 시간(초)")] public float Duration = 4f;
    }
}
