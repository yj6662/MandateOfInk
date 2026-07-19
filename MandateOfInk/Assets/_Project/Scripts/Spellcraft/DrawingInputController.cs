using System.Collections.Generic;
using System.IO;
using MandateOfInk.Data;
using PDollarGestureRecognizer;
using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 작도 모드 입력: 글자(초성+중성)를 이어 그리면 「획 그룹 분할」로 인식해 발동한다.
    // 먹선 표현(InkStroke 메시): 붓끝 Lerp 끌림 + 속도 기반 굵기·농도 + 먹 소모(뒤 획일수록 갈필)
    // + 비백(마른 붓 틈) + 기필(시작 눌림)·수필(끝 빼기) + 번짐. 파라미터는 전부 [가정] — 룩 판정은 사람.
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

        [Header("[가정] 붓 움직임")]
        [Tooltip("붓끝이 커서를 따라오는 속도 — 낮을수록 무겁게 끌린다")]
        [SerializeField] private float _brushLerpSpeed = 8f;

        [Header("[가정] 먹선 굵기")]
        [SerializeField] private float _baseWidth = 0.018f;
        [Tooltip("빠른 획일수록 가늘게(min) / 느린 획일수록 굵게(max)")]
        [SerializeField] private float _minWidthFactor = 0.45f;
        [SerializeField] private float _maxWidthFactor = 1.5f;
        [Tooltip("굵기에 유기적 흔들림을 주는 노이즈 진폭")]
        [SerializeField, Range(0f, 0.5f)] private float _widthNoise = 0.22f;
        [Tooltip("기필: 획 시작 몇 샘플 동안 눌린 굵기 배율")]
        [SerializeField] private float _startPressFactor = 1.35f;
        [SerializeField] private int _startPressSamples = 5;
        [Tooltip("수필: 획 끝을 빼는 길이(m)")]
        [SerializeField] private float _endTaperLength = 0.025f;

        [Header("[가정] 먹 농도")]
        [Tooltip("느린 획의 농도(진함)")]
        [SerializeField, Range(0f, 1f)] private float _maxDensity = 1f;
        [Tooltip("빠른 획의 농도(옅음)")]
        [SerializeField, Range(0f, 1f)] private float _minDensity = 0.8f;
        [Tooltip("한 글자 동안 먹이 마르는 총 길이(m) — 길수록 천천히 마른다")]
        [SerializeField] private float _inkCapacity = 1.2f;
        [Tooltip("붓자국 아틀라스(생성 텍스처) — 비우면 절차 생성 텍스처 사용")]
        [SerializeField] private Texture2D _brushAtlas;
        [SerializeField] private int _brushAtlasRows = 4;
        [SerializeField] private Color _inkColor = new Color(0.02f, 0.02f, 0.02f, 1f);

        [Header("[가정] 번짐")]
        [SerializeField] private float _bleedMultiplier = 1.62f;
        [SerializeField] private float _bleedSeconds = 1.1f;

        private static readonly string[] MedialKeys = { "ㅏ", "ㅓ", "ㅗ", "ㅜ" };
        private static readonly string[] MedialTags = { "a", "eo", "o", "u" };

        private readonly List<Point> _points = new List<Point>();
        private readonly List<InkStroke> _strokes = new List<InkStroke>();
        private Gesture[] _initialTemplates = new Gesture[0];
        private Gesture[] _medialTemplates = new Gesture[0];

        private int _strokeId = -1;
        private float _idleTimer;
        private Vector3 _brushScreenPos;
        private Vector3 _prevSample;
        private Vector3 _prevLocal;
        private int _sampleInStroke;
        private float _inkUsed; // 글자 단위 누적 — 획을 거듭할수록 갈필이 된다
        private float _noiseSeed;
        private Material _inkMaterial;
        private bool _recordMode;

        private void Start()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            _inkMaterial = new Material(shader)
            {
                mainTexture = _brushAtlas != null ? (Texture)_brushAtlas : InkStroke.CreateBrushAtlas()
            };
            if (_brushAtlas == null) _brushAtlasRows = InkStroke.ProceduralAtlasRows;
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

        // ---- 인식 (획 그룹 분할) ----

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
                Result r = PointCloudRecognizer.Classify(new Gesture(_points.ToArray()), _initialTemplates);
                initial = r.GestureClass;
                medial = "ㅏ";
                bestScore = r.Score;
            }
            else
            {
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
                // 붓자국 행 선택: 먹 잔량이 적을수록 마른 행 + 약간의 무작위 — 획마다 다른 붓자국
                float chargeNow = 1f - Mathf.Clamp01(_inkUsed / Mathf.Max(_inkCapacity, 0.01f));
                int row = Mathf.Clamp(
                    Mathf.RoundToInt((1f - chargeNow) * (_brushAtlasRows - 1) + Random.Range(-0.7f, 0.7f)),
                    0, _brushAtlasRows - 1);
                _strokes.Add(new InkStroke(_viewCamera.transform, _inkMaterial,
                    LayerMask.NameToLayer("ViewModel"), _inkColor, row, _brushAtlasRows, $"InkStroke_{_strokeId}"));
                _brushScreenPos = Input.mousePosition;
                _prevSample = _brushScreenPos;
                _prevLocal = ScreenToLocal(_brushScreenPos);
                _sampleInStroke = 0;
                _noiseSeed = Random.value * 100f;
            }

            if (Input.GetMouseButtonUp(0) && _strokes.Count > 0)
            {
                // 수필: 획 끝을 뾰족하게 뺀다
                var stroke = _strokes[_strokes.Count - 1];
                stroke.EndTaper(_endTaperLength);
                stroke.Apply();
            }

            if (!Input.GetMouseButton(0)) return;

            _idleTimer = 0f;
            float k = 1f - Mathf.Exp(-_brushLerpSpeed * Time.unscaledDeltaTime);
            _brushScreenPos = Vector3.Lerp(_brushScreenPos, Input.mousePosition, k);

            if (Vector3.Distance(_brushScreenPos, _prevSample) < 2f) return;
            float speed = Vector3.Distance(_brushScreenPos, _prevSample) / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            _prevSample = _brushScreenPos;

            _points.Add(new Point(_brushScreenPos.x, Screen.height - _brushScreenPos.y, _strokeId));

            Vector3 local = ScreenToLocal(_brushScreenPos);
            float segLen = Vector3.Distance(local, _prevLocal);
            _prevLocal = local;
            _inkUsed += segLen;
            _sampleInStroke++;

            float speed01 = Mathf.InverseLerp(0f, 2200f, speed);
            float inkCharge = 1f - Mathf.Clamp01(_inkUsed / Mathf.Max(_inkCapacity, 0.01f)); // 1=먹 가득, 0=다 마름

            // 굵기: 속도(빠르면 가늘게) x 유기적 노이즈 x 기필(시작 눌림)
            float width = _baseWidth * Mathf.Lerp(_maxWidthFactor, _minWidthFactor, speed01);
            width *= 1f + _widthNoise * (Mathf.PerlinNoise(_noiseSeed, _inkUsed * 25f) - 0.5f) * 2f;
            if (_sampleInStroke <= _startPressSamples)
                width *= Mathf.Lerp(_startPressFactor, 1f, (_sampleInStroke - 1f) / _startPressSamples);

            // 농도: 속도(빠르면 옅게) x 먹 잔량(마를수록 옅게)
            float density = Mathf.Lerp(_maxDensity, _minDensity, speed01) * Mathf.Lerp(0.85f, 1f, inkCharge);

            var current = _strokes[_strokes.Count - 1];
            current.AddSample(local, width, density);
            current.Apply();
        }

        private Vector3 ScreenToLocal(Vector3 screenPos)
        {
            Vector3 world = _viewCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0.6f));
            return _viewCamera.transform.InverseTransformPoint(world);
        }

        private void AnimateBleed()
        {
            foreach (var stroke in _strokes)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - stroke.BornTime) / _bleedSeconds);
                stroke.SetWidthMultiplier(Mathf.Lerp(1f, _bleedMultiplier, Mathf.SmoothStep(0f, 1f, t)));
                stroke.Apply();
            }
        }

        private void ClearDrawing()
        {
            _points.Clear();
            _strokeId = -1;
            _idleTimer = 0f;
            _inkUsed = 0f; // 새 글자 = 먹 다시 찍기
            foreach (var s in _strokes) s.Destroy();
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
