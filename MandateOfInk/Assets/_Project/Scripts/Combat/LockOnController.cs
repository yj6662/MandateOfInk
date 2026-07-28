using UnityEngine;

namespace MandateOfInk.Combat
{
    // 간단 락온: 토글 키로 범위 내 최근접 적을 잠그고, 플레이어 요·카메라 피치를 대상에 맞춘다.
    // StarterAssets를 직접 참조하지 않고 Transform만 조작한다(프로토 타협 — 해제 시 시점 스냅 가능).
    // 실행 순서를 늦춰 FirstPersonController의 LateUpdate 회전 뒤에 덮어쓴다.
    [DefaultExecutionOrder(100)]
    public sealed class LockOnController : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Transform _playerRoot;   // PlayerCapsule — 요(수평) 회전
        [SerializeField] private Transform _cameraTarget; // PlayerCameraRoot — 피치(조준) 회전

        [Header("[가정] 파라미터")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.Tab;
        [SerializeField] private float _range = 20f;
        [SerializeField] private float _turnSpeed = 12f;
        [SerializeField] private float _aimHeightOffset = 0.5f; // 적 몸통 중심 보정

        public EnemyHealth Target { get; private set; }

        private GUIStyle _reticleStyle;

        private void Awake()
        {
            useGUILayout = false; // OnGUI 레이아웃 패스 생략 — 레티클만 그린다
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                if (Target != null) { Target = null; Debug.Log("[LockOn] 해제"); }
                else Acquire();
            }
            // 대상 사망(파괴)·이탈 시 자동 해제
            if (Target == null) return;
            if (Vector3.Distance(_playerRoot.position, Target.transform.position) > _range * 1.2f)
            {
                Target = null;
                Debug.Log("[LockOn] 범위 이탈 — 해제");
            }
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            // 프레임률 무관 지수 감쇠 계수 — Slerp 인자에 큰 dt곱을 쓰면 프레임마다 과회전해 떨림이 생긴다
            float k = 1f - Mathf.Exp(-_turnSpeed * Time.unscaledDeltaTime);

            // 수평(요): 플레이어 몸만 회전 (카메라 루트는 자식이라 함께 돈다)
            Vector3 flat = Target.transform.position - _playerRoot.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                _playerRoot.rotation = Quaternion.Slerp(_playerRoot.rotation, Quaternion.LookRotation(flat), k);

            // 수직(피치): 카메라 루트의 로컬 X 회전만 사용.
            // 월드 회전을 덮어쓰면 부모(몸) 회전과 매 프레임 피드백이 생겨 심하게 흔들린다.
            Vector3 aim = Target.transform.position + Vector3.up * _aimHeightOffset - _cameraTarget.position;
            float horizontal = new Vector2(aim.x, aim.z).magnitude;
            float pitchDeg = -Mathf.Atan2(aim.y, Mathf.Max(horizontal, 0.001f)) * Mathf.Rad2Deg;
            var desiredLocal = Quaternion.Euler(pitchDeg, 0f, 0f);
            _cameraTarget.localRotation = Quaternion.Slerp(_cameraTarget.localRotation, desiredLocal, k);
        }

        private void Acquire()
        {
            float best = float.MaxValue;
            EnemyHealth bestEnemy = null;
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(_playerRoot.position, enemy.transform.position);
                if (d <= _range && d < best) { best = d; bestEnemy = enemy; }
            }
            Target = bestEnemy;
            Debug.Log(Target != null ? $"[LockOn] {Target.name} ({best:F1}m)" : "[LockOn] 범위 내 대상 없음");
        }

        // 최소 HUD 허용 항목: 락온 레티클 (프로토 임시 표시).
        // 대상이 그로기면 레티클이 금빛 「틈」으로 바뀐다 — 폭딜 창을 조준선에서 바로 읽는다.
        private void OnGUI()
        {
            if (Target == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(Target.transform.position + Vector3.up * _aimHeightOffset);
            if (sp.z < 0f) return;

            if (_reticleStyle == null)
                _reticleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };

            var status = Target.GetComponent<EnemyStatus>();
            bool groggy = status != null && status.IsGroggy;
            bool armed = status != null && status.HasMark; // 격발 표식 부착 = 「장전됨」(전투코어루프 §6)
            var prevColor = GUI.color;
            GUI.color = groggy ? new Color(0.98f, 0.82f, 0.25f) : Color.white;
            GUI.Label(new Rect(sp.x - 24f, Screen.height - sp.y - 20f, 48f, 40f), groggy ? "틈" : "◎", _reticleStyle);
            if (armed && !groggy)
            {
                GUI.color = new Color(0.85f, 0.65f, 0.25f);
                GUI.Label(new Rect(sp.x - 24f, Screen.height - sp.y + 12f, 48f, 30f), "장전", _reticleStyle);
            }
            GUI.color = prevColor;
        }
    }
}
