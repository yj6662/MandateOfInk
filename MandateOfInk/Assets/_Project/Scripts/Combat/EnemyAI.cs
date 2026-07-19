using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 졸개 AI — 명시적 상태머신: 대기 -> 추적 -> 텔레그래프(예비 동작) -> 공격 -> 회복.
    // 능력치(이동 속도·텔레그래프 시간·공격력)는 EnemyDefinitionSO에서, 교전 거리류는 [가정] 필드.
    // 텔레그래프는 몸 색이 서서히 붉어지는 것으로 표시(디제틱 — HUD 금지 항목인 HP바 없음).
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyAI : MonoBehaviour
    {
        private enum State { Idle, Chase, Telegraph, Attack, Recover }

        [Header("[가정] 교전 파라미터 — 원거리형")]
        [SerializeField] private float _detectRange = 18f;
        [SerializeField] private float _attackRange = 9f;   // 이 거리 안에 들어오면 멈추고 투사체 공격
        [SerializeField] private float _recoverSeconds = 1.2f;
        [SerializeField] private Color _telegraphColor = new Color(0.9f, 0.25f, 0.2f);

        [Header("[가정] 투사체")]
        [SerializeField] private GameObject _projectileVfxPrefab;
        [SerializeField] private float _projectileSpeed = 12f;
        [SerializeField] private float _projectileLifetime = 4f;

        private State _state = State.Idle;
        private float _timer;
        private EnemyHealth _health;
        private Transform _player;
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Color _baseColor = Color.white;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            if (_renderer != null && _renderer.sharedMaterial != null && _renderer.sharedMaterial.HasProperty(BaseColorId))
                _baseColor = _renderer.sharedMaterial.GetColor(BaseColorId);
        }

        private void Start()
        {
            // 프로토: 플레이어는 씬에 하나뿐인 CharacterController로 찾는다
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) _player = cc.transform;
        }

        private void Update()
        {
            if (_player == null || _health.Definition == null) return;
            float dist = Vector3.Distance(transform.position, _player.position);
            var def = _health.Definition;

            switch (_state)
            {
                case State.Idle:
                    if (dist <= _detectRange) _state = State.Chase;
                    break;

                case State.Chase:
                    FacePlayer();
                    transform.position += transform.forward * (def.MoveSpeed * Time.deltaTime);
                    if (dist <= _attackRange) { _state = State.Telegraph; _timer = 0f; }
                    else if (dist > _detectRange * 1.5f) _state = State.Idle;
                    break;

                case State.Telegraph:
                    FacePlayer();
                    _timer += Time.deltaTime; // 시간 감속의 영향을 받는다 — 작도 중에는 예비 동작도 느려진다
                    SetTint(Color.Lerp(_baseColor, _telegraphColor, _timer / def.TelegraphSeconds));
                    if (_timer >= def.TelegraphSeconds) Attack(dist, def);
                    break;

                case State.Recover:
                    _timer += Time.deltaTime;
                    if (_timer >= _recoverSeconds) { _state = State.Chase; }
                    break;
            }
        }

        // 원거리 공격: 텔레그래프가 끝난 시점의 플레이어 위치로 투사체를 쏜다.
        // 유도 없음 — 텔레그래프를 보고 옆으로 움직이면 피할 수 있다(소울라이크 회피 문법).
        private void Attack(float dist, EnemyDefinitionSO def)
        {
            SetTint(_baseColor);

            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            Vector3 aim = (_player.position + Vector3.up * 0.9f) - origin;

            var go = new GameObject($"EnemyProjectile_{name}");
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(aim));
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.22f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            go.AddComponent<EnemyProjectile>().Init(_projectileSpeed, def.AttackDamage, _projectileLifetime);
            if (_projectileVfxPrefab != null)
                Instantiate(_projectileVfxPrefab, go.transform.position, go.transform.rotation, go.transform);

            _state = State.Recover;
            _timer = 0f;
        }

        private void FacePlayer()
        {
            Vector3 flat = _player.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(flat), 8f * Time.deltaTime);
        }

        private void SetTint(Color c)
        {
            if (_renderer == null) return;
            _mpb.SetColor(BaseColorId, c);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
