using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 졸개 AI — 명시적 상태머신: 대기 -> 추적 -> 텔레그래프(예비 동작) -> (페인트면 멈칫) -> 공격 -> 회복.
    // 텔레그래프 어휘 3종(전투코어루프 v0.3):
    //   느린(진홍, 1.2s) = 강타 — 굵고 느린 투사체, 고피해. 받아치기 유도.
    //   빠른(주황, 0.8s) = 속사 — 빠른 투사체, 저피해. 회피 강제.
    //   페인트(진홍 위장, 0.7s + 멈칫 0.5s) = 느린 것처럼 보이다 멈칫 후 지연 발사 — 성급한 회피 처벌.
    // 페인트가 "느린"과 같은 색인 것이 의도다: 색만 보고 즉시 구르면 멈칫 후의 지연탄에 맞는다.
    // 시간·피해 수치는 전부 EnemyDefinitionSO(데이터), 교전 거리류는 [가정] 필드.
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyAI : MonoBehaviour
    {
        private enum State { Idle, Chase, Telegraph, FeintPause, Recover }
        private enum Pattern { Slow, Fast, Feint }

        [Header("[가정] 교전 파라미터 — 원거리형")]
        [SerializeField] private float _detectRange = 18f;
        [SerializeField] private float _attackRange = 9f;   // 이 거리 안에 들어오면 멈추고 투사체 공격
        [SerializeField] private float _recoverSeconds = 1.2f;
        [SerializeField] private Color _telegraphColor = new Color(0.9f, 0.25f, 0.2f); // 느린·페인트 공용(위장)
        [SerializeField] private Color _fastTelegraphColor = new Color(1f, 0.75f, 0.1f);

        [Header("[가정] 패턴 선택 가중치")]
        [Range(0f, 1f)] [SerializeField] private float _slowWeight = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float _fastWeight = 0.4f; // 나머지가 페인트

        [Header("[가정] 투사체")]
        [SerializeField] private GameObject _projectileVfxPrefab;
        [SerializeField] private float _projectileSpeed = 12f;
        [SerializeField] private float _projectileLifetime = 4f;
        [SerializeField] private float _slowProjectileSpeedScale = 0.75f; // 강타는 굵고 느리게
        [SerializeField] private float _fastProjectileSpeedScale = 1.4f;

        private State _state = State.Idle;
        private Pattern _pattern;
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
                    if (dist <= _attackRange) { ChoosePattern(); }
                    else if (dist > _detectRange * 1.5f) _state = State.Idle;
                    break;

                case State.Telegraph:
                {
                    FacePlayer();
                    _timer += Time.deltaTime; // 시간 감속의 영향을 받는다 — 작도 중에는 예비 동작도 느려진다
                    float duration = TelegraphSeconds(def);
                    SetTint(Color.Lerp(_baseColor, TelegraphColor(), _timer / duration));
                    if (_timer >= duration)
                    {
                        if (_pattern == Pattern.Feint)
                        {
                            // 멈칫 — 색을 유지한 채 얼어붙는다. 여기서 미리 구르면 지연탄에 맞는다.
                            _timer = 0f;
                            _state = State.FeintPause;
                        }
                        else Attack(def);
                    }
                    break;
                }

                case State.FeintPause:
                    // 조준을 멈춘다(FacePlayer 없음) — 시선 고정도 페인트의 일부
                    _timer += Time.deltaTime;
                    if (_timer >= def.FeintPauseSeconds) Attack(def);
                    break;

                case State.Recover:
                    _timer += Time.deltaTime;
                    if (_timer >= _recoverSeconds) { _state = State.Chase; }
                    break;
            }
        }

        private void ChoosePattern()
        {
            float r = Random.value;
            _pattern = r < _slowWeight ? Pattern.Slow
                : r < _slowWeight + _fastWeight ? Pattern.Fast : Pattern.Feint;
            _timer = 0f;
            _state = State.Telegraph;
        }

        private float TelegraphSeconds(EnemyDefinitionSO def) =>
            _pattern == Pattern.Slow ? def.SlowTelegraphSeconds :
            _pattern == Pattern.Fast ? def.FastTelegraphSeconds : def.FeintTelegraphSeconds;

        private Color TelegraphColor() =>
            _pattern == Pattern.Fast ? _fastTelegraphColor : _telegraphColor; // 페인트=느린 색 위장

        // 공격: 텔레그래프(또는 멈칫)가 끝난 시점의 플레이어 위치로 투사체를 쏜다.
        // 유도 없음 — 예비 동작을 보고 옆으로 움직이면 피할 수 있다(소울라이크 회피 문법).
        private void Attack(EnemyDefinitionSO def)
        {
            SetTint(_baseColor);

            float damage = def.AttackDamage;
            float speed = _projectileSpeed;
            float radius = 0.22f;
            switch (_pattern)
            {
                case Pattern.Slow:
                    damage *= def.SlowDamageMultiplier;
                    speed *= _slowProjectileSpeedScale;
                    radius = 0.38f; // 굵은 강타탄
                    break;
                case Pattern.Fast:
                    damage *= def.FastDamageMultiplier;
                    speed *= _fastProjectileSpeedScale;
                    radius = 0.16f;
                    break;
            }

            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            Vector3 aim = (_player.position + Vector3.up * 0.9f) - origin;

            var go = new GameObject($"EnemyProjectile_{name}");
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(aim));
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            go.AddComponent<EnemyProjectile>().Init(speed, damage, _projectileLifetime);
            if (_projectileVfxPrefab != null)
            {
                var vfx = Instantiate(_projectileVfxPrefab, go.transform.position, go.transform.rotation, go.transform);
                if (_pattern == Pattern.Slow) vfx.transform.localScale *= 1.6f; // 강타탄은 크게 보인다
                else if (_pattern == Pattern.Fast) vfx.transform.localScale *= 0.7f;
            }

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
