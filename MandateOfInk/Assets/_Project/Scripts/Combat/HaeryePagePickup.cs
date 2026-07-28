using MandateOfInk.Data;
using UnityEngine;

namespace MandateOfInk.Combat
{
    // 해례본 낱장 습득물 — 접근 + E(기존 화물 수령 패턴). 습득 시 CodexInventory에 추가되고 사라진다.
    // 비주얼: 3D 고서 모델(_modelPrefab)이 떠서 천천히 돌고, 그 위에 제목·한문 원문이 입체 텍스트로
    // 떠 있다(빌보드 — 카메라를 향함, 거리에 따라 흐려짐). 모델 미배선 시 스프라이트 폴백.
    public sealed class HaeryePagePickup : MonoBehaviour
    {
        [SerializeField] private HaeryePageSO _page;
        [SerializeField] private GameObject _modelPrefab;
        [SerializeField] private Sprite _sprite; // 모델 미배선 시 폴백
        [Header("[가정]")]
        [SerializeField] private float _interactRange = 3f;
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private float _bobHeight = 0.12f;
        [SerializeField] private float _spinDegreesPerSecond = 40f;
        [SerializeField] private float _modelSize = 0.45f;
        [Tooltip("원문 텍스트가 보이기 시작하는 거리(m) — 이 안에서 가까울수록 또렷")]
        [SerializeField] private float _textVisibleRange = 9f;

        private Transform _player;
        private CodexInventory _inventory;
        private bool _playerNear;
        private Transform _visual;
        private TextMesh _label;
        private string _prompt;
        private float _baseY = 0.7f;

        private void Start()
        {
            useGUILayout = false; // OnGUI 레이아웃 패스 생략 — 프롬프트만 그린다
            if (_page != null)
                _prompt = string.IsNullOrEmpty(_page.SourceBook)
                    ? $"E: 습득 — 「{_page.Title}」"
                    : $"E: 습득 — {_page.SourceBook} 「{_page.Title}」";
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null)
            {
                _player = cc.transform;
                _inventory = cc.GetComponent<CodexInventory>();
            }
            BuildVisual();
            BuildLabel();
        }

        private void BuildVisual()
        {
            if (_modelPrefab != null)
            {
                var model = Instantiate(_modelPrefab, transform);
                var rends = model.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    foreach (var r in rends) b.Encapsulate(r.bounds);
                    float unit = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (unit > 0.001f) model.transform.localScale *= _modelSize / unit;
                    // 스케일 반영 후 바운드 중심을 부유 기준점에 정렬
                    Bounds b2 = rends[0].bounds;
                    foreach (var r in rends) b2.Encapsulate(r.bounds);
                    model.transform.position += transform.position + Vector3.up * _baseY - b2.center;
                }
                _visual = model.transform;
                return;
            }

            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            if (_sprite != null && _sprite.bounds.size.y > 0.01f)
                go.transform.localScale = Vector3.one * (0.55f / _sprite.bounds.size.y);
            go.transform.localPosition = Vector3.up * _baseY;
            _visual = go.transform;
        }

        // 원문 텍스트 — 책 위에 뜨는 입체 글씨(제목 + 한문 발췌)
        private void BuildLabel()
        {
            if (_page == null) return;
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * (_baseY + 0.55f);
            _label = go.AddComponent<TextMesh>();
            _label.text = $"{_page.Title}\n{_page.HanjaQuote}";
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 동적 폰트 — 한자 OS 폴백
            _label.fontSize = 48;
            _label.characterSize = 0.018f;
            _label.anchor = TextAnchor.LowerCenter;
            _label.alignment = TextAlignment.Center;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _label.font.material; // TextMesh는 폰트 머티리얼을 손으로 물려야 글자가 보인다
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Update()
        {
            if (_visual != null)
            {
                Vector3 pos = _visual.localPosition;
                pos.y = _baseY + Mathf.Sin(Time.time * 1.7f) * _bobHeight
                    + (pos.y - _baseY) * 0f; // 모델 정렬 오프셋은 스폰 시 반영됨 — 여기선 순수 부유만
                _visual.localPosition = new Vector3(_visual.localPosition.x,
                    pos.y, _visual.localPosition.z);
                _visual.Rotate(0f, _spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            }

            if (_player == null) return;
            float dist = Vector3.Distance(_player.position, transform.position);

            // 원문 라벨 — 카메라를 향하고, 멀수록 흐려진다
            if (_label != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 look = _label.transform.position - cam.transform.position;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.001f)
                        _label.transform.rotation = Quaternion.LookRotation(look);
                }
                float alpha = Mathf.Clamp01(1f - dist / _textVisibleRange);
                _label.color = new Color(0.15f, 0.13f, 0.11f, alpha * 0.95f);
            }

            if (_inventory == null || _page == null) return;
            _playerNear = dist <= _interactRange;
            if (_playerNear && Input.GetKeyDown(_interactKey))
            {
                if (_inventory.Collect(_page))
                {
                    SpellVisuals.SpawnBurst(transform.position + Vector3.up * 0.8f,
                        new Color(0.16f, 0.15f, 0.13f, 0.9f), 1.1f, 0.5f);
                    Destroy(gameObject);
                }
            }
        }

        private void OnGUI()
        {
            if (!_playerNear || _prompt == null) return;
            GUI.Label(new Rect(10, 108, 520, 24), _prompt);
        }
    }
}
