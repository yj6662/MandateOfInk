using UnityEngine;

namespace MandateOfInk.Combat
{
    // 보스 기계 팔 하나의 절차 애니메이션 — 리지드 페어런팅(스키닝 없음) 원칙.
    // 어깨(이 GO) - 팔꿈치 - 손목 3관절 체인. 메시는 상완/전완/손 3조각으로 분할되어
    // 각 관절 밑에 매달린다.
    //
    // 타격 파이프라인(박력 문법):
    //   대기 -> 치켜들기(크게 감아올림, 붉은 텔레그래프) -> 정점 홀드(부들부들 떨며 멈칫)
    //   -> 조준 확정(이후 무유도) -> 낙하(가속, 목표보다 더 꽂히는 과회전)
    //   -> 착지(광역 피해 + 이중 충격파 + 화면 흔들림) -> 반동(살짝 튕겨 올라옴)
    //   -> 꽂힘 유지(한 박자) -> 복귀.
    public sealed class BossArm : MonoBehaviour
    {
        private enum Phase { Rest, Raise, Hold, Slam, Recoil, Plant, Return }

        [Header("관절 연결 (조립 시 설정)")]
        [SerializeField] private Transform _elbow;
        [SerializeField] private Transform _wrist;
        [SerializeField] private float _handLength = 1.8f; // 손목 -> 손끝 거리
        [SerializeField] private Transform _elbowRod;      // 어깨-팔꿈치 황동 피스톤 로드
        [SerializeField] private Transform _wristRod;      // 팔꿈치-손목 황동 피스톤 로드
        [SerializeField] private Transform _forearmSleeve; // 전완 슬리브 컨테이너 — 신장 시 손목을 추적해 늘어난다(분리 방지)
        [SerializeField] private float _extendFactor = 2.2f;  // 타격 시 관절 신장 배율(텔레스코픽)
        [SerializeField] private float _primeFactor = 1.08f;  // 홀드 중 예열 신장(치익)

        [Header("[가정] 타이밍 — 육중함: 느린 예비·긴 홀드·짧은 낙하·긴 꽂힘")]
        [SerializeField] private float _raiseSeconds = 1.3f;   // 텔레그래프(느린 계열)
        [SerializeField] private float _holdSeconds = 0.5f;    // 정점에서 떨며 멈칫
        [SerializeField] private float _slamSeconds = 0.28f; // 스톱모션 단이 읽히는 최소 길이
        [SerializeField] private float _recoilSeconds = 0.22f;
        [SerializeField] private float _plantSeconds = 0.75f;  // 땅에 꽂힌 채 유지
        [SerializeField] private float _returnSeconds = 1.8f;

        [Header("[가정] 조형")]
        [SerializeField] private float _armLength = 6f;        // 피벗->손끝 거리 (관절 미연결 시 폴백)
        [SerializeField] private float _raiseForwardTilt = 0.3f;  // 치켜들기: 몸통 위 방향에 섞을 앞쏠림
        [SerializeField] private float _raiseSideSpread = 0.35f;  // 치켜들기: 팔 원위치 쪽으로 벌려 겹침 방지
        [SerializeField] private Vector3 _elbowRaiseEuler = new Vector3(-95f, 0f, 0f); // 깊은 접힘 — 망치 장전(손이 어깨 위로 걸린다)
        [SerializeField] private Vector3 _wristRaiseEuler = new Vector3(55f, 0f, 0f);  // 손목 젖힘 — 손바닥이 바닥을 향한다
        [SerializeField] private Vector3 _elbowSlamEuler = new Vector3(16f, 0f, 0f);   // 펴짐(살짝 과신전)
        [SerializeField] private Vector3 _wristSlamEuler = new Vector3(-55f, 0f, 0f);  // 따귀 스냅 — 손바닥이 지면으로 후려친다
        [SerializeField] private float _quiverDegrees = 1.8f;    // 홀드 중 떨림 폭
        [SerializeField] private float _overshootDegrees = 7f;   // 목표 너머로 꽂히는 과회전
        [Tooltip("스톱모션 경련 낙하 — 낙하 보간을 이 단수로 뚝뚝 끊는다. 0=부드럽게")]
        [SerializeField] private int _slamSteps = 5;
        [SerializeField] private Color _telegraphColor = new Color(0.9f, 0.2f, 0.1f);

        [Header("[가정] 착지 연출")]
        [SerializeField] private float _shakeStrength = 0.8f;
        [SerializeField] private float _shakeSeconds = 0.6f;
        [SerializeField] private float _shakeFalloffRange = 30f; // 이 거리 밖이면 흔들림 없음

        [Header("[가정] 증기 분출 — 관절 배기구(조립 시 배선)")]
        [SerializeField] private ParticleSystem[] _steamVents;
        [SerializeField] private int _steamBurstRaise = 10;    // 치켜들기 시작 — 기동 배기
        [SerializeField] private int _steamBurstPrime = 16;    // 홀드 진입 — 피스톤 예열 치익
        [SerializeField] private int _steamBurstImpact = 34;   // 착지 — 전 관절 대분출

        [Header("[가정] 유휴 물결 — 팔마다 위상을 다르게 심는다")]
        [SerializeField] private float _idlePhase;
        [SerializeField] private float _idleSpeed = 0.5f;
        [SerializeField] private float _idleShoulderDegrees = 2.2f;
        [SerializeField] private float _idleElbowDegrees = 3.2f;
        [SerializeField] private float _idleWristDegrees = 7f;
        [SerializeField] private float _idleStepDegrees = 0.9f; // 서보 래칫 눈금
        [SerializeField] private float _idleWristRollDegrees = 5f; // 손 비틀림 — 기괴함 담당
        [SerializeField] private float _idleTwitchDegrees = 6f;    // 이따금 터지는 경련

        public bool IsBusy => _phase != Phase.Rest;

        /// <summary>완전 신장 시 손끝까지의 월드 도달 거리 — 배차 게이트용.</summary>
        public float MaxReachWorld => (_armLength * _extendFactor + _handLength) * transform.lossyScale.z;

        /// <summary>휴식 자세의 월드 방향(부챗살 바깥쪽) — 목표와 방향이 맞는 팔만 배차한다.</summary>
        public Vector3 RestOutwardDirection
        {
            get
            {
                var parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
                return parentRot * _restLocalRotation * Vector3.forward;
            }
        }

        // 보스 몸통 기울임 가중치(0~1) — BossAI가 집계해 타격 방향으로 상체를 숙인다
        public float LeanWeight => _phase switch
        {
            Phase.Hold => Mathf.Clamp01(_timer / _holdSeconds) * 0.5f,
            Phase.Slam => 1f,
            Phase.Recoil => 1f,
            Phase.Plant => 0.7f,
            Phase.Return => 1f - Mathf.Clamp01(_timer / _returnSeconds),
            _ => 0f,
        };

        private Phase _phase = Phase.Rest;
        private float _timer;
        private bool _raiseVented;
        private float _paceScale = 1f; // 1=평시, 0.6=과부하(빠른 예비 동작)
        private Quaternion _restLocalRotation;
        private Quaternion _elbowRest;
        private Quaternion _wristRest;
        private float _restSideX; // 휴식 방향의 좌우 성분 — 치켜들 때 그쪽으로 살짝 벌린다
        private Quaternion _raiseStartWorldRotation;
        private float _elbowRestZ;
        private float _wristRestZ;
        private Quaternion _slamWorldRotation;
        private Quaternion _overshootWorldRotation;
        private Quaternion _slamStartRotation;
        private Quaternion _elbowSlamStart;
        private Quaternion _wristSlamStart;
        private int _lastSlamStep = -1;
        private Vector3 _forearmSleeveBaseScale = Vector3.one;
        private Vector3 _targetPoint;
        private float _damage;
        private float _impactRadius;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            // 조립 시 팔마다 조금씩 다른 휴식 자세(관절 굽힘·크기)를 심어두므로,
            // 여기서 잡은 현재 자세가 곧 그 팔의 고유 휴식 자세다.
            _restLocalRotation = transform.localRotation;
            if (_elbow != null) _elbowRest = _elbow.localRotation;
            if (_wrist != null) _wristRest = _wrist.localRotation;
            _restSideX = (_restLocalRotation * Vector3.forward).x;
            if (_elbow != null) _elbowRestZ = _elbow.localPosition.z;
            if (_wrist != null) _wristRestZ = _wrist.localPosition.z;
            if (_forearmSleeve != null) _forearmSleeveBaseScale = _forearmSleeve.localScale;
            // 파티클(증기)은 틴트 제외 — 텔레그래프 적색이 증기를 물들이지 않게 항상 흰 증기 유지
            var all = GetComponentsInChildren<Renderer>();
            var meshOnly = new System.Collections.Generic.List<Renderer>();
            foreach (var r in all)
                if (!(r is ParticleSystemRenderer)) meshOnly.Add(r);
            _renderers = meshOnly.ToArray();
            _mpb = new MaterialPropertyBlock();
        }

        // delaySeconds: 시작 지연(볼리 스태거용). frantic: 과부하 모드 — 예비 동작이 빨라지고 떨림이 커진다.
        public void SlamAt(Vector3 targetPoint, float damage, float impactRadius,
            float delaySeconds = 0f, bool frantic = false)
        {
            if (IsBusy) return;
            _targetPoint = targetPoint;
            _damage = damage;
            _impactRadius = impactRadius;
            _paceScale = frantic ? 0.6f : 1f;
            _raiseVented = false;
            _raiseStartWorldRotation = transform.rotation;
            _timer = -Mathf.Max(0f, delaySeconds); // 음수 타이머 = 대기. IsBusy는 즉시 참이라 중복 배차가 안 된다.
            _phase = Phase.Raise;
        }

        private void Update()
        {
            if (_phase == Phase.Rest) { IdleSway(); return; }
            _timer += Time.deltaTime; // 시간 감속 영향 — 작도 중엔 팔도 느려진다

            switch (_phase)
            {
                case Phase.Raise:
                {
                    if (!_raiseVented && _timer > 0f) { Vent(_steamBurstRaise); _raiseVented = true; }
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_timer / (_raiseSeconds * _paceScale)));
                    // 어느 팔이든 몸통 위쪽으로 치켜든다 — 목표는 매 프레임 몸통 기준으로 재계산
                    transform.rotation = Quaternion.Slerp(_raiseStartWorldRotation, RaisedWorldRotation(), t);
                    if (_elbow != null)
                        _elbow.localRotation = Quaternion.Slerp(_elbowRest,
                            _elbowRest * Quaternion.Euler(_elbowRaiseEuler), t);
                    if (_wrist != null)
                        _wrist.localRotation = Quaternion.Slerp(_wristRest,
                            _wristRest * Quaternion.Euler(_wristRaiseEuler), t);
                    SetTint(Color.Lerp(Color.white, _telegraphColor, t));
                    if (_timer >= _raiseSeconds * _paceScale) { _timer = 0f; _phase = Phase.Hold; Vent(_steamBurstPrime); }
                    break;
                }
                case Phase.Hold:
                {
                    // 정점에서 부들부들 — 떨림이 점점 커지고 색이 맥동하며 "온다"를 알린다.
                    // 과부하(frantic)면 홀드가 짧아지는 대신 떨림이 커진다 — 고장난 기계의 다급함.
                    float holdDur = _holdSeconds * _paceScale;
                    float grow = Mathf.Clamp01(_timer / holdDur);
                    float quiver = Mathf.Sin(Time.time * 52f) * (_quiverDegrees / _paceScale) * grow;
                    transform.rotation = RaisedWorldRotation() * Quaternion.Euler(quiver, quiver * 0.6f, 0f);
                    SetExtension(Mathf.Lerp(1f, _primeFactor, grow)); // 피스톤 예열 — 치익
                    SetTint(Color.Lerp(_telegraphColor, new Color(1f, 0.55f, 0.15f),
                        Mathf.PingPong(_timer * 7f, 1f)));
                    if (_timer >= holdDur)
                    {
                        // 조준 확정 — 이후에는 유도하지 않는다(회피 가능).
                        // 업힌트 없이 LookRotation을 쓰면 롤이 재정렬되어 손바닥이 하늘을 향하므로
                        // 보스 정면을 업힌트로 줘서 손바닥 방향을 보존한다.
                        Vector3 rollHint = transform.parent != null ? transform.parent.forward : Vector3.forward;
                        _slamWorldRotation = Quaternion.LookRotation(
                            (_targetPoint - transform.position).normalized, rollHint);
                        // 목표보다 더 깊이 꽂히는 과회전 — 땅을 뚫을 기세
                        _overshootWorldRotation = _slamWorldRotation * Quaternion.Euler(_overshootDegrees, 0f, 0f);
                        // 스톱모션 낙하 기준 자세 고정(자기참조 보간 금지 — 단이 균일해야 경련답다)
                        _slamStartRotation = transform.rotation;
                        _elbowSlamStart = _elbow != null ? _elbow.localRotation : Quaternion.identity;
                        _wristSlamStart = _wrist != null ? _wrist.localRotation : Quaternion.identity;
                        _lastSlamStep = -1;
                        _timer = 0f;
                        _phase = Phase.Slam;
                    }
                    break;
                }
                case Phase.Slam:
                {
                    float t = Mathf.Clamp01(_timer / _slamSeconds);
                    float a = t * t * t; // 급가속 낙하
                    // 스톱모션 경련 — 보간을 단수로 양자화해 필름 끊긴 괴물처럼 뚝뚝 떨어진다
                    if (_slamSteps > 0)
                    {
                        float q = Mathf.Floor(a * _slamSteps) / _slamSteps;
                        int step = (int)(a * _slamSteps);
                        if (step != _lastSlamStep) { _lastSlamStep = step; Vent(3); } // 경련마다 칙
                        a = t >= 1f ? 1f : q;
                    }
                    transform.rotation = Quaternion.Slerp(_slamStartRotation, _overshootWorldRotation, a);
                    SetExtension(Mathf.Lerp(_primeFactor, _extendFactor, a)); // 피스톤 사출 — 쾅
                    if (_elbow != null)
                        _elbow.localRotation = Quaternion.Slerp(_elbowSlamStart,
                            _elbowRest * Quaternion.Euler(_elbowSlamEuler), a);
                    if (_wrist != null)
                        _wrist.localRotation = Quaternion.Slerp(_wristSlamStart,
                            _wristRest * Quaternion.Euler(_wristSlamEuler), a);
                    if (t >= 1f) Impact(); // -> Recoil
                    break;
                }
                case Phase.Recoil:
                {
                    // 과회전에서 목표 자세로 살짝 튕겨 돌아온다 — 반동
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_timer / _recoilSeconds));
                    transform.rotation = Quaternion.Slerp(transform.rotation, _slamWorldRotation, t);
                    if (_timer >= _recoilSeconds) { _timer = 0f; _phase = Phase.Plant; }
                    break;
                }
                case Phase.Plant:
                {
                    // 땅에 꽂힌 채 한 박자 — 무게감. 회피/반격 창이기도 하다.
                    if (_timer >= _plantSeconds) { _timer = 0f; _phase = Phase.Return; }
                    break;
                }
                case Phase.Return:
                {
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_timer / _returnSeconds));
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, _restLocalRotation, t);
                    if (_elbow != null)
                        _elbow.localRotation = Quaternion.Slerp(_elbow.localRotation, _elbowRest, t);
                    if (_wrist != null)
                        _wrist.localRotation = Quaternion.Slerp(_wrist.localRotation, _wristRest, t);
                    SetExtension(Mathf.Lerp(_extendFactor, 1f, t)); // 피스톤 수납
                    if (_timer >= _returnSeconds) { SetExtension(1f); _phase = Phase.Rest; }
                    break;
                }
            }
        }

        // 유휴 모션 3겹 — 느린 유기적 흔들림(자연) + 손목의 깊은 굽이·비틀림(기괴)
        // + 팔마다 다른 순간에 터지는 짧은 경련(기괴). 팔꿈치만 래칫 양자화로 기계 질감을 남긴다.
        private void IdleSway()
        {
            float t = Time.time * _idleSpeed + _idlePhase;
            // 좁은 펄스: sin을 고차 거듭제곱하면 주기 대부분 0이다가 이따금 훅 치솟는다
            float twitch = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 0.37f + _idlePhase * 2.3f)), 24f);
            transform.localRotation = _restLocalRotation * Quaternion.Euler(
                Mathf.Sin(t) * _idleShoulderDegrees,
                Mathf.Sin(t * 0.71f + 1.3f) * _idleShoulderDegrees * 0.6f, 0f);
            if (_elbow != null)
                _elbow.localRotation = _elbowRest * Quaternion.Euler(
                    Quantize(Mathf.Sin(t * 1.13f + 0.5f) * _idleElbowDegrees) + twitch * _idleTwitchDegrees, 0f, 0f);
            if (_wrist != null)
                _wrist.localRotation = _wristRest * Quaternion.Euler(
                    Mathf.Sin(t * 0.43f + 1.1f) * _idleWristDegrees + twitch * _idleTwitchDegrees * 1.5f,
                    0f,
                    Mathf.Sin(t * 0.31f + 2.2f) * _idleWristRollDegrees);
        }

        private float Quantize(float degrees) =>
            _idleStepDegrees <= 0f ? degrees : Mathf.Round(degrees / _idleStepDegrees) * _idleStepDegrees;

        // 치켜들기 목표 자세(월드) — 몸통 위 + 살짝 앞, 팔 원위치 쪽으로 벌려 서로 겹치지 않게.
        // 업힌트 = 몸통 정면이라 손바닥(+Y)이 전방을 유지하고, 손목 젖힘(+55도)이 손바닥을 바닥으로 향하게 한다.
        private Quaternion RaisedWorldRotation()
        {
            Transform body = transform.parent != null ? transform.parent : transform;
            Vector3 upDir = (body.up
                + body.forward * _raiseForwardTilt
                + body.right * (_restSideX * _raiseSideSpread)).normalized;
            return Quaternion.LookRotation(upDir, body.forward);
        }

        // 텔레스코픽 신장 — 관절을 축 방향으로 밀어내고, 벌어진 틈은 황동 피스톤 로드가 채운다.
        // 로드는 피벗/팔꿈치의 자식이라 관절이 나가면 실린더에서 뽑혀 나오는 것처럼 드러난다.
        private void SetExtension(float factor)
        {
            if (_elbow != null)
            {
                var p = _elbow.localPosition;
                _elbow.localPosition = new Vector3(p.x, p.y, _elbowRestZ * factor);
            }
            if (_wrist != null)
            {
                var p = _wrist.localPosition;
                _wrist.localPosition = new Vector3(p.x, p.y, _wristRestZ * factor);
            }
            StretchRod(_elbowRod, _elbow != null ? _elbow.localPosition.z : 0f);
            StretchRod(_wristRod, _wrist != null ? _wrist.localPosition.z : 0f);
            // 전완 슬리브가 손목을 정확히 추적 — 손목 분리 방지(피스톤은 측면에서 계속 노출)
            if (_forearmSleeve != null)
            {
                var s = _forearmSleeveBaseScale;
                _forearmSleeve.localScale = new Vector3(s.x, s.y, s.z * factor);
            }
        }

        private static void StretchRod(Transform rod, float spanZ)
        {
            if (rod == null || spanZ <= 0f) return;
            var s = rod.localScale;
            var p = rod.localPosition;
            rod.localScale = new Vector3(s.x, spanZ * 0.5f, s.z); // 실린더 기본 높이 2 기준
            rod.localPosition = new Vector3(p.x, p.y, spanZ * 0.5f); // x/y = 팔대 중심축 오프셋 보존
        }

        private Vector3 TipPosition()
        {
            if (_wrist != null) return _wrist.position + _wrist.forward * _handLength;
            return transform.position + transform.forward * _armLength;
        }

        private void Impact()
        {
            SetTint(Color.white);
            Vector3 tip = TipPosition();
            tip.y = 0.1f;
            // 이중 충격파 — 안쪽 진한 타격 + 바깥 옅은 파동
            SpellVisuals.SpawnBurst(tip, new Color(0.65f, 0.3f, 0.1f, 0.55f), _impactRadius * 2f, 0.35f);
            SpellVisuals.SpawnBurst(tip, new Color(0.75f, 0.55f, 0.3f, 0.22f), _impactRadius * 4.2f, 0.5f);

            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null)
            {
                float dist = Vector3.Distance(cc.transform.position, tip);
                // 거리 감쇠 화면 흔들림 — 가까울수록 쾅
                float falloff = Mathf.Clamp01(1f - dist / _shakeFalloffRange);
                if (falloff > 0f) CameraShake.Shake(_shakeStrength * falloff, _shakeSeconds);
                if (dist <= _impactRadius)
                {
                    var hp = cc.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.TakeDamage(_damage); Debug.Log("[BossArm] 내려찍기 적중"); }
                }
                else Debug.Log("[BossArm] 내려찍기 빗나감");
            }

            Vent(_steamBurstImpact); // 착지 — 전 관절 증기 대분출
            _timer = 0f;
            _phase = Phase.Recoil;
        }

        private void Vent(int count)
        {
            if (_steamVents == null) return;
            foreach (var v in _steamVents)
                if (v != null) v.Emit(count);
        }

        private void SetTint(Color c)
        {
            _mpb.SetColor(BaseColorId, c);
            foreach (var r in _renderers) r.SetPropertyBlock(_mpb);
        }
    }
}
