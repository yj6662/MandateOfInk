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

        [Header("판정 — 홀드 키를 떼면 확정 (키·임계는 SpellcraftModeConfigSO)")]
        [SerializeField] private SpellcraftModeConfigSO _config;
        [SerializeField] private InkPool _inkPool; // 먹 소모 (없으면 무료 — 프로토 폴백)
        [SerializeField] private string _initialTemplateDir = "_Project/Data/JamoTemplates/Initials";
        [SerializeField] private string _medialTemplateDir = "_Project/Data/JamoTemplates/Medials";

        [Header("[가정] 소멸 연출")]
        [Tooltip("정발동: 테두리 글로우와 함께 짧게 소멸")]
        [SerializeField] private float _successFadeSeconds = 0.45f;
        [SerializeField] private Color _glowColor = new Color(1f, 0.92f, 0.6f, 0.55f);
        [Tooltip("약발동: 효과 없이 서서히 소멸")]
        [SerializeField] private float _weakFadeSeconds = 1.2f;

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

        [Header("[가정] 쿼터뷰 표시 — 1인칭이 아닐 때 글자를 머리 위에 크게 띄운다")]
        [SerializeField] private float _quarterGlyphScale = 3f;
        [SerializeField] private float _quarterGlyphHeight = 2.4f;

        private static readonly string[] MedialKeys = { "ㅏ", "ㅓ", "ㅗ", "ㅜ" };
        private static readonly string[] MedialTags = { "a", "eo", "o", "u" };

        private readonly List<Point> _points = new List<Point>();
        private readonly List<InkStroke> _strokes = new List<InkStroke>();
        private Gesture[] _initialTemplates = new Gesture[0];
        private Gesture[] _medialTemplates = new Gesture[0];

        private int _strokeId = -1;
        private Vector3 _brushScreenPos;
        private Vector3 _prevSample;
        private Vector3 _prevLocal;
        private int _sampleInStroke;
        private float _inkUsed; // 글자 단위 누적 — 획을 거듭할수록 갈필이 된다
        private float _noiseSeed;
        private Material _inkMaterial;
        private bool _recordMode;
        private Transform _strokeRoot;  // 획들을 담는 루트 — 시점에 따라 위치·크기를 바꾼다
        private Transform _playerRoot;

        private void Start()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            _inkMaterial = new Material(shader)
            {
                mainTexture = _brushAtlas != null ? (Texture)_brushAtlas : InkStroke.CreateBrushAtlas()
            };
            if (_brushAtlas == null) _brushAtlasRows = InkStroke.ProceduralAtlasRows;

            var rootGo = new GameObject("InkStrokeRoot");
            rootGo.transform.SetParent(_viewCamera.transform, false);
            _strokeRoot = rootGo.transform;

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
            UpdateStrokeRootPose();

            if (_recordMode) { HandleRecordKeys(); return; } // 등록 모드는 키를 떼도 유지(F2로 종료)

            // 홀드 방식: 작도 키를 떼는 순간 그린 글자를 판정한다
            if (_config != null && Input.GetKeyUp(_config.ToggleKey))
            {
                if (_points.Count >= 8) RecognizeLetter();
                else { ClearDrawing(); _modeController.CompleteDrawing(); } // 그리다 만 것은 취소
            }
        }

        // 1인칭이면 획 루트를 뷰모델 카메라에 밀착(기본), 쿼터뷰(다른 MainCamera 활성)면
        // 플레이어 머리 위에 확대·빌보드로 띄워 어느 시점에서도 작도가 읽히게 한다.
        private void UpdateStrokeRootPose()
        {
            if (_strokeRoot == null || _viewCamera == null) return;
            var activeCam = Camera.main;
            bool firstPerson = activeCam == null || activeCam.transform == _viewCamera.transform.parent;

            if (firstPerson)
            {
                _strokeRoot.SetParent(_viewCamera.transform, false);
                _strokeRoot.localPosition = Vector3.zero;
                _strokeRoot.localRotation = Quaternion.identity;
                _strokeRoot.localScale = Vector3.one;
                return;
            }

            if (_playerRoot == null)
            {
                var cc = FindFirstObjectByType<CharacterController>();
                if (cc != null) _playerRoot = cc.transform;
                if (_playerRoot == null) return;
            }

            _strokeRoot.SetParent(null, true);
            Vector3 anchor = _playerRoot.position + Vector3.up * _quarterGlyphHeight;
            var rot = Quaternion.LookRotation(anchor - activeCam.transform.position); // 카메라가 -z 쪽 = 글자가 바로 읽힘
            _strokeRoot.rotation = rot;
            // 획 점들은 루트 로컬 z≈0.6 평면에 있으므로 그만큼 당겨 글자 중심을 앵커에 맞춘다
            _strokeRoot.position = anchor - rot * (Vector3.forward * 0.6f * _quarterGlyphScale);
            _strokeRoot.localScale = Vector3.one * _quarterGlyphScale;
        }

        // ---- 인식 (획 그룹 분할) ----

        private void RecognizeLetter()
        {
            if (_initialTemplates.Length == 0) { ClearDrawing(); _modeController.CompleteDrawing(); return; }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int strokeCount = _strokeId + 1;
            string initial = null, medial = null;
            float worstJamoDistance; // 가장 서툰 자모의 거리 — 약발동 판정 기준

            if (strokeCount < 2 || _medialTemplates.Length == 0)
            {
                var m = JamoMatcher.Classify(_points.ToArray(), _initialTemplates);
                initial = m.Name;
                medial = "ㅏ";
                worstJamoDistance = m.Distance;
            }
            else
            {
                float bestMetric = float.MaxValue;
                float bestIniDist = 0f, bestMedDist = 0f;
                Point[] bestMedPts = null;
                for (int split = 1; split < strokeCount; split++)
                {
                    var iniPts = CollectPoints(0, split);
                    var medPts = CollectPoints(split, strokeCount);
                    if (iniPts.Length < 4 || medPts.Length < 4) continue;
                    // 순수 직선 하나뿐인 중성 후보(예: ㅏ의 가로점만 떼어진 그룹)는 잘못된 분할 — 배제.
                    // $P는 퇴화된 직선 그룹에 부당하게 좋은 거리를 주므로 구조로 걸러야 한다.
                    if (!IsPlausibleMedialShape(medPts)) continue;

                    var mi = JamoMatcher.Classify(iniPts, _initialTemplates);
                    var mm = JamoMatcher.Classify(medPts, _medialTemplates);
                    float metric = mi.Distance + mm.Distance;
                    if (metric < bestMetric)
                    {
                        bestMetric = metric;
                        initial = mi.Name;
                        medial = mm.Name;
                        bestIniDist = mi.Distance;
                        bestMedDist = mm.Distance;
                        bestMedPts = medPts;
                    }
                }
                // 거울상 중성은 기하 판별로 확정 ($P는 분할 선택까지만)
                if (bestMedPts != null)
                {
                    medial = ClassifyMedialByGeometry(bestMedPts, medial);
                    worstJamoDistance = Mathf.Max(bestIniDist, bestMedDist);
                }
                else
                {
                    // 유효한 분할이 없음(중성을 안 그린 경우 등) — 전체를 초성으로 보고 ㅏ 기본형 폴백
                    var m = JamoMatcher.Classify(_points.ToArray(), _initialTemplates);
                    initial = m.Name;
                    medial = "ㅏ";
                    worstJamoDistance = float.MaxValue; // 항상 약발동
                }
            }
            sw.Stop();

            // 약발동 판정 — 완전 불발 금지(절대 규칙): 임계 미달이면 가장 가까운 글자를 약하게 발동
            bool isWeak = _config != null && worstJamoDistance > _config.WeakCastDistanceThreshold;
            float power = isWeak && _config != null ? _config.WeakCastPowerMultiplier : 1f;

            var diagram = _library.FindByJamo(initial, medial, "") ?? _library.FindByJamo(initial, "ㅏ", "");

            // 먹 소모 — 부족하면 쥐어짜기(잔량 비율만큼 약해진 채 발동, 잔량 전부 소모)
            if (diagram != null && _inkPool != null)
            {
                float inkPower = _inkPool.TrySpendForCast(diagram.InkCost);
                if (inkPower < 0.999f) { isWeak = true; power *= inkPower; }
            }
            Debug.Log($"[Drawing] 분할 인식 {initial}+{medial} (최악 자모 거리 {worstJamoDistance:F2}, {sw.Elapsed.TotalMilliseconds:F1}ms, {strokeCount}획) -> 「{(diagram != null ? diagram.Letter : "없음")}」{(isWeak ? " [약발동]" : "")}");

            if (diagram != null)
                _diagramDrawn?.Raise(new DiagramCastRequest { Diagram = diagram, IsWeak = isWeak, PowerMultiplier = power });

            // 먹선 소멸 연출: 정발동 = 글로우와 함께 짧게 / 약발동 = 효과 없이 서서히
            ReleaseStrokes(!isWeak);
            _modeController.CompleteDrawing();
        }

        // 판정이 끝난 획들을 소멸 연출로 넘기고 작도 상태만 초기화한다 (파괴는 페이더 담당)
        private void ReleaseStrokes(bool success)
        {
            foreach (var s in _strokes)
                s.ReleaseForFade(success, success ? _successFadeSeconds : _weakFadeSeconds, _glowColor);
            _strokes.Clear();
            _points.Clear();
            _strokeId = -1;
            _inkUsed = 0f;
        }

        // 중성 후보 구조 검증 — 기본 중성(ㅏㅓㅗㅜ)은 「긴 획 + 직교 짧은 획」이라
        // 보조축 폭이 주축의 일정 비율 이상이어야 한다. (추후 ㅣ·ㅡ 특수 중성 도입 시 재설계 필요)
        private static bool IsPlausibleMedialShape(Point[] rawPoints)
        {
            var pts = new Gesture(rawPoints).Points;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
            }
            float w = maxX - minX, h = maxY - minY;
            float major = Mathf.Max(w, h), minor = Mathf.Min(w, h);
            return minor > major * 0.22f; // [가정] 직선 판정 임계
        }

        // 중성 기하 판별 — ㅏ/ㅓ, ㅗ/ㅜ는 거울상이라 $P 거리로는 변별이 약하다.
        // 정규화 점구름(무게중심=원점)에서 바운딩박스 중심의 부호로 긴 획(점 많음)과 점획의 방향을 가른다.
        // 세로형: 박스중심 x>0 이면 점이 오른쪽 = ㅏ, 왼쪽 = ㅓ. 가로형(y 아래+): y<0 이면 점이 위 = ㅗ, 아래 = ㅜ.
        private static string ClassifyMedialByGeometry(Point[] rawPoints, string fallback)
        {
            var pts = new Gesture(rawPoints).Points; // 32점 균등 리샘플·정규화(무게중심 원점)
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                if (p.X < minX) minX = p.X; if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y; if (p.Y > maxY) maxY = p.Y;
            }
            float w = maxX - minX, h = maxY - minY;
            float centerX = (minX + maxX) * 0.5f, centerY = (minY + maxY) * 0.5f;

            if (h > w * 1.15f) return centerX > 0f ? "ㅏ" : "ㅓ";
            if (w > h * 1.15f) return centerY < 0f ? "ㅗ" : "ㅜ";
            return fallback; // 종횡이 애매하면 $P 결과 유지
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
                _strokes.Add(new InkStroke(_strokeRoot, _inkMaterial,
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
