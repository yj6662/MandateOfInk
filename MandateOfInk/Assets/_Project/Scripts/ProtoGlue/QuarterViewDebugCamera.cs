using UnityEngine;

// 임시 디버그 도구: F3로 1인칭 <-> 쿼터뷰 전환. 방어 돔·광역 파동처럼
// 1인칭에서 안 보이는 진 효과를 확인하는 용도다. (프로토 전용 — 출시 빌드 제외 대상)
// 쿼터뷰 카메라도 MainCamera 태그를 달아, 활성 카메라가 항상 Camera.main이 되게 한다
// (빌보드 글자·락온 레티클이 자동으로 따라온다).
public sealed class QuarterViewDebugCamera : MonoBehaviour
{
    [Header("[가정] 설정")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.F3;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 9f, -7f);
    [SerializeField] private float _followLerpSpeed = 10f;

    private Camera _mainCamera;
    private Camera _quarterCamera;
    private Transform _target;
    private bool _quarterActive;

    private void Start()
    {
        _mainCamera = Camera.main;
        var cc = FindFirstObjectByType<CharacterController>();
        if (cc != null) _target = cc.transform;

        var go = new GameObject("QuarterViewCamera") { tag = "MainCamera" };
        _quarterCamera = go.AddComponent<Camera>();
        _quarterCamera.enabled = false;
        if (_target != null)
        {
            go.transform.position = _target.position + _offset;
            go.transform.LookAt(_target.position + Vector3.up * 1f);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_toggleKey) || _mainCamera == null || _target == null) return;
        _quarterActive = !_quarterActive;
        _mainCamera.enabled = !_quarterActive;
        _quarterCamera.enabled = _quarterActive;
        Debug.Log(_quarterActive ? "[Debug] 쿼터뷰 켜짐 (작도 먹선은 1인칭 전용이라 안 보임)" : "[Debug] 1인칭 복귀");
    }

    private void LateUpdate()
    {
        if (!_quarterActive || _target == null) return;
        Vector3 desired = _target.position + _offset;
        float k = 1f - Mathf.Exp(-_followLerpSpeed * Time.unscaledDeltaTime);
        _quarterCamera.transform.position = Vector3.Lerp(_quarterCamera.transform.position, desired, k);
        _quarterCamera.transform.LookAt(_target.position + Vector3.up * 1f);
    }
}
