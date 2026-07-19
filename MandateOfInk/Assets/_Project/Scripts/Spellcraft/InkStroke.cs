using System.Collections.Generic;
using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 먹 획 하나를 절차 생성 메시로 그린다 (LineRenderer 대체).
    // 점별 폭(굵기)·정점 알파(농도)·텍스처 아틀라스 행(마름/비백)을 제어해 붓글씨 질감을 낸다.
    // 셰이더 저작 없이 Sprites/Default(정점색 x 텍스처)로 동작한다.
    public sealed class InkStroke
    {
        private struct Sample
        {
            public Vector3 Center; // 카메라 로컬
            public float Width;
            public float Density;  // 0~1 농도(알파)
        }

        public const int ProceduralAtlasRows = 6;

        private readonly GameObject _root;
        private readonly Mesh _mesh;
        private readonly List<Sample> _samples = new List<Sample>();
        private readonly Color _inkColor;
        private readonly int _atlasRow;   // 이 획이 쓸 붓자국 행 (0=촉촉)
        private readonly int _atlasRows;
        private float _cumLength;
        private float _widthMultiplier = 1f;
        private bool _dirty;

        public float BornTime { get; }
        public GameObject Root => _root;

        public InkStroke(Transform parent, Material material, int layer, Color inkColor, int atlasRow, int atlasRows, string name)
        {
            _root = new GameObject(name);
            _root.transform.SetParent(parent, false);
            _root.layer = layer;
            _mesh = new Mesh { name = "InkStrokeMesh" };
            _mesh.MarkDynamic();
            _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = _root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _inkColor = inkColor;
            _atlasRow = Mathf.Clamp(atlasRow, 0, atlasRows - 1);
            _atlasRows = Mathf.Max(atlasRows, 1);
            BornTime = Time.unscaledTime;
        }

        public void AddSample(Vector3 localCenter, float width, float density)
        {
            if (_samples.Count > 0)
                _cumLength += Vector3.Distance(_samples[_samples.Count - 1].Center, localCenter);
            _samples.Add(new Sample { Center = localCenter, Width = width, Density = density });
            _dirty = true;
        }

        // 수필(收筆): 획 끝을 마지막 진행 방향으로 뾰족하게 뺀다
        public void EndTaper(float taperLength)
        {
            if (_samples.Count < 2) return;
            var last = _samples[_samples.Count - 1];
            var prev = _samples[_samples.Count - 2];
            Vector3 dir = (last.Center - prev.Center).normalized;
            int steps = 3;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                AddSample(last.Center + dir * (taperLength * t),
                    last.Width * (1f - t) * 0.7f,
                    last.Density * (1f - 0.4f * t));
            }
        }

        public void SetWidthMultiplier(float m)
        {
            if (Mathf.Approximately(m, _widthMultiplier)) return;
            _widthMultiplier = m;
            _dirty = true;
        }

        // 변경이 있을 때만 리빌드 (번짐 애니메이션 중에도 호출)
        public void Apply()
        {
            if (!_dirty || _samples.Count < 2) return;
            _dirty = false;

            int n = _samples.Count;
            var vertices = new Vector3[n * 2];
            var colors = new Color32[n * 2];
            var uvs = new Vector2[n * 2];
            var tris = new int[(n - 1) * 6];

            float len = 0f;
            for (int i = 0; i < n; i++)
            {
                var s = _samples[i];
                Vector3 dir;
                if (i == 0) dir = _samples[1].Center - s.Center;
                else if (i == n - 1) dir = s.Center - _samples[i - 1].Center;
                else dir = _samples[i + 1].Center - _samples[i - 1].Center;
                if (i > 0) len += Vector3.Distance(s.Center, _samples[i - 1].Center);

                // 화면 평면(카메라 로컬 XY)에서의 수직 방향
                Vector2 d2 = new Vector2(dir.x, dir.y).normalized;
                var normal = new Vector3(-d2.y, d2.x, 0f);
                float half = s.Width * _widthMultiplier * 0.5f;

                vertices[i * 2] = s.Center + normal * half;
                vertices[i * 2 + 1] = s.Center - normal * half;

                byte a = (byte)(Mathf.Clamp01(s.Density) * _inkColor.a * 255f);
                var c = new Color32((byte)(_inkColor.r * 255), (byte)(_inkColor.g * 255), (byte)(_inkColor.b * 255), a);
                colors[i * 2] = c;
                colors[i * 2 + 1] = c;

                // U = 획 시작 0 -> 끝 1 (텍스처의 기필·수필이 실제 획 양끝에 얹힌다), V = 이 획의 붓자국 행
                float v0 = (_atlasRow + 0.04f) / _atlasRows;
                float v1 = (_atlasRow + 0.96f) / _atlasRows;
                float u = _cumLength > 0.0001f ? len / _cumLength : 0f;
                uvs[i * 2] = new Vector2(u, v1);
                uvs[i * 2 + 1] = new Vector2(u, v0);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int b = i * 6, v = i * 2;
                tris[b] = v; tris[b + 1] = v + 1; tris[b + 2] = v + 2;
                tris[b + 3] = v + 2; tris[b + 4] = v + 1; tris[b + 5] = v + 3;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.colors32 = colors;
            _mesh.uv = uvs;
            _mesh.triangles = tris;
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
            if (_mesh != null) Object.Destroy(_mesh);
        }

        // 붓결 텍스처 아틀라스 생성 — 행이 위로 갈수록 마른 붓(비백: 세로 붓결 틈이 커짐).
        // 가장자리는 부드럽게 빠지고, 결의 흐트러짐은 펄린 노이즈로 만든다.
        public static Texture2D CreateBrushAtlas()
        {
            const int w = 256, rowH = 32;
            var tex = new Texture2D(w, rowH * ProceduralAtlasRows, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int row = 0; row < ProceduralAtlasRows; row++)
            {
                float dry = row / (float)(ProceduralAtlasRows - 1); // 0=먹 가득, 1=갈필
                for (int y = 0; y < rowH; y++)
                {
                    float across = (y + 0.5f) / rowH;             // 0~1 획 단면 위치
                    float edge = Mathf.Abs(across - 0.5f) * 2f;   // 0(중앙)~1(가장자리)
                    float edgeAlpha = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.72f, 1f, edge));
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w;
                        // 붓결: 길이 방향으로 흐르는 노이즈 결 — 마를수록 임계가 낮아져 틈(비백)이 생긴다
                        float streak = Mathf.PerlinNoise(u * 7f + row * 13.7f, across * 9f + row * 31.1f);
                        float gap = Mathf.InverseLerp(0.35f + (1f - dry) * 0.65f, 1f, streak); // dry=0이면 gap 없음
                        float grain = 0.96f + 0.04f * Mathf.PerlinNoise(u * 23f, across * 17f + row * 7.3f);
                        float a = edgeAlpha * (1f - gap) * grain * (1f - 0.08f * dry);
                        tex.SetPixel(x, row * rowH + y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                    }
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
