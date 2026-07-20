using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 적 체력 — 수치는 전부 EnemyDefinitionSO(데이터)에서 온다.
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO _definition;
        [SerializeField] private GameObject _deathVfxPrefab; // 사망 연출(임시)

        public EnemyDefinitionSO Definition => _definition;
        public float CurrentHp { get; private set; }
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
            CurrentHp -= amount;
            Debug.Log($"[Enemy] {name} 피해 {amount:F1} -> 남은 HP {CurrentHp:F1}");
            if (CurrentHp <= 0f) Die();
        }

        private void Die()
        {
            Debug.Log($"[Enemy] {name} 격파");
            if (_deathVfxPrefab != null)
            {
                var vfx = Instantiate(_deathVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f); // [가정] VFX 잔류 상한
            }
            Destroy(gameObject);
        }
    }
}
