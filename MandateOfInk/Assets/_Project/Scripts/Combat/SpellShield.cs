using UnityEngine;

namespace MandateOfInk.Combat
{
    // 방어 진(ㅓ=전방 막, ㅜ=광역 돔) — 지속 시간 동안 적 투사체를 막는다.
    // 트리거 콜라이더라 이동을 방해하지 않고, EnemyProjectile이 이 컴포넌트를 감지해 소멸한다.
    // [가정] 흡수량(Absorb)은 프로토에서 미적용 — 지속 시간 동안 무제한 차단. M1 실측에서 재검.
    public sealed class SpellShield : MonoBehaviour
    {
        private float _duration;
        private float _elapsed;
        private Material _material;
        private Color _baseColor;

        public void Init(float duration, Material material)
        {
            _duration = Mathf.Max(duration, 0.5f);
            _material = material;
            _baseColor = material.color;
        }

        // 적 투사체를 막았을 때 (EnemyProjectile이 호출)
        public void OnBlocked(Vector3 hitPosition)
        {
            SpellVisuals.SpawnBurst(hitPosition, _baseColor, 0.7f, 0.25f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            // 만료가 다가올수록 옅어진다 — 남은 시간이 읽히게
            var c = _baseColor;
            c.a *= Mathf.Lerp(1f, 0.25f, t);
            _material.color = c;
            if (_elapsed >= _duration) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
