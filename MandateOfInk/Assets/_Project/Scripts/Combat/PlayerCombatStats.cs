using System.Collections.Generic;
using MandateOfInk.Spellcraft;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 플레이어 전투 배율 공통 허브 — 버프(소환수 등)가 여기 배율을 걸고, 소비측(평타·이동·피격)이 여기서 읽는다.
    // 여러 버프 소스가 겹치면 배율은 곱해지지 않고 "가장 센 것 하나만" 적용한다(중첩 폭주 방지 — [가정]).
    // 이동속도는 StarterAssets(서드파티, asmdef 미참조 대상)에 직접 곱하지 못하므로 MoveSpeedMultiplier 값만
    // 노출해 두고, 실제 반영은 별도 글루(ProtoGlue 계층)가 담당한다(M1 자체 컨트롤러 도입 시 재정리 예정).
    public sealed class PlayerCombatStats : MonoBehaviour
    {
        private readonly struct Buff
        {
            public readonly object Source;
            public readonly float DamageMultiplier;
            public readonly float MoveSpeedMultiplier;
            public readonly float DefenseMultiplier; // 받는 피해 배율(1보다 작으면 방어력 증가)
            public readonly float PoiseDamageMultiplier; // 적에게 축적시키는 포이즈 배율(1보다 크면 그로기 유도가 쉬워짐)
            public Buff(object source, float dmg, float move, float defense, float poise)
            {
                Source = source; DamageMultiplier = dmg; MoveSpeedMultiplier = move; DefenseMultiplier = defense; PoiseDamageMultiplier = poise;
            }
        }

        private readonly List<Buff> _buffs = new();

        public float DamageMultiplier { get; private set; } = 1f;
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float DefenseMultiplier { get; private set; } = 1f;
        public float PoiseDamageMultiplier { get; private set; } = 1f;

        // source(보통 소환수 GameObject/컴포넌트)를 키로 버프를 걸거나 갱신한다. 배율 1f는 "효과 없음".
        public void ApplyBuff(object source, float damageMultiplier = 1f, float moveSpeedMultiplier = 1f,
            float defenseMultiplier = 1f, float poiseDamageMultiplier = 1f)
        {
            _buffs.RemoveAll(b => b.Source == source);
            _buffs.Add(new Buff(source, damageMultiplier, moveSpeedMultiplier, defenseMultiplier, poiseDamageMultiplier));
            Recalculate();
        }

        // source가 사라지면(소환수 소멸 등) 그 버프를 뗀다.
        public void RemoveBuff(object source)
        {
            if (_buffs.RemoveAll(b => b.Source == source) > 0) Recalculate();
        }

        private void Recalculate()
        {
            float dmg = 1f, move = 1f, def = 1f, poise = 1f;
            foreach (var b in _buffs)
            {
                dmg = Mathf.Max(dmg, b.DamageMultiplier);
                move = Mathf.Max(move, b.MoveSpeedMultiplier);
                def = Mathf.Min(def, b.DefenseMultiplier); // 방어는 작을수록(피해 감소) 유리
                poise = Mathf.Max(poise, b.PoiseDamageMultiplier);
            }
            DamageMultiplier = dmg;
            MoveSpeedMultiplier = move;
            DefenseMultiplier = def;
            PoiseDamageMultiplier = poise;
        }
    }

    // 런타임에 동적 생성되는 오브젝트(투사체 등)는 씬 배선을 못 받으므로, 씬의 PlayerCombatStats를
    // 지연 조회해 캐싱하는 정적 조회창. 없으면 전부 배율 1(무영향).
    public static class PlayerBuffLookup
    {
        private static PlayerCombatStats _stats;
        private static PlayerCombatStats Stats
        {
            get
            {
                if (_stats == null) _stats = Object.FindFirstObjectByType<PlayerCombatStats>();
                return _stats;
            }
        }

        public static float DamageMultiplier => Stats != null ? Stats.DamageMultiplier : 1f;
        public static float MoveSpeedMultiplier => Stats != null ? Stats.MoveSpeedMultiplier : 1f;
        public static float DefenseMultiplier => Stats != null ? Stats.DefenseMultiplier : 1f;
        public static float PoiseDamageMultiplier => Stats != null ? Stats.PoiseDamageMultiplier : 1f;

        // 수(水) 지속 — 틱마다 시전자에게 먹을 조금씩 돌려준다(같은 지연 조회 패턴).
        private static InkPool _inkPool;
        public static void RefundInk(float amount)
        {
            if (amount <= 0f) return;
            if (_inkPool == null) _inkPool = Object.FindFirstObjectByType<InkPool>();
            if (_inkPool != null) _inkPool.Add(amount);
        }
    }
}
