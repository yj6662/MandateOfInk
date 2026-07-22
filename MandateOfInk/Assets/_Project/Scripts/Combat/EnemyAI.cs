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
        private enum State { Idle, Chase, Telegraph, FeintPause, Charge, Recover }
        private enum Pattern { Slow, Fast, Feint }

        /// <summary>훔치기 성공 순간(앙괭이) — 프레젠테이션(애니메이션)이 구독한다.</summary>
        public event System.Action StoleCoins;

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
        private EnemyStatus _status; // 속박(ㄱ받침) 감속 — 런타임에 부착되므로 지연 조회
        private Vector3 _chargeDirection; // 토 돌진
        private float _chargeTimer;
        private bool _chargeHit;
        private float _moltenClock;       // 화마 경화 순환 시계
        private bool _isHardenedPhase;
        private Transform _player;
        private Renderer[] _renderers; // 모델 자식 포함 전체 — 텔레그래프 틴트 대상
        private MaterialPropertyBlock _mpb;
        private Color _baseColor = Color.white;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            if (_renderers.Length > 0 && _renderers[0].sharedMaterial != null
                && _renderers[0].sharedMaterial.HasProperty(BaseColorId))
                _baseColor = _renderers[0].sharedMaterial.GetColor(BaseColorId);
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
            // 그로기 — 행동 불능(빈틈). 텔레그래프 중이었어도 얼어붙는다.
            if (_status == null) TryGetComponent(out _status);
            if (_status != null && _status.IsGroggy) return;
            float dist = Vector3.Distance(transform.position, _player.position);
            var def = _health.Definition;

            // 앙괭이(도둑) — 상태머신 밖의 독자 행동: 훔치고, 도망치고, 절대 싸우지 않는다
            if (def.Archetype == EnemyArchetype.Thief)
            {
                ThiefUpdate(def, dist);
                return;
            }

            // 화마 경화 순환 — 굳으면 멈추고 단단해지고(피해 급감), 끓어야 움직이고 공격한다.
            // 굳은 틈은 「기다렸다 끓을 때 친다」가 아니라 상합 셋업(표식 설치)의 틈으로 쓰인다.
            if (def.Archetype == EnemyArchetype.MoltenCycler && _state != State.Idle)
            {
                _moltenClock += Time.deltaTime;
                float cycle = def.MoltenSeconds + def.HardenSeconds;
                bool harden = (_moltenClock % cycle) >= def.MoltenSeconds;
                if (harden != _isHardenedPhase)
                {
                    _isHardenedPhase = harden;
                    if (harden)
                    {
                        _status?.SetHardened(def.HardenDamageMultiplier, def.HardenSeconds);
                        SetTint(new Color(0.25f, 0.2f, 0.18f)); // 식어 굳은 쇳빛
                        Debug.Log($"[{name}] 경화 — 굳는다");
                    }
                    else
                    {
                        SetTint(_baseColor);
                        Debug.Log($"[{name}] 용융 — 끓어오른다");
                    }
                }
                if (_isHardenedPhase) return; // 굳은 동안은 정지
            }

            switch (_state)
            {
                case State.Idle:
                    if (dist <= _detectRange) _state = State.Chase;
                    break;

                case State.Chase:
                    FacePlayer();
                    float slow = _status != null ? _status.MoveMultiplier : 1f;
                    transform.position += transform.forward * (def.MoveSpeed * slow * Time.deltaTime);
                    if (dist <= _attackRange)
                    {
                        // 도깨비불: 달라붙는 순간 작은 파열과 함께 스스로 꺼진다
                        if (def.Archetype == EnemyArchetype.SwarmBurster) { Burst(def, dist); break; }
                        ChoosePattern();
                    }
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

                case State.Charge: // 토 — 확정된 직선으로 무겁게 돌진(유도 없음, 옆으로 피한다)
                    _chargeTimer += Time.deltaTime;
                    transform.position += _chargeDirection * (def.ArchetypePrimary * Time.deltaTime);
                    if (!_chargeHit && Vector3.Distance(transform.position, _player.position) < 1.7f)
                    {
                        _chargeHit = true;
                        var hp = _player.GetComponentInParent<PlayerHealth>();
                        if (hp == null) hp = _player.GetComponent<PlayerHealth>();
                        if (hp != null) hp.TakeDamage(def.AttackDamage);
                        SpellVisuals.SpawnBurst(_player.position + Vector3.up * 1f,
                            new Color(0.6f, 0.5f, 0.25f, 0.6f), 1.6f, 0.3f);
                    }
                    if (_chargeTimer >= def.ArchetypeSecondary)
                    {
                        SetTint(_baseColor);
                        _timer = 0f;
                        _state = State.Recover;
                    }
                    break;

                case State.Recover:
                    _timer += Time.deltaTime;
                    if (_timer >= _recoverSeconds) { _state = State.Chase; }
                    break;
            }
        }

        private void ChoosePattern()
        {
            var archetype = _health.Definition.Archetype;
            if (archetype == EnemyArchetype.RangedBasic)
            {
                // 기본 원거리만 3종 어휘를 섞는다
                float r = Random.value;
                _pattern = r < _slowWeight ? Pattern.Slow
                    : r < _slowWeight + _fastWeight ? Pattern.Fast : Pattern.Feint;
            }
            else
            {
                // 아키타입 전용기: 금(참격)은 빠른 예비, 나머지는 느린 예비 — 어휘 색으로 읽힌다
                _pattern = archetype == EnemyArchetype.MetalSlasher ? Pattern.Fast : Pattern.Slow;
            }
            _timer = 0f;
            _state = State.Telegraph;
        }

        private float TelegraphSeconds(EnemyDefinitionSO def) =>
            _pattern == Pattern.Slow ? def.SlowTelegraphSeconds :
            _pattern == Pattern.Fast ? def.FastTelegraphSeconds : def.FeintTelegraphSeconds;

        private Color TelegraphColor() =>
            _pattern == Pattern.Fast ? _fastTelegraphColor : _telegraphColor; // 페인트=느린 색 위장

        // 공격: 텔레그래프(또는 멈칫)가 끝난 시점의 플레이어 위치를 노린다.
        // 유도 없음 — 예비 동작을 보고 옆으로 움직이면 피할 수 있다(소울라이크 회피 문법).
        private void Attack(EnemyDefinitionSO def)
        {
            // 아키타입 전용기 — 속성이 공격 「동사」를 바꾼다(전투코어루프 §9)
            switch (def.Archetype)
            {
                case EnemyArchetype.MoltenCycler: // 화마 — 끓는 동안엔 화 폭발과 동일
                case EnemyArchetype.FireBomber:
                    // 화: 플레이어 발밑 예고 장판 — 퓨즈 안에 걸어 나가면 회피
                    SetTint(_baseColor);
                    GroundBlast.Spawn(_player.position, def.ArchetypePrimary, def.ArchetypeSecondary,
                        def.AttackDamage, new Color(0.85f, 0.3f, 0.15f));
                    EndAttack();
                    return;

                case EnemyArchetype.MetalSlasher:
                    // 금: 근접 쾌속 참격 — 전방 반경 안이면 즉시 피해. 회복이 짧아 연타 리듬이 된다
                    SetTint(_baseColor);
                    Vector3 slashCenter = transform.position + transform.forward * 1.4f + Vector3.up * 1f;
                    SpellVisuals.SpawnBurst(slashCenter, new Color(0.8f, 0.82f, 0.85f, 0.5f), def.ArchetypePrimary * 1.6f, 0.18f);
                    if (Vector3.Distance(slashCenter, _player.position + Vector3.up * 1f) <= def.ArchetypePrimary)
                    {
                        var hp = _player.GetComponentInParent<PlayerHealth>();
                        if (hp == null) hp = _player.GetComponent<PlayerHealth>();
                        if (hp != null) hp.TakeDamage(def.AttackDamage);
                    }
                    EndAttack();
                    return;

                case EnemyArchetype.EarthCharger:
                    // 토: 조준 확정 후 직선 돌진 — Charge 상태로 전환
                    Vector3 dir = _player.position - transform.position;
                    dir.y = 0f;
                    _chargeDirection = dir.normalized;
                    _chargeTimer = 0f;
                    _chargeHit = false;
                    _state = State.Charge;
                    return;
            }

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
            var proj = go.AddComponent<EnemyProjectile>();
            proj.Init(speed, damage, _projectileLifetime, def.Element, _health);
            // 목·수 아키타입은 투사체에 탑재물을 싣는다
            if (def.Archetype == EnemyArchetype.WoodBinder)
                proj.SetBindPayload(def.ArchetypePrimary, def.ArchetypeSecondary);
            else if (def.Archetype == EnemyArchetype.WaterPuller)
                proj.SetPullPayload(def.ArchetypePrimary, def.ArchetypeSecondary);
            if (_projectileVfxPrefab != null)
            {
                var vfx = Instantiate(_projectileVfxPrefab, go.transform.position, go.transform.rotation, go.transform);
                if (_pattern == Pattern.Slow) vfx.transform.localScale *= 1.6f; // 강타탄은 크게 보인다
                else if (_pattern == Pattern.Fast) vfx.transform.localScale *= 0.7f;
            }

            EndAttack();
        }

        private void EndAttack()
        {
            _state = State.Recover;
            _timer = 0f;
        }

        // 도깨비불 자폭 파열 — Primary=발화 거리(공격 사거리로 씬 배선), Secondary=파열 반경
        private void Burst(EnemyDefinitionSO def, float dist)
        {
            SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.8f,
                new Color(0.95f, 0.55f, 0.2f, 0.6f), def.ArchetypeSecondary * 2f, 0.3f);
            if (dist <= def.ArchetypeSecondary)
            {
                var hp = _player.GetComponentInParent<PlayerHealth>();
                if (hp == null) hp = _player.GetComponent<PlayerHealth>();
                if (hp != null) hp.TakeDamage(def.AttackDamage);
            }
            _health.TakeDamage(float.MaxValue); // 스스로 꺼짐 — 드롭(먹 부스러기 수준)은 정상 지급
        }

        // 앙괭이 — 떨어진 통보가 있으면 달려가 훔치고, 아니면 플레이어를 피해 다닌다.
        // 정수리 불빛(전승)은 도주 중에도 위치를 드러낸다 — 표현은 밝은 틴트로 대신 [가정]
        private void ThiefUpdate(EnemyDefinitionSO def, float dist)
        {
            float slow = _status != null ? _status.MoveMultiplier : 1f;
            var drop = FindFirstObjectByType<DroppedCoins>();

            Vector3 moveDir = Vector3.zero;
            if (drop != null)
            {
                Vector3 toDrop = drop.transform.position - transform.position;
                toDrop.y = 0f;
                if (toDrop.magnitude <= 1.2f)
                {
                    // 훔친다 — 처치해야 돌려받는다(전량 + 기본 드롭이 웃돈 역할)
                    _health.BonusCoins += drop.Amount;
                    Debug.Log($"[앙괭이] 통보 {drop.Amount} 훔침! 잡아서 되찾아라");
                    Destroy(drop.gameObject);
                    StoleCoins?.Invoke();
                }
                else moveDir = toDrop.normalized;
            }
            if (moveDir == Vector3.zero && dist < _detectRange)
            {
                Vector3 away = transform.position - _player.position;
                away.y = 0f;
                moveDir = away.normalized; // 도주
            }
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(moveDir), 10f * Time.deltaTime);
                transform.position += moveDir * (def.MoveSpeed * slow * Time.deltaTime);
            }
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
            if (_renderers == null) return;
            _mpb.SetColor(BaseColorId, c);
            foreach (var r in _renderers)
                if (r != null) r.SetPropertyBlock(_mpb);
        }
    }
}
