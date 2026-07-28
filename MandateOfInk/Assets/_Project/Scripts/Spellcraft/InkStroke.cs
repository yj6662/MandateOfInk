using System.Collections.Generic;
using UnityEngine;

namespace MandateOfInk.Spellcraft
{
    // 먹 획 하나를 절차 생성 메시로 그린다 (LineRenderer 대체).
    // 점별 폭(굵기)·정점 알파(농도)·붓자국 아틀라스 행을 제어해 붓글씨 질감을 낸다.
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
        private bool _released;

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
            _atlasRows = Mathf.Max(atlasRows, 1);
            _atlasRow = Mathf.Clamp(atlasRow, 0, _atlasRows - 1);
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
            if (_released || Mathf.Approximately(m, _widthMultiplier)) return;
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

                Vector2 d2 = new Vector2(dir.x, dir.y).normalized;
                var normal = new Vector3(-d2.y, d2.x, 0f);
                float half = s.Width * _widthMultiplier * 0.5f;

                vertices[i * 2] = s.Center + normal * half;
                vertices[i * 2 + 1] = s.Center - normal * half;

                byte a = (byte)(Mathf.Clamp01(s.Density) * _inkColor.a * 255f);
                var c = new Color32((byte)(_inkColor.r * 255), (byte)(_inkColor.g * 255), (byte)(_inkColor.b * 255), a);
                colors[i * 2] = c;
                colors[i * 2 + 1] = c;

                // U = 획 시작 0 -> 끝 1 (붓자국의 기필·수필이 실제 획 양끝에 얹힌다), V = 이 획의 아틀라스 행
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
            if (_released) return; // 소멸 연출로 넘어간 획은 페이더가 수명을 관리한다
            if (_root != null) Object.Destroy(_root);
            if (_mesh != null) Object.Destroy(_mesh);
        }

        // 판정 후 소멸 연출로 전환 — 이후 수명은 InkStrokeFader가 스스로 관리한다.
        // 정발동: 가장자리 글로우와 함께 짧게 사라짐 / 약발동: 아무 효과 없이 서서히 사라짐.
        public void ReleaseForFade(bool success, float duration, Color glowColor)
        {
            if (_root == null || _released) return;
            Apply();
            _released = true;
            var fader = _root.AddComponent<InkStrokeFader>();
            fader.Init(_mesh, duration, success, glowColor);
        }

        // 붓결 텍스처 아틀라스 절차 생성 — 생성 텍스처가 없을 때의 폴백.
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
                    float across = (y + 0.5f) / rowH;
                    float edge = Mathf.Abs(across - 0.5f) * 2f;
                    float edgeAlpha = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.72f, 1f, edge));
                    for (int x = 0; x < w; x++)
                    {
                        float u = x / (float)w;
                        float streak = Mathf.PerlinNoise(u * 7f + row * 13.7f, across * 9f + row * 31.1f);
                        float gap = Mathf.InverseLerp(0.35f + (1f - dry) * 0.65f, 1f, streak);
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

    // 판정이 끝난 먹 획의 소멸을 담당하는 일회용 컴포넌트.
    // 성공 글로우는 같은 메시의 사본을 가산 블렌드로 살짝 뒤에 겹쳐서 낸다 —
    // 먹 중심부(불투명)에는 가려지고 부드러운 가장자리에서만 배어 나와 「테두리 글로우」로 보인다.
    public sealed class InkStrokeFader : MonoBehaviour
    {
        private static Material _glowMaterial; // 공유 가산 머티리얼

        private Mesh _mesh;
        private Color32[] _inkOriginal;
        private Color32[] _inkScratch;   // 매 프레임 재사용 — GC 할당 방지
        private Mesh _glowMesh;
        private Color32[] _glowOriginal;
        private Color32[] _glowScratch;
        private float _duration;
        private float _elapsed;

        public void Init(Mesh mesh, float duration, bool success, Color glowColor)
        {
            _mesh = mesh;
            _inkOriginal = mesh.colors32;
            _inkScratch = new Color32[_inkOriginal.Length];
            _duration = Mathf.Max(duration, 0.05f);

            if (!success) return;

            if (_glowMaterial == null)
            {
                var shader = Shader.Find("Legacy Shaders/Particles/Additive");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _glowMaterial = new Material(shader);
                var src = GetComponent<MeshRenderer>();
                if (src != null) _glowMaterial.mainTexture = src.sharedMaterial.mainTexture;
            }

            // 글로우용 메시 사본: 먹색 정점을 글로우 색으로 (알파는 원본 모양 유지)
            _glowMesh = Instantiate(_mesh);
            _glowOriginal = new Color32[_inkOriginal.Length];
            var gc = (Color32)glowColor;
            for (int i = 0; i < _glowOriginal.Length; i++)
                _glowOriginal[i] = new Color32(gc.r, gc.g, gc.b, (byte)(_inkOriginal[i].a * glowColor.a));
            _glowScratch = new Color32[_glowOriginal.Length];
            _glowMesh.colors32 = _glowOriginal;

            var glowGo = new GameObject("InkGlow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0f, 0.004f); // 먹선 살짝 뒤
            glowGo.layer = gameObject.layer;
            glowGo.AddComponent<MeshFilter>().sharedMesh = _glowMesh;
            var mr = glowGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _glowMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float remain = 1f - Mathf.Clamp01(_elapsed / _duration);
            if (remain <= 0f) { Destroy(gameObject); return; }

            ScaleAlpha(_inkOriginal, _inkScratch, remain);
            _mesh.colors32 = _inkScratch;
            if (_glowMesh != null)
            {
                ScaleAlpha(_glowOriginal, _glowScratch, remain);
                _glowMesh.colors32 = _glowScratch;
            }
        }

        private static void ScaleAlpha(Color32[] source, Color32[] dest, float factor)
        {
            for (int i = 0; i < source.Length; i++)
            {
                var c = source[i];
                dest[i] = new Color32(c.r, c.g, c.b, (byte)(c.a * factor));
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_glowMesh != null) Destroy(_glowMesh);
        }
    }
}
