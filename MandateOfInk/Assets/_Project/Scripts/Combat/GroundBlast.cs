using UnityEngine;

namespace MandateOfInk.Combat
{
    // 지면 예고 폭발(화 아키타입) — 플레이어 발밑에 장판이 예고되고, 퓨즈가 다 타면 터진다.
    // 장판을 보고 걸어 나가면 피할 수 있다(회피 문법). 표현은 반투명 원판 + 맥동.
    public sealed class GroundBlast : MonoBehaviour
    {
        private float _radius;
        private float _fuseSeconds;
        private float _damage;
        private float _elapsed;
        private Material _material;
        private Color _baseColor;

        public static void Spawn(Vector3 position, float radius, float fuseSeconds, float damage, Color color)
        {
            color.a = 0.30f;
            var go = SpellVisuals.CreateTranslucent(PrimitiveType.Cylinder, color,
                new Vector3(radius * 2f, 0.03f, radius * 2f), keepColliderAsTrigger: false);
            go.name = "GroundBlast";
            position.y = 0.05f;
            go.transform.position = position;

            var blast = go.AddComponent<GroundBlast>();
            blast._radius = radius;
            blast._fuseSeconds = Mathf.Max(fuseSeconds, 0.2f);
            blast._damage = damage;
            blast._material = go.GetComponent<MeshRenderer>().material;
            blast._baseColor = color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _fuseSeconds);
            // 맥동 — 터지기 직전일수록 빠르고 진하게
            var c = _baseColor;
            c.a = _baseColor.a * (0.6f + 0.4f * Mathf.PingPong(_elapsed * (2f + 6f * t), 1f)) * (0.6f + 0.8f * t);
            _material.color = c;

            if (_elapsed < _fuseSeconds) return;

            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.4f,
                new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.55f), _radius * 2.2f, 0.35f);
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null)
            {
                Vector3 flat = cc.transform.position - transform.position;
                flat.y = 0f;
                if (flat.magnitude <= _radius)
                {
                    var hp = cc.GetComponent<PlayerHealth>();
                    if (hp != null) hp.TakeDamage(_damage);
                }
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
