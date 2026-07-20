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

        // 지속(틱 피해)
        private float _dotUntil;
        private float _dotInterval;
        private float _dotDamagePerTick;
        private float _nextTickTime;
        private Color _dotColor;
        private GameObject _dotVisual;

        // 포이즈·그로기
        private float _poise;
        private float _poiseRegenPerSecond;
        private float _groggyUntil;
        private float _groggyDamageMultiplier = 1f;
        private GameObject _groggyVisual;
        public bool IsGroggy => Time.time < _groggyUntil;
        public float GroggyDamageMultiplier => IsGroggy ? _groggyDamageMultiplier : 1f;

        // 격발 표식
        private Element _markElement;
        private float _markUntil;
        private float _markBonusMultiplier;
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
            }

            // 포이즈 자연 회복 (그로기 중엔 정지)
            if (!IsGroggy && _poise > 0f)
                _poise = Mathf.Max(0f, _poise - _poiseRegenPerSecond * Time.deltaTime);

            // 만료된 표시 정리
            if (_bindVisual != null && Time.time >= _slowUntil) Destroy(_bindVisual);
            if (_dotVisual != null && Time.time >= _dotUntil) Destroy(_dotVisual);
            if (_markVisual != null && Time.time >= _markUntil) Destroy(_markVisual);
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

        // ㄱ(목) 속박
        public void ApplyBind(float moveMultiplier, float seconds, Color color)
        {
            _slowFactor = moveMultiplier;
            _slowUntil = Time.time + seconds;
            RefreshVisual(ref _bindVisual, "ㄱ", color, new Vector3(1.6f, 0.08f, 1.6f), Vector3.up * 0.1f);
            Debug.Log($"[Status] {name} 속박 {seconds:F1}s (이동 x{moveMultiplier:F2})");
        }

        // ㄴ(화) 지속
        public void ApplyDot(float damagePerTick, float interval, float seconds, Color color)
        {
            _dotDamagePerTick = damagePerTick;
            _dotInterval = Mathf.Max(interval, 0.1f);
            _dotUntil = Time.time + seconds;
            _nextTickTime = Time.time + _dotInterval;
            _dotColor = color;
            RefreshVisual(ref _dotVisual, "ㄴ", color, Vector3.one * 0.45f, Vector3.up * 2.2f);
            Debug.Log($"[Status] {name} 지속 피해 {seconds:F1}s (틱 {damagePerTick:F1})");
        }

        // ㅁ(토) 격발 표식 설치
        public void InstallMark(Element element, float seconds, float bonusMultiplier, float burstDiameter, Color color)
        {
            _markElement = element;
            _markUntil = Time.time + seconds;
            _markBonusMultiplier = bonusMultiplier;
            _markBurstDiameter = burstDiameter;
            _markColor = color;
            RefreshVisual(ref _markVisual, "ㅁ", color, Vector3.one * 0.5f, Vector3.up * 2.8f);
            Debug.Log($"[Status] {name} 격발 표식 설치 ({element}, {seconds:F1}s)");
        }

        // 표식 격발 시도 — 술식이 적중했을 때 호출. 격발했으면 보너스 피해를 돌려준다.
        // [가정] 격발 조건 = 아무 술식 적중. 상합 5x5 매트릭스 확정 시 조건을 데이터로 교체.
        public bool TryDetonateMark(float incomingDamage, out float bonusDamage)
        {
            bonusDamage = 0f;
            if (!HasMark) return false;
            _markUntil = 0f;
            if (_markVisual != null) Destroy(_markVisual);
            bonusDamage = incomingDamage * _markBonusMultiplier;
            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 1.2f, _markColor, _markBurstDiameter, 0.4f);
            Debug.Log($"[Status] {name} 표식 격발! 보너스 {bonusDamage:F1} ({_markElement})");
            return true;
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
