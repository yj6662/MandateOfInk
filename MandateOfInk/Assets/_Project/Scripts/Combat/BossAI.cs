using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 메카천수관음 프로토 보스 AI — 명시적 상태머신.
    // 텔레그래프 3종 어휘(전투코어루프 v0.3)를 보스 패턴으로 구현한다:
    //   느린(팔 부채 휘두르기 — 근접 광역, 받아치기 유도) / 빠른(다연장 부채꼴 — 회피 강제)
    //   / 페인트(멈칫했다 지연 타격 — 성급한 반응 처벌)
    // 수치는 전부 [가정] — 손맛·시간 개연성 튜닝은 사람 영역.
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class BossAI : MonoBehaviour
    {
        private enum State { Idle, Choose, Telegraph, FeintPause, ComboSlam, Recover }
        private enum Pattern { Sweep, Barrage, Feint }

        [Header("연결 (선택)")]
        [SerializeField] private BossArmRig _armRig; // 있으면 팔 내려찍기 패턴 사용
        [SerializeField] private float _slamMaxRange = 16f; // [가정] 이 거리 안이면 내려찍기 후보
        [SerializeField] private float _slamReach = 6.5f;   // [가정] 팔이 실제 땅에 닿는 수평 도달 거리 — 타깃을 여기까지로 클램프
        [SerializeField] private float _leanDegrees = 6f;   // [가정] 내려찍을 때 몸통이 타격 방향으로 숙는 각도

        [Header("[가정] 2페이즈 (HP 절반 이하) — 다중·연속 내려찍기")]
        [SerializeField] private float _enrageHpFraction = 0.5f;
        [SerializeField] private int _volleyArmCount = 3;
        [SerializeField] private float _volleySpreadRadius = 5f;
        [SerializeField] private int _comboSlamCount = 3;
        [SerializeField] private float _comboIntervalSeconds = 0.45f;

        [Header("[가정] 교전")]
        [SerializeField] private float _engageRange = 28f;
        [SerializeField] private float _turnSpeed = 3f;
        [SerializeField] private float _recoverSeconds = 1.6f;

        [Header("[가정] 느린 패턴 — 팔 부채 휘두르기(근접 광역)")]
        [SerializeField] private float _sweepTelegraphSeconds = 1.6f;
        [SerializeField] private float _sweepRange = 7f;
        [SerializeField] private float _sweepDamage = 22f;
        [SerializeField] private Color _slowTelegraphColor = new Color(0.85f, 0.15f, 0.1f);

        [Header("[가정] 빠른 패턴 — 다연장 부채꼴")]
        [SerializeField] private float _barrageTelegraphSeconds = 0.5f;
        [SerializeField] private int _barrageCount = 5;
        [SerializeField] private float _barrageSpreadDegrees = 42f;
        [SerializeField] private float _barrageProjectileSpeed = 14f;
        [SerializeField] private float _barrageDamage = 8f;
        [SerializeField] private GameObject _projectileVfxPrefab;
        [SerializeField] private Color _fastTelegraphColor = new Color(1f, 0.75f, 0.1f);

        [Header("[가정] 페인트 — 멈칫 후 지연 타격")]
        [SerializeField] private float _feintTelegraphSeconds = 0.7f;
        [SerializeField] private float _feintPauseSeconds = 0.55f;
        [SerializeField] private float _feintStrikeRange = 8f;
        [SerializeField] private float _feintDamage = 15f;

        private State _state = State.Idle;
        private Pattern _pattern;
        private float _timer;
        private Quaternion _yawRotation;
        private float _lean;
        private int _comboRemaining;
        private EnemyHealth _health;
        private EnemyStatus _status;
        private Transform _player;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) _player = cc.transform;
            _yawRotation = transform.rotation;
        }

        private void Update()
        {
            if (_player == null) return;
            // 그로기 — 받아치기 등으로 포이즈가 무너지면 잠시 행동 불능
            if (_status == null) TryGetComponent(out _status);
            if (_status != null && _status.IsGroggy) return;
            float dist = Vector3.Distance(transform.position, _player.position);
            FacePlayer();

            switch (_state)
            {
                case State.Idle:
                    if (dist <= _engageRange) { _state = State.Choose; Debug.Log("[Boss] 교전 개시"); }
                    break;

                case State.Choose:
                    // 팔 내려찍기 [가정 40%]: 텔레그래프는 팔의 치켜들기가 담당 — 즉시 발동 후 회복
                    bool enraged = _health != null && _health.NormalizedHp <= _enrageHpFraction;
                    if (_armRig != null && dist <= _slamMaxRange && _armRig.HasIdleArm
                        && Random.value < (enraged ? 0.55f : 0.4f))
                    {
                        // 타깃은 모션 시작 시점의 플레이어 위치 — 예비 동작을 보고 피할 수 있다
                        if (enraged && Random.value < 0.5f)
                        {
                            // 변주 1: 여러 팔 동시 내려찍기 — 중심 정조준 + 주변 흩뿌리기
                            _armRig.SlamVolley(ClampSlamTarget(_player.position), _volleyArmCount, _volleySpreadRadius);
                            _timer = 0f;
                            _state = State.Recover;
                        }
                        else if (enraged)
                        {
                            // 변주 2: 연속 내려찍기 — 발동 시마다 그 순간의 위치를 새로 조준
                            _comboRemaining = _comboSlamCount;
                            _timer = _comboIntervalSeconds; // 첫 발은 즉시
                            _state = State.ComboSlam;
                        }
                        else
                        {
                            _armRig.SlamRandomArm(ClampSlamTarget(_player.position));
                            _timer = 0f;
                            _state = State.Recover;
                        }
                        break;
                    }
                    // 거리 기반 가중 선택 [가정]: 가까우면 휘두르기/페인트, 멀면 다연장
                    float r = Random.value;
                    if (dist <= _sweepRange) _pattern = r < 0.55f ? Pattern.Sweep : (r < 0.8f ? Pattern.Feint : Pattern.Barrage);
                    else _pattern = r < 0.65f ? Pattern.Barrage : (r < 0.85f ? Pattern.Feint : Pattern.Sweep);
                    _timer = 0f;
                    _state = State.Telegraph;
                    break;

                case State.Telegraph:
                    _timer += Time.deltaTime; // 시간 감속의 영향을 받는다 — 작도 중엔 예비 동작도 느려짐
                    float duration = TelegraphSeconds(_pattern);
                    SetTint(Color.Lerp(Color.white, TelegraphColor(_pattern), _timer / duration));
                    if (_timer >= duration)
                    {
                        if (_pattern == Pattern.Feint) { SetTint(Color.white); _timer = 0f; _state = State.FeintPause; }
                        else { Execute(_pattern, dist); }
                    }
                    break;

                case State.FeintPause: // 멈칫 — 여기서 성급하게 반응하면 처벌당한다
                    _timer += Time.deltaTime;
                    if (_timer >= _feintPauseSeconds)
                    {
                        Execute(Pattern.Feint, dist);
                    }
                    break;

                case State.ComboSlam: // 2페이즈: 일정 간격으로 연달아 내려찍기
                    _timer += Time.deltaTime;
                    if (_timer >= _comboIntervalSeconds)
                    {
                        _timer = 0f;
                        if (_armRig != null && _armRig.HasIdleArm)
                            _armRig.SlamRandomArm(ClampSlamTarget(_player.position), frantic: true);
                        _comboRemaining--;
                        if (_comboRemaining <= 0) _state = State.Recover;
                    }
                    break;

                case State.Recover:
                    _timer += Time.deltaTime;
                    if (_timer >= _recoverSeconds) _state = State.Choose;
                    break;
            }
        }

        // 사거리 밖이면 그 방향 최대 도달 지점으로 클램프 — 허공 타격 금지
        private Vector3 ClampSlamTarget(Vector3 playerPos)
        {
            Vector3 flat = playerPos - transform.position;
            flat.y = 0f;
            return flat.magnitude > _slamReach
                ? transform.position + flat.normalized * _slamReach
                : playerPos;
        }

        private float TelegraphSeconds(Pattern p) =>
            p == Pattern.Sweep ? _sweepTelegraphSeconds :
            p == Pattern.Barrage ? _barrageTelegraphSeconds : _feintTelegraphSeconds;

        private Color TelegraphColor(Pattern p) =>
            p == Pattern.Barrage ? _fastTelegraphColor : _slowTelegraphColor;

        private void Execute(Pattern pattern, float dist)
        {
            SetTint(Color.white);
            switch (pattern)
            {
                case Pattern.Sweep:
                    SpellVisuals.SpawnBurst(transform.position + Vector3.up * 2f,
                        new Color(0.7f, 0.15f, 0.1f, 0.4f), _sweepRange * 2f, 0.45f);
                    if (dist <= _sweepRange * 1.15f) DamagePlayer(_sweepDamage, "팔 휘두르기");
                    else Debug.Log("[Boss] 휘두르기 빗나감");
                    break;

                case Pattern.Barrage:
                    FireBarrage();
                    break;

                case Pattern.Feint:
                    SpellVisuals.SpawnBurst(transform.position + Vector3.up * 2f,
                        new Color(0.7f, 0.15f, 0.1f, 0.35f), _feintStrikeRange * 1.6f, 0.35f);
                    if (dist <= _feintStrikeRange) DamagePlayer(_feintDamage, "페인트 후 타격");
                    else Debug.Log("[Boss] 페인트 타격 빗나감");
                    break;
            }
            _timer = 0f;
            _state = State.Recover;
        }

        private void FireBarrage()
        {
            Vector3 origin = transform.position + Vector3.up * 2.5f;
            Vector3 toPlayer = (_player.position + Vector3.up * 0.9f - origin).normalized;
            for (int i = 0; i < _barrageCount; i++)
            {
                float t = _barrageCount > 1 ? (float)i / (_barrageCount - 1) - 0.5f : 0f;
                var dir = Quaternion.AngleAxis(t * _barrageSpreadDegrees, Vector3.up) * toPlayer;
                var go = new GameObject($"BossProjectile_{i}");
                go.transform.SetPositionAndRotation(origin + dir * 1.5f, Quaternion.LookRotation(dir));
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.25f;
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                go.AddComponent<EnemyProjectile>().Init(_barrageProjectileSpeed, _barrageDamage, 5f,
                    _health.Definition != null ? _health.Definition.Element : Data.Element.Metal, _health);
                if (_projectileVfxPrefab != null)
                    Instantiate(_projectileVfxPrefab, go.transform.position, go.transform.rotation, go.transform);
            }
            Debug.Log($"[Boss] 다연장 {_barrageCount}발");
        }

        private void DamagePlayer(float damage, string label)
        {
            var hp = _player.GetComponentInParent<PlayerHealth>();
            if (hp == null) hp = _player.GetComponent<PlayerHealth>();
            if (hp != null) { hp.TakeDamage(damage); Debug.Log($"[Boss] {label} 적중"); }
        }

        private void FacePlayer()
        {
            // 요(yaw)와 피치(lean)를 분리 관리 — 팔이 내려찍는 동안 몸통이 타격 방향으로 숙는다
            Vector3 flat = _player.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                _yawRotation = Quaternion.Slerp(_yawRotation,
                    Quaternion.LookRotation(flat), _turnSpeed * Time.deltaTime);
            float targetLean = _armRig != null ? _armRig.MaxLeanWeight * _leanDegrees : 0f;
            _lean = Mathf.MoveTowards(_lean, targetLean, 40f * Time.deltaTime);
            transform.rotation = _yawRotation * Quaternion.Euler(_lean, 0f, 0f);
        }

        private void SetTint(Color c)
        {
            _mpb.SetColor(BaseColorId, c);
            foreach (var r in _renderers) r.SetPropertyBlock(_mpb);
        }
    }
}
