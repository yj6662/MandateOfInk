using System.Collections.Generic;
using System.IO;
using MandateOfInk.Data;
using PDollarGestureRecognizer;
using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 작도 모드 입력: 글자(초성+중성)를 이어 그리면 「획 그룹 분할」로 인식해 발동한다.
    //   한글은 초성을 먼저 쓰므로, 획 순서 기준 모든 분할점(앞=초성, 뒤=중성)을 시도하고
    //   각 부분을 독립 정규화·인식해 합산 점수가 최고인 조합을 택한다.
    //   -> 자모의 위치·크기·침범과 무관해지고, 자모별 인식 정확도(S2 수준)를 유지한다.
    //   종성 확장 시 분할점 2개(3그룹)로 같은 방식을 쓴다.
    // 먹선 표현: 붓끝 Lerp 스무딩 + 속도 기반 굵기 + 번짐 애니 + 소프트 엣지 텍스처. 파라미터는 [가정].
    public sealed class DrawingInputController : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private SpellcraftModeController _modeController;
        [SerializeField] private DiagramEventChannelSO _diagramDrawn;
        [SerializeField] private SpellDiagramLibrarySO _library;
        [SerializeField] private Camera _viewCamera;

        [Header("[가정] 판정 파라미터")]
        [Tooltip("획 종료 후 이 시간(실시간 초) 동안 무입력이면 글자 확정")]
        [SerializeField] private float _commitIdleSeconds = 0.7f;
        [SerializeField] private string _initialTemplateDir = "_Project/Data/JamoTemplates/Initials";
        [SerializeField] private string _medialTemplateDir = "_Project/Data/JamoTemplates/Medials";

        [Header("[가정] 먹선 스타일")]
        [SerializeField] private float _brushLerpSpeed = 14f;
        [SerializeField] private float _baseWidth = 0.006f;
        [SerializeField] private float _bleedMultiplier = 1.6f;
        [SerializeField] private float _bleedSeconds = 0.9f;
        [SerializeField] private float _minWidthFactor = 0.55f;
        [SerializeField] private float _maxWidthFactor = 1.4f;
        [SerializeField] private Color _inkColor = new Color(0.13f, 0.12f, 0.11f, 0.95f);

        private sealed class Stroke
        {
            public LineRenderer Line;
            public readonly List<float> Widths = new List<float>();
            public float BornTime;
        }

        private static readonly string[] MedialKeys = { "ㅏ", "ㅓ", "ㅗ", "ㅜ" };
        private static readonly string[] MedialTags = { "a", "eo", "o", "u" };

        private readonly List<Point> _points = new List<Point>();
        private readonly List<Stroke> _strokes = new List<Stroke>();
        private Gesture[] _initialTemplates = new Gesture[0];
        private Gesture[] _medialTemplates = new Gesture[0];

        private int _strokeId = -1;
        private float _idleTimer;
        private Vector3 _brushScreenPos;
        private Vector3 _prevSample;
        private Material _lineMaterial;
        private bool _recordMode; // F2: 중성 실필기 등록 모드

        private void Start()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader) { mainTexture = CreateSoftEdgeTexture() };
            LoadAllTemplates();
        }

        private void LoadAllTemplates()
        {
            _initialTemplates = LoadTemplates(_initialTemplateDir, "초성");
            _medialTemplates = LoadTemplates(_medialTemplateDir, "중성");
        }

        private Gesture[] LoadTemplates(string relativeDir, string label)
        {
            var list = new List<Gesture>();
            string dir = Path.Combine(Application.dataPath, relativeDir);
            if (!Directory.Exists(dir)) { Debug.LogWarning($"[Drawing] {label} 템플릿 폴더 없음: {dir}"); return list.ToArray(); }
            foreach (string file in Directory.GetFiles(dir, "*.xml"))
                list.Add(GestureIO.ReadGestureFromFile(file));
            Debug.Log($"[Drawing] {label} 템플릿 {list.Count}개 로드");
            return list.ToArray();
        }

        private void Update()
        {
            if (_modeController == null || _modeController.Mode != SpellcraftMode.Drawing)
            {
                if (_points.Count > 0 || _strokes.Count > 0) ClearDrawing();
                _recordMode = false;
                return;
            }
            if (Input.GetKeyDown(KeyCode.F2)) { _recordMode = !_recordMode; ClearDrawing(); }

            HandleStroke();
            AnimateBleed();

            if (_recordMode) HandleRecordKeys();
            else HandleCommit();
        }

        // ---- 인식 ----

        private void HandleCommit()
        {
            if (Input.GetMouseButton(0) || _points.Count < 8) return;
            _idleTimer += Time.unscaledDeltaTime;
            if (_idleTimer < _commitIdleSeconds) return;
            RecognizeLetter();
        }

        private void RecognizeLetter()
        {
            if (_initialTemplates.Length == 0) { ClearDrawing(); _modeController.CompleteDrawing(); return; }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int strokeCount = _strokeId + 1;
            string initial = null, medial = null;
            float bestScore = float.MinValue;

            if (strokeCount < 2 || _medialTemplates.Length == 0)
            {
                // 획이 하나뿐이면 초성 단독으로 보고 ㅏ 기본형 폴백
                Result r = PointCloudRecognizer.Classify(new Gesture(_points.ToArray()), _initialTemplates);
                initial = r.GestureClass;
                medial = "ㅏ";
                bestScore = r.Score;
            }
            else
            {
                // 획 순서 기준 모든 분할점 시도: 앞 그룹 = 초성, 뒤 그룹 = 중성.
                // 부분별로 독립 정규화되므로 자모의 위치·크기·침범은 판정에 영향이 없다.
                for (int split = 1; split < strokeCount; split++)
                {
                    var iniPts = CollectPoints(0, split);
                    var medPts = CollectPoints(split, strokeCount);
                    if (iniPts.Length < 4 || medPts.Length < 4) continue;

                    Result ri = PointCloudRecognizer.Classify(new Gesture(iniPts), _initialTemplates);
                    Result rm = PointCloudRecognizer.Classify(new Gesture(medPts), _medialTemplates);
                    float score = ri.Score + rm.Score;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        initial = ri.GestureClass;
                        medial = rm.GestureClass;
                    }
                }
            }
            sw.Stop();

            // 폴백: 항상 최근접 자모 조합을 쓰므로 완전 불발은 없다
            var diagram = _library.FindByJamo(initial, medial, "") ?? _library.FindByJamo(initial, "ㅏ", "");
            Debug.Log($"[Drawing] 분할 인식 {initial}+{medial} (합산 {bestScore:F2}, {sw.Elapsed.TotalMilliseconds:F1}ms, {strokeCount}획) -> 「{(diagram != null ? diagram.Letter : "없음")}」");

            if (diagram != null) _diagramDrawn?.Raise(diagram);
            ClearDrawing();
            _modeController.CompleteDrawing();
        }

        private Point[] CollectPoints(int strokeFrom, int strokeTo)
        {
            var list = new List<Point>();
            foreach (var p in _points)
                if (p.StrokeID >= strokeFrom && p.StrokeID < strokeTo) list.Add(p);
            return list.ToArray();
        }

        // ---- 중성 실필기 등록 ----

        private void HandleRecordKeys()
        {
            if (_points.Count < 4) return;
            for (int i = 0; i < MedialKeys.Length; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
                string dir = Path.Combine(Application.dataPath, _medialTemplateDir);
                Directory.CreateDirectory(dir);
                int n = Directory.GetFiles(dir, MedialTags[i] + "_*.xml").Length + 1;
                string file = Path.Combine(dir, $"{MedialTags[i]}_{n:D2}.xml");
                GestureIO.WriteGesture(_points.ToArray(), MedialKeys[i], file);
                Debug.Log($"[Drawing] 중성 「{MedialKeys[i]}」 실필기 저장 — 템플릿 재로드");
                ClearDrawing();
                LoadAllTemplates();
                return;
            }
        }

        // ---- 먹선 입력·표현 ----

        private void HandleStroke()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _strokeId++;
                _strokes.Add(NewStroke());
                _brushScreenPos = Input.mousePosition;
                _prevSample = _brushScreenPos;
            }
            if (!Input.GetMouseButton(0)) return;

            _idleTimer = 0f;
            float k = 1f - Mathf.Exp(-_brushLerpSpeed * Time.unscaledDeltaTime);
            _brushScreenPos = Vector3.Lerp(_brushScreenPos, Input.mousePosition, k);

            if (Vector3.Distance(_brushScreenPos, _prevSample) < 2f) return;
            float speed = Vector3.Distance(_brushScreenPos, _prevSample) / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            _prevSample = _brushScreenPos;

            _points.Add(new Point(_brushScreenPos.x, Screen.height - _brushScreenPos.y, _strokeId));

            var stroke = _strokes[_strokes.Count - 1];
            Vector3 world = _viewCamera.ScreenToWorldPoint(new Vector3(_brushScreenPos.x, _brushScreenPos.y, 0.6f));
            Vector3 local = _viewCamera.transform.InverseTransformPoint(world);
            var lr = stroke.Line;
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, local);

            float speed01 = Mathf.InverseLerp(0f, 2200f, speed);
            stroke.Widths.Add(Mathf.Lerp(_maxWidthFactor, _minWidthFactor, speed01) * _baseWidth);
            RebuildWidthCurve(stroke);
        }

        private static void RebuildWidthCurve(Stroke stroke)
        {
            int n = stroke.Widths.Count;
            if (n < 2) return;
            int keyCount = Mathf.Min(n, 32);
            var keys = new Keyframe[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                int src = Mathf.RoundToInt((float)i / (keyCount - 1) * (n - 1));
                keys[i] = new Keyframe((float)i / (keyCount - 1), stroke.Widths[src]);
            }
            stroke.Line.widthCurve = new AnimationCurve(keys);
        }

        private void AnimateBleed()
        {
            foreach (var stroke in _strokes)
            {
                if (stroke.Line == null) continue;
                float t = Mathf.Clamp01((Time.unscaledTime - stroke.BornTime) / _bleedSeconds);
                stroke.Line.widthMultiplier = Mathf.Lerp(1f, _bleedMultiplier, Mathf.SmoothStep(0f, 1f, t));
            }
        }

        private Stroke NewStroke()
        {
            var go = new GameObject($"InkStroke_{_strokeId}");
            go.transform.SetParent(_viewCamera.transform, false);
            go.layer = LayerMask.NameToLayer("ViewModel");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _lineMaterial;
            lr.startColor = lr.endColor = _inkColor;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.positionCount = 0;
            lr.useWorldSpace = false; // 카메라 로컬 공간 — 이동·회전을 따라온다
            lr.widthMultiplier = 1f;
            lr.startWidth = lr.endWidth = _baseWidth;
            return new Stroke { Line = lr, BornTime = Time.unscaledTime };
        }

        private static Texture2D CreateSoftEdgeTexture()
        {
            const int h = 64;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float d = Mathf.Abs(y - (h - 1) * 0.5f) / ((h - 1) * 0.5f);
                float a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.45f, 1f, d));
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return tex;
        }

        private void ClearDrawing()
        {
            _points.Clear();
            _strokeId = -1;
            _idleTimer = 0f;
            foreach (var s in _strokes) if (s.Line != null) Destroy(s.Line.gameObject);
            _strokes.Clear();
        }

        // 작도 진행 안내 (프로토 임시 표시)
        private void OnGUI()
        {
            if (_modeController == null || _modeController.Mode != SpellcraftMode.Drawing) return;
            string msg = _recordMode
                ? "[등록 모드] 중성을 그리고 1=ㅏ 2=ㅓ 3=ㅗ 4=ㅜ 로 저장 | F2 = 등록 종료"
                : "작도: 글자를 이어 그리세요 (예: 가 = ㄱ+ㅏ, 노 = ㄴ+ㅗ) — 잠시 멈추면 발동 | F2 = 중성 등록";
            GUI.Label(new Rect(10, Screen.height - 30, 900, 24), msg);
        }
    }
}
