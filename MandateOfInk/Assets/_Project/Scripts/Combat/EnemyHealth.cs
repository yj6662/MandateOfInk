using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 적 체력 — 수치는 전부 EnemyDefinitionSO(데이터)에서 온다.
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO _definition;
        [SerializeField] private GameObject _deathVfxPrefab; // 사망 연출(임시)
        [SerializeField] private float _deathDestroySeconds; // [가정] 사망 애니 재생 여유 — 0이면 즉시 파괴
        [Tooltip("처치 시 적 Id를 발신 — 사냥 방(榜) 집계 등이 구독")]
        [SerializeField] private MandateOfInk.Core.Events.StringEventChannelSO _enemyKilledChannel;

        /// <summary>사망 확정 순간 — 프레젠테이션(사망 애니메이션)이 구독한다.</summary>
        public event System.Action Died;

        public EnemyDefinitionSO Definition => _definition;
        public float CurrentHp { get; private set; }
        public bool IsDead { get; private set; }
        public int BonusCoins { get; set; } // 앙괭이가 훔친 통보 등 — 처치 시 기본 드롭에 합산
        public float MaxHp => _definition != null ? _definition.MaxHp : 1f;
        public float NormalizedHp => MaxHp > 0f ? CurrentHp / MaxHp : 0f;

        // 에디터 테스트 전용 진입점 — 페이즈 전환 확인용 (EditorTools의 인스펙터 버튼이 호출)
        public void DebugSetHpFraction(float fraction)
        {
            CurrentHp = MaxHp * Mathf.Clamp01(fraction);
            Debug.Log($"[Enemy] {name} HP 강제 설정 -> {CurrentHp:F1}/{MaxHp:F1}");
            if (CurrentHp <= 0f) Die();
        }

        private void Awake()
        {
            CurrentHp = _definition != null ? _definition.MaxHp : 1f;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            CurrentHp -= amount;
            Debug.Log($"[Enemy] {name} 피해 {amount:F1} -> 남은 HP {CurrentHp:F1}");
            if (CurrentHp <= 0f) Die();
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Debug.Log($"[Enemy] {name} 격파");
            // 조선통보 지급 — 소울류처럼 처치 즉시 획득 (통보=화폐, 마석과 분리)
            int coins = (_definition != null ? _definition.CoinDrop : 0) + BonusCoins;
            if (coins > 0)
            {
                var wallet = FindFirstObjectByType<PlayerWallet>();
                if (wallet != null) wallet.Add(coins);
                if (BonusCoins > 0) Debug.Log($"[Enemy] {name} — 훔친 통보 {BonusCoins} 회수");
            }
            // 사망 연출 — 수묵 먹빛 번짐(오행부 미학). 화염 폭발 프리팹은 폐기, 코드 파티클로 대체.
            SpellVisuals.SpawnInkDeathBurst(transform.position);
            if (_deathVfxPrefab != null)
            {
                // 별도 사망 VFX 프리팹이 배선돼 있으면 함께 재생(현재 씬 배선은 비움 — 수묵 연출만 사용)
                var vfx = Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f); // [가정] VFX 잔류 상한
            }
            Died?.Invoke();
            if (_enemyKilledChannel != null && _definition != null && !string.IsNullOrEmpty(_definition.Id))
                _enemyKilledChannel.Raise(_definition.Id);
            // 사망 애니 여유가 있으면 행동·충돌만 끄고 지연 파괴(시체 잔류)
            if (_deathDestroySeconds > 0f)
            {
                if (TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
                foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
                Destroy(gameObject, _deathDestroySeconds);
            }
            else Destroy(gameObject);
        }
    }
}
