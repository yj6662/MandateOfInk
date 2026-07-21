using UnityEngine;

namespace MandateOfInk.Combat
{
    // 진(술식) 표현 공통 도구 — VFX 에셋 대신 반투명 프리미티브 + 인식 글자 텍스트로 표현한다.
    // 정식 수묵 연출은 M1 룩 단계 몫이고, 이것은 「무슨 기술인지 읽히는」 기능 우선 표현이다.
    public static class SpellVisuals
    {
        private static Font _font;

        private static Font LetterFont
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // 반투명 프리미티브 생성. keepColliderAsTrigger=true면 콜라이더를 트리거로 유지, 아니면 제거.
        public static GameObject CreateTranslucent(PrimitiveType type, Color color, Vector3 scale, bool keepColliderAsTrigger)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (keepColliderAsTrigger) col.isTrigger = true;
            else Object.Destroy(col);
            go.transform.localScale = scale;

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = MakeTranslucentMaterial(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        // URP에서 확실히 동작하는 무광 반투명 머티리얼 (Sprites/Default — 먹선과 동일 셰이더)
        public static Material MakeTranslucentMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default");
            return new Material(shader) { color = color };
        }

        // 인식된 글자를 오브젝트 위에 띄운다 (항상 카메라를 향함).
        // [가정] 그린 획 원본 표시가 아닌 텍스트 표기 — 획 텍스처 방식은 M1 폴리시에서 검토.
        public static void AttachLetter(Transform parent, string letter, float worldHeight, Color textColor)
        {
            var go = new GameObject("Letter");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            // 부모 스케일의 영향을 지우고 월드 크기를 고정한다
            Vector3 ls = parent.lossyScale;
            go.transform.localScale = new Vector3(
                ls.x > 0.0001f ? 1f / ls.x : 1f,
                ls.y > 0.0001f ? 1f / ls.y : 1f,
                ls.z > 0.0001f ? 1f / ls.z : 1f);

            var tm = go.AddComponent<TextMesh>();
            tm.text = letter;
            tm.font = LetterFont;
            tm.fontSize = 64;
            tm.characterSize = worldHeight * 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = textColor;

            // TextMesh 기본 텍스트 셰이더는 URP에서 깨질 수 있어 폰트 아틀라스를 Sprites/Default로 그린다
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            LetterFont.RequestCharactersInTexture(letter, tm.fontSize, FontStyle.Normal);
            mat.mainTexture = LetterFont.material.mainTexture;
            mr.material = mat;

            go.AddComponent<SpellBillboard>();
        }

        // 판정용 프리미티브를 시각적으로 숨긴다(렌더러 비활성) — 콜라이더·스크립트는 유지.
        // 문양 프리팹으로 완전 교체할 때 판정 구는 보이지 않게만 둔다.
        public static void HideRenderer(GameObject go)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        // 문양 진 프리팹을 부모에 부착해 바닥에 깐다(수평, 지름 맞춤).
        // tint를 주면 파티클 색을 오방색으로 밀어준다(에셋은 청록·주황 위주라 오행 색과 어긋나므로).
        public static GameObject AttachGroundPattern(Transform parent, GameObject prefab, float worldDiameter, bool followParent, Color? tint = null)
        {
            if (prefab == null) return null;
            var fx = Object.Instantiate(prefab);
            fx.name = "PatternCircle";
            if (followParent)
            {
                fx.transform.SetParent(parent, false);
                fx.transform.localPosition = Vector3.zero;
                fx.transform.localRotation = Quaternion.identity;
            }
            else
            {
                fx.transform.position = parent.position;
            }
            // 에셋 진은 XZ 평면(바닥)을 상정 — 부모 스케일 영향을 지우고 월드 지름으로 정규화
            NormalizePatternScale(fx.transform, parent, worldDiameter);
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            return fx;
        }

        // 발사체 문양을 투사체에 부착(진행 방향 정렬)
        public static GameObject AttachProjectilePattern(Transform parent, GameObject prefab, float worldDiameter, Color? tint = null)
        {
            if (prefab == null) return null;
            var fx = Object.Instantiate(prefab);
            fx.name = "PatternProjectile";
            fx.transform.SetParent(parent, false);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
            NormalizePatternScale(fx.transform, parent, worldDiameter);
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            return fx;
        }

        // 문양의 모든 파티클 startColor를 오방색으로 물들인다 — 명도는 살리되 색상은 tint로 곱한다.
        private static void ApplyTint(GameObject fx, Color tint)
        {
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                var sc = main.startColor;
                if (sc.mode == ParticleSystemGradientMode.Color)
                {
                    main.startColor = MultiplyKeepValue(sc.color, tint);
                }
                else if (sc.mode == ParticleSystemGradientMode.TwoColors)
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        MultiplyKeepValue(sc.colorMin, tint), MultiplyKeepValue(sc.colorMax, tint));
                }
                else
                {
                    main.startColor = tint;
                }
            }
        }

        // 원래 색의 명도(밝기)는 유지하고 색상만 tint로 교체 — 흰 하이라이트가 죽지 않게.
        private static Color MultiplyKeepValue(Color original, Color tint)
        {
            float value = Mathf.Max(original.r, Mathf.Max(original.g, original.b)); // 원래 밝기
            Color.RGBToHSV(tint, out float h, out float s, out _);
            Color tinted = Color.HSVToRGB(h, s, value);
            tinted.a = original.a;
            return tinted;
        }

        private static void NormalizePatternScale(Transform fx, Transform parent, float worldDiameter)
        {
            Vector3 ls = parent.lossyScale;
            float inv = worldDiameter / Mathf.Max(0.01f,
                Mathf.Max(Mathf.Abs(ls.x), Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z))));
            fx.localScale = Vector3.one * inv;
        }

        // 명중·소멸 등에 쓰는 팽창-소멸 구
        public static void SpawnBurst(Vector3 position, Color color, float maxDiameter, float seconds = 0.3f)
        {
            var go = CreateTranslucent(PrimitiveType.Sphere, color, Vector3.one * 0.05f, keepColliderAsTrigger: false);
            go.name = "SpellBurst";
            go.transform.position = position;
            go.AddComponent<SpellBurstVisual>().Init(maxDiameter, seconds);
        }
    }

    // 텍스트·표식이 항상 카메라를 향하게 한다
    public sealed class SpellBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    // 팽창하며 옅어지다 사라지는 시각 효과 (파티클 대체)
    public sealed class SpellBurstVisual : MonoBehaviour
    {
        private float _maxDiameter;
        private float _seconds;
        private float _elapsed;
        private Material _material;
        private Color _baseColor;

        public void Init(float maxDiameter, float seconds)
        {
            _maxDiameter = maxDiameter;
            _seconds = Mathf.Max(seconds, 0.05f);
            _material = GetComponent<MeshRenderer>().material;
            _baseColor = _material.color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _seconds);
            transform.localScale = Vector3.one * Mathf.Lerp(0.05f, _maxDiameter, Mathf.Sqrt(t));
            var c = _baseColor;
            c.a *= 1f - t;
            _material.color = c;
            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
