using System.Text;
using MandateOfInk.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace MandateOfInk.Presentation
{
    // 서책 UI 왼쪽의 「낱장 3D 뷰어」 — 씬 밖 숨은 무대(전용 레이어)에 한지 낱장을 세우고
    // 전용 카메라로 RenderTexture에 찍어 UI(RawImage)에 띄운다. 드래그=회전, 휠=확대.
    // 낱장은 코드로 조립: 한지 판 + 광곽(붉은 인찰 테두리) + 세로쓰기 한문 원문 + 장서인.
    // 세로쓰기·우횡좌서(오른쪽에서 왼쪽 열)는 조선 초기 판본 관행을 따른다.
    public sealed class CodexPageViewer : MonoBehaviour
    {
        public const string StageLayerName = "CodexStage";

        // 전부 [가정] — 낱장 크기·글씨 크기·카메라 거리 등 손맛 수치
        private const float LeafWidth = 0.32f;
        private const float LeafHeight = 0.46f;
        private const float DefaultDistance = 0.75f;
        private const int CharsPerColumn = 12;
        private const float ColumnStep = 0.046f;

        private static readonly Color PaperColor = new Color(0.92f, 0.88f, 0.78f, 1f);
        private static readonly Color FrameColor = new Color(0.5f, 0.2f, 0.16f, 1f);
        private static readonly Color InkColor = new Color(0.14f, 0.12f, 0.1f, 1f);

        private GameObject _stageRoot;
        private Transform _leafRoot;
        private Transform _textRoot;
        private Camera _cam;
        private RenderTexture _texture;
        private Material _paperMat;
        private Material _frameMat;
        private int _layer;
        private float _yaw;
        private float _pitch;
        private float _distance = DefaultDistance;

        public RenderTexture Texture
        {
            get
            {
                EnsureStage();
                return _texture;
            }
        }

        public void SetActiveStage(bool active)
        {
            EnsureStage();
            _stageRoot.SetActive(active);
        }

        public void Orbit(Vector2 delta)
        {
            if (_leafRoot == null) return;
            _yaw -= delta.x * 0.4f;
            _pitch = Mathf.Clamp(_pitch + delta.y * 0.4f, -75f, 75f);
            _leafRoot.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        public void Zoom(float scrollY)
        {
            if (_cam == null) return;
            _distance = Mathf.Clamp(_distance - scrollY * 0.06f, 0.35f, 1.3f);
            _cam.transform.localPosition = new Vector3(0f, 0f, -_distance);
        }

        // 선택한 낱장의 원문을 세로쓰기 열로 다시 조판. null이면 백지 낱장.
        public void ShowPage(HaeryePageSO page)
        {
            EnsureStage();
            for (int i = _textRoot.childCount - 1; i >= 0; i--)
            {
                var child = _textRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            _yaw = 0f;
            _pitch = 0f;
            _distance = DefaultDistance;
            _leafRoot.localRotation = Quaternion.identity;
            _cam.transform.localPosition = new Vector3(0f, 0f, -_distance);

            if (page == null || string.IsNullOrEmpty(page.HanjaQuote)) return;

            string chars = page.HanjaQuote.Replace(" ", "");
            int perColumn = CharsPerColumn;
            int columns = Mathf.CeilToInt(chars.Length / (float)perColumn);
            if (columns > 6)
            {
                columns = 6;
                perColumn = Mathf.CeilToInt(chars.Length / 6f);
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 동적 폰트 — 한자 OS 폴백
            float startX = (columns - 1) * ColumnStep * 0.5f; // 첫 열이 오른쪽(우횡좌서)
            for (int c = 0; c < columns; c++)
            {
                var sb = new StringBuilder();
                int end = Mathf.Min((c + 1) * perColumn, chars.Length);
                for (int i = c * perColumn; i < end; i++) sb.Append(chars[i]).Append('\n');

                var go = new GameObject($"Col_{c}");
                go.layer = _layer;
                go.transform.SetParent(_textRoot, false);
                go.transform.localPosition = new Vector3(startX - c * ColumnStep, LeafHeight * 0.4f, 0f);
                var tm = go.AddComponent<TextMesh>();
                tm.text = sb.ToString().TrimEnd('\n');
                tm.font = font;
                tm.fontSize = 64;
                tm.characterSize = 0.0038f; // 12자 열이 광곽 안에 들어가는 크기
                tm.anchor = TextAnchor.UpperCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = InkColor;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = font.material; // TextMesh는 폰트 머티리얼을 손으로 물려야 글자가 보인다
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public void EnsureStage()
        {
            if (_stageRoot != null) return;

            _layer = LayerMask.NameToLayer(StageLayerName);
            if (_layer < 0)
            {
                _layer = 0;
                Debug.LogWarning("[CodexPageViewer] CodexStage 레이어 없음 — Default 레이어 사용(월드 카메라에 비칠 수 있음)");
            }

            _stageRoot = new GameObject("CodexPageStage");
            _stageRoot.transform.position = new Vector3(0f, -400f, 0f); // 씬 밖 지하 무대
            _leafRoot = new GameObject("Leaf").transform;
            _leafRoot.SetParent(_stageRoot.transform, false);
            BuildLeaf();

            var camGo = new GameObject("StageCam");
            camGo.transform.SetParent(_stageRoot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -_distance);
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.9f, 0.86f, 0.76f, 1f);
            _cam.fieldOfView = 40f;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 5f;
            _cam.cullingMask = 1 << _layer;
            _texture = new RenderTexture(1024, 1024, 16);
            _cam.targetTexture = _texture;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;
            camData.SetRenderer(1); // 먹 포스트 없는 뷰모델 렌더러 — 이중 적용 교훈의 재발 방지
        }

        private void BuildLeaf()
        {
            var toon = Shader.Find("MandateOfInk/Temp/ToonLit");
            var paperMat = _paperMat = new Material(toon);
            paperMat.SetColor("_BaseColor", PaperColor);
            paperMat.SetFloat("_Saturation", 1f);
            var frameMat = _frameMat = new Material(toon);
            frameMat.SetColor("_BaseColor", FrameColor);
            frameMat.SetFloat("_Saturation", 1f);

            MakeQuad("PaperFront", _leafRoot, new Vector3(0f, 0f, 0f),
                new Vector3(LeafWidth, LeafHeight, 1f), Quaternion.identity, paperMat);
            MakeQuad("PaperBack", _leafRoot, new Vector3(0f, 0f, 0.001f),
                new Vector3(LeafWidth, LeafHeight, 1f), Quaternion.Euler(0f, 180f, 0f), paperMat);

            // 광곽(외곽 테두리) — 붉은 인찰선
            float fx = LeafWidth * 0.46f;
            float fy = LeafHeight * 0.46f;
            MakeQuad("FrameTop", _leafRoot, new Vector3(0f, fy, -0.0005f),
                new Vector3(fx * 2f, 0.005f, 1f), Quaternion.identity, frameMat);
            MakeQuad("FrameBottom", _leafRoot, new Vector3(0f, -fy, -0.0005f),
                new Vector3(fx * 2f, 0.005f, 1f), Quaternion.identity, frameMat);
            MakeQuad("FrameLeft", _leafRoot, new Vector3(-fx, 0f, -0.0005f),
                new Vector3(0.005f, fy * 2f, 1f), Quaternion.identity, frameMat);
            MakeQuad("FrameRight", _leafRoot, new Vector3(fx, 0f, -0.0005f),
                new Vector3(0.005f, fy * 2f, 1f), Quaternion.identity, frameMat);

            // 장서인 — 왼쪽 아래 붉은 인장
            MakeQuad("Seal", _leafRoot, new Vector3(-fx + 0.035f, -fy + 0.04f, -0.001f),
                new Vector3(0.035f, 0.035f, 1f), Quaternion.Euler(0f, 0f, 4f), frameMat);

            _textRoot = new GameObject("Text").transform;
            _textRoot.SetParent(_leafRoot, false);
            _textRoot.localPosition = new Vector3(0f, 0f, -0.002f);
        }

        private void MakeQuad(string name, Transform parent, Vector3 localPos, Vector3 localScale,
            Quaternion localRot, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.layer = _layer;
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = localRot;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            if (_texture != null) _texture.Release();
            DestroyResource(_texture);
            DestroyResource(_paperMat);
            DestroyResource(_frameMat);
            if (_stageRoot != null)
            {
                if (Application.isPlaying) Destroy(_stageRoot);
                else DestroyImmediate(_stageRoot);
            }
        }

        private static void DestroyResource(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }
    }

    // 뷰어 RawImage 위에 붙어 포인터 드래그·휠을 뷰어로 중계한다.
    internal sealed class CodexPageDragRelay : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public CodexPageViewer Viewer;

        public void OnDrag(PointerEventData eventData)
        {
            if (Viewer != null) Viewer.Orbit(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (Viewer != null) Viewer.Zoom(eventData.scrollDelta.y);
        }
    }
}
