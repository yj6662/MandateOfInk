using System.Collections.Generic;
using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 적에게 붙는 상태 이상 — 종성(받침) 거동의 수신측.
    // 속박(ㄱ): 이동 배율 / 지속(ㄴ): 틱 피해 / 격발 표식(ㅁ): 설치 후 다음 술식 적중 시 격발.
    // 필요할 때 EnemyHealth와 같은 오브젝트에 런타임으로 부착된다(씬 사전 배선 불필요).
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyStatus : MonoBehaviour
    {
        private EnemyHealth _health;

        // 속박
        private float _slowUntil;
        private float _slowFactor = 1f;
        private GameObject _bindVisual;
        public float MoveMultiplier => Time.time < _slowUntil ? _slowFactor : 1f;

        // ㄱ속박 오행별 부가효과(사용자 결정 2026-07-24): 화·토=공격이 느려짐(텔레그래프 배율), 수=조준이 흔들림(오차)
        private float _telegraphSlowUntil;
        private float _telegraphMultiplier = 1f; // 1보다 크면 예비 동작이 더 오래 걸림(공격이 느려짐)
        public float TelegraphMultiplier => Time.time < _telegraphSlowUntil ? _telegraphMultiplier : 1f;
        private float _aimJitterUntil;
        private float _aimJitterDegrees;
        public float AimJitterDegrees => Time.time < _aimJitterUntil ? _aimJitterDegrees : 0f;

        // 지속(틱 피해)
        private float _dotUntil;
        private float _dotInterval;
        private float _dotDamagePerTick;
        private float _nextTickTime;
        private Color _dotColor;
        private GameObject _dotVisual;

        // ㄴ지속 오행별 부가효과(사용자 결정 2026-07-24): 화=틱마다 강해짐/금=포이즈 동반/수=시전자 먹 환급
        private float _dotFractionGrowthPerTick; // 화 — 매 틱마다 _dotDamagePerTick에 곱해지는 성장분(비율 기준)
        private float _dotBaseDamage; // 화 성장분 계산용 원 피해(damage), 0이면 성장 없음
        private float _dotPoiseTickFraction; // 금 — 틱 피해 대비 포이즈 축적 비율
        private CombatConfigSO _dotCombatConfig; // 금 포이즈 축적에 필요
        private float _dotInkRefundPerTick; // 수 — 틱마다 시전자에게 돌려줄 먹의 양

        // ㄴ지속(토) — 지속 동안 주기적으로 주변 적을 새로 끌어들이는 광역 판정
        private float _spreadRadius;
        private float _spreadInterval;
        private float _nextSpreadTime;
        private float _spreadTickFraction;
        private float _spreadTickInterval;
        private float _spreadSeconds;
        private Color _spreadColor;
        private Element _spreadElement;

        // 포이즈·그로기
        private float _poise;
        private float _poiseRegenPerSecond;
        private float _groggyUntil;
        private float _groggyDamageMultiplier = 1f;
        private GameObject _groggyVisual;
        public bool IsGroggy => Time.time < _groggyUntil;

        // 경화(화마 굳음) — 받는 피해 급감
        private float _hardenedUntil;
        private float _hardenedMultiplier = 1f;
        public bool IsHardened => Time.time < _hardenedUntil;

        // 받는 피해 총 배율 — 그로기(증가)와 경화(감소)를 합산. 공격측이 이 값을 곱한다.
        public float GroggyDamageMultiplier =>
            (IsGroggy ? _groggyDamageMultiplier : 1f) * (IsHardened ? _hardenedMultiplier : 1f);

        public void SetHardened(float damageMultiplier, float seconds)
        {
            _hardenedMultiplier = damageMultiplier;
            _hardenedUntil = Time.time + seconds;
        }

        // 격발 표식 — 동시 설치 상한(도배 방지)을 위해 보유자를 전역 등록한다
        private static readonly List<EnemyStatus> ActiveMarkHolders = new List<EnemyStatus>();
        private Element _markElement;
        private float _markUntil;
        private float _markPoiseFraction;
        private float _markBurstDiameter;
        private Color _markColor;
        private GameObject _markVisual;
        public bool HasMark => Time.time < _markUntil;

        public static EnemyStatus GetOrAdd(EnemyHealth enemy)
        {
            var status = enemy.GetComponent<EnemyStatus>();
            if (status == null) status = enemy.gameObject.AddComponent<EnemyStatus>();
            return status;
        }

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
        }

        private void Update()
        {
            // 지속 피해 틱
            if (Time.time < _dotUntil && Time.time >= _nextTickTime)
            {
                _nextTickTime = Time.time + _dotInterval;
                _health.TakeDamage(_dotDamagePerTick);
                SpellVisuals.SpawnBurst(transform.position + Vector3.up * 1.2f, _dotColor, 0.5f, 0.25f);

                if (_dotPoiseTickFraction > 0f && _dotCombatConfig != null) // 금 — 틱마다 포이즈도 축적
                    AddPoise(_dotDamagePerTick * _dotPoiseTickFraction, _dotCombatConfig);

                if (_dotInkRefundPerTick > 0f) // 수 — 틱마다 시전자에게 먹 환급
                    PlayerBuffLookup.RefundInk(_dotInkRefundPerTick);

                if (_dotFractionGrowthPerTick > 0f && _dotBaseDamage > 0f) // 화 — 다음 틱은 더 세짐
                    _dotDamagePerTick += _dotBaseDamage * _dotFractionGrowthPerTick;
            }

            // ㄴ지속(토) — 주기적으로 주변의 아직 안 걸린 적에게도 지속 피해를 옮긴다
            if (Time.time < _dotUntil && _spreadRadius > 0f && Time.time >= _nextSpreadTime)
            {
                _nextSpreadTime = Time.time + Mathf.Max(_spreadInterval, 0.1f);
                SpreadToNearby();
            }

            // 포이즈 자연 회복 (그로기 중엔 정지)
            if (!IsGroggy && _poise > 0f)
                _poise = Mathf.Max(0f, _poise - _poiseRegenPerSecond * Time.deltaTime);

            // 만료된 표시 정리
            if (_bindVisual != null && Time.time >= _slowUntil) Destroy(_bindVisual);
            if (_dotVisual != null && Time.time >= _dotUntil) Destroy(_dotVisual);
            if (_markVisual != null && Time.time >= _markUntil) ClearMark(); // 시간 만료 = 불발
            if (_groggyVisual != null && !IsGroggy) Destroy(_groggyVisual);
        }

        // 포이즈 축적 — 한계(MaxPoise) 도달 시 그로기. 그로기 중에는 추가 축적 없음.
        public void AddPoise(float amount, CombatConfigSO config)
        {
            if (IsGroggy || config == null || amount <= 0f) return;
            _poiseRegenPerSecond = config.PoiseRegenPerSecond;
            _poise += amount;
            float max = _health.Definition != null ? _health.Definition.MaxPoise : 50f;
            if (_poise < max) return;

            _poise = 0f;
            _groggyUntil = Time.time + config.GroggySeconds;
            _groggyDamageMultiplier = config.GroggyDamageMultiplier;
            // 「틈」 — 빈틈이 열렸다는 디제틱 표시 (금빛)
            RefreshVisual(ref _groggyVisual, "틈", new Color(0.95f, 0.8f, 0.25f), Vector3.one * 0.6f, Vector3.up * 2.5f);
            Debug.Log($"[Status] {name} 그로기! {config.GroggySeconds:F1}s — 받는 피해 x{config.GroggyDamageMultiplier:F1}");
        }

        // ㄱ(목) 속박 — 오행별 방해 방식이 다르다(사용자 결정 2026-07-24): 화·토=공격 느려짐, 수=조준 흔들림.
        //   telegraphMultiplier>1이면 예비 동작이 느려지고, aimJitterDegrees>0이면 조준에 오차가 생긴다. 둘 다 0/1이면 미적용.
        public void ApplyBind(float moveMultiplier, float seconds, Color color,
            float telegraphMultiplier = 1f, float aimJitterDegrees = 0f)
        {
            _slowFactor = moveMultiplier;
            _slowUntil = Time.time + seconds;
            RefreshVisual(ref _bindVisual, "ㄱ", color, new Vector3(1.6f, 0.08f, 1.6f), Vector3.up * 0.1f);
            Debug.Log($"[Status] {name} 속박 {seconds:F1}s (이동 x{moveMultiplier:F2})");

            if (telegraphMultiplier > 1f)
            {
                _telegraphMultiplier = telegraphMultiplier;
                _telegraphSlowUntil = Time.time + seconds;
            }
            if (aimJitterDegrees > 0f)
            {
                _aimJitterDegrees = aimJitterDegrees;
                _aimJitterUntil = Time.time + seconds;
            }
        }

        // ㄴ(화) 지속 — 오행별 부가효과(사용자 결정 2026-07-24): 화=틱마다 강해짐/금=포이즈 동반/수=시전자 먹 환급.
        //   growthPerTick>0이면 매 틱 damagePerTick이 커지고, poiseTickFraction/inkRefundPerTick>0이면 각각 부가효과가 붙는다.
        public void ApplyDot(float damagePerTick, float interval, float seconds, Color color,
            float growthPerTick = 0f, float poiseTickFraction = 0f, CombatConfigSO combatConfig = null,
            float inkRefundPerTick = 0f)
        {
            _dotDamagePerTick = damagePerTick;
            _dotBaseDamage = damagePerTick;
            _dotInterval = Mathf.Max(interval, 0.1f);
            _dotUntil = Time.time + seconds;
            _nextTickTime = Time.time + _dotInterval;
            _dotColor = color;
            _dotFractionGrowthPerTick = growthPerTick;
            _dotPoiseTickFraction = poiseTickFraction;
            _dotCombatConfig = combatConfig;
            _dotInkRefundPerTick = inkRefundPerTick;
            RefreshVisual(ref _dotVisual, "ㄴ", color, Vector3.one * 0.45f, Vector3.up * 2.2f);
            Debug.Log($"[Status] {name} 지속 피해 {seconds:F1}s (틱 {damagePerTick:F1})");
        }

        // ㄴ(토) 지속 — 지속 동안 주기적으로 주변의 아직 안 걸린 적에게도 같은 지속 피해를 옮긴다(광역 확산 판정).
        public void ApplySustainSpread(float spreadRadius, float spreadInterval,
            float tickFraction, float tickInterval, float seconds, float baseDamage, Color color, Element element)
        {
            _spreadRadius = spreadRadius;
            _spreadInterval = spreadInterval;
            _nextSpreadTime = Time.time + Mathf.Max(spreadInterval, 0.1f);
            _spreadTickFraction = tickFraction;
            _spreadTickInterval = tickInterval;
            _spreadSeconds = seconds;
            _spreadColor = color;
            _spreadElement = element;
            _spreadBaseDamageForTick = baseDamage;
        }
        private float _spreadBaseDamageForTick;

        private void SpreadToNearby()
        {
            var hits = Physics.OverlapSphere(transform.position, _spreadRadius);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<EnemyHealth>();
                if (enemy == null || enemy == _health) continue;
                var other = GetOrAdd(enemy);
                if (Time.time < other._dotUntil) continue; // 이미 지속 걸린 적은 건너뜀(중첩 방지)
                other.ApplyDot(_spreadBaseDamageForTick * _spreadTickFraction, _spreadTickInterval, _spreadSeconds, _spreadColor);
                other.ApplySustainSpread(_spreadRadius, _spreadInterval, _spreadTickFraction, _spreadTickInterval,
                    _spreadSeconds, _spreadBaseDamageForTick, _spreadColor, _spreadElement); // 옮겨붙은 적도 계속 퍼뜨림
            }
        }

        // ㅁ(토) 격발 표식 설치 — 동시 상한 초과 시 가장 오래된 설치가 불발로 흩어진다(처벌 없음)
        public void InstallMark(Element element, float seconds, float poiseFraction,
            float burstDiameter, Color color, int maxActiveMarks)
        {
            if (!ActiveMarkHolders.Contains(this))
            {
                while (ActiveMarkHolders.Count >= Mathf.Max(maxActiveMarks, 1))
                {
                    var oldest = ActiveMarkHolders[0];
                    Debug.Log($"[Status] {oldest.name} 설치 상한 초과 — 가장 오래된 표식 불발");
                    oldest.ClearMark();
                }
                ActiveMarkHolders.Add(this);
            }
            _markElement = element;
            _markUntil = Time.time + seconds;
            _markPoiseFraction = poiseFraction;
            _markBurstDiameter = burstDiameter;
            _markColor = color;
            RefreshVisual(ref _markVisual, "ㅁ", color, Vector3.one * 0.5f, Vector3.up * 2.8f);
            Debug.Log($"[Status] {name} 격발 표식 설치 ({element}, {seconds:F1}s)");
        }

        // 표식 격발 — 양(陽) 진이 닿았을 때 호출(호출측이 음양을 가른다).
        // 보너스는 피해가 아니라 「그로기 대폭」(작도설계안 §5, M1 사양).
        public bool TryDetonateMark(CombatConfigSO combatConfig)
        {
            if (!HasMark || combatConfig == null) return false;
            float poise = (_health.Definition != null ? _health.Definition.MaxPoise : 50f) * _markPoiseFraction;
            var color = _markColor;
            float dia = _markBurstDiameter;
            var element = _markElement;
            ClearMark();
            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 1.2f, color, dia, 0.4f);
            Debug.Log($"[Status] {name} 표식 격발! 그로기 +{poise:F1} ({element})");
            AddPoise(poise, combatConfig);
            return true;
        }

        private void ClearMark()
        {
            _markUntil = 0f;
            if (_markVisual != null) Destroy(_markVisual);
            ActiveMarkHolders.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveMarkHolders.Remove(this);
        }

        // 상태 표시: 반투명 프리미티브 + 받침 글자 (기존 표현 문법 준수)
        private void RefreshVisual(ref GameObject visual, string letter, Color color, Vector3 scale, Vector3 offset)
        {
            if (visual != null) Destroy(visual);
            color.a = 0.4f;
            visual = SpellVisuals.CreateTranslucent(PrimitiveType.Sphere, color, scale, keepColliderAsTrigger: false);
            visual.name = $"Status_{letter}";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = offset;
            SpellVisuals.AttachLetter(visual.transform, letter, 0.35f, new Color(0.05f, 0.05f, 0.05f, 0.9f));
        }
    }
}
