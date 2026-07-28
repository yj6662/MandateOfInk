using System.Collections.Generic;
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
            var mat = MakeTranslucentMaterial(color);
            mr.material = mat;
            RuntimeMaterialCleaner.Track(go, mat);
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
            RuntimeMaterialCleaner.Track(go, mat);

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

        // 이 프리팹들은 에디터에서 미리 분해된 순수본이다(SplitFly/): Element_Projectile(비행 몸체만),
        // Element_Explosion(명중 폭발만). Animator·타임라인·불필요 그룹이 이미 제거돼 있어 그냥 붙이면 된다.

        // 비행 몸체 문양을 투사체에 부착.
        public static GameObject AttachProjectilePattern(Transform parent, GameObject prefab, float worldDiameter, Color? tint = null)
        {
            if (prefab == null) return null;
            var fx = Object.Instantiate(prefab);
            fx.name = "PatternProjectile";
            fx.transform.SetParent(parent, false);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
            KeepSingleTrailStream(fx);  // 여러 갈래(FxTrail_*)로 퍼지는 문제 → 가운데 한 줄만 남긴다
            NormalizePatternScale(fx.transform, parent, worldDiameter);
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            return fx;
        }

        // 원본 발사체(Fly 분해본)는 궤적 스트림 FxTrail_01/03/05/07/09가 한 원점에 겹쳐 있어
        // 화면에서 여러 갈래로 퍼져 보인다. 한 갈래만 남기려고 가운데 스트림 하나만 켜고 나머지는 끈다.
        // (오브젝트를 지우지 않고 비활성화만 — Sub Emitter 등 상호참조가 깨지지 않게.)
        private const string PreferredTrailStream = "FxTrail_05"; // 5개 중 가운데(권장). 없으면 첫 번째로 폴백.
        private static void KeepSingleTrailStream(GameObject fx)
        {
            // 하위 어디에 있든 이름이 FxTrail_로 시작하는 최상위 스트림들을 모은다(그 자식 FxTrail은 제외).
            var streams = new System.Collections.Generic.List<Transform>();
            foreach (var t in fx.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("FxTrail_")) continue;
                if (t.parent != null && t.parent.name.StartsWith("FxTrail_")) continue; // 중첩 FxTrail 자식은 제외
                streams.Add(t);
            }
            if (streams.Count <= 1) return; // 이미 한 갈래거나 없음 — 손대지 않는다

            Transform keep = null;
            foreach (var s in streams)
                if (s.name == PreferredTrailStream) { keep = s; break; }
            if (keep == null) keep = streams[0]; // 지정 스트림이 없으면 첫 번째를 남긴다

            foreach (var s in streams)
                if (s != keep) s.gameObject.SetActive(false);

            // 남긴 스트림 안에도 곁줄기가 하나 더 있다: FxTrail은 주 몸체(Flame 등)와
            // 보조 광선 리본(Fx_Light_Trail)을 함께 뿜어 '큰 줄기 + 작은 줄기'로 갈라져 보인다.
            // 보조 리본을 꺼서 완전히 한 줄기로 만든다(주 몸체만 남긴다).
            foreach (Transform child in keep)
                if (child.name.StartsWith("Fx_Light_Trail"))
                    child.gameObject.SetActive(false);
        }

        // 명중·충돌 지점에 폭발 문양을 재생하고 자동 소멸한다.
        public static void SpawnPatternExplosion(GameObject prefab, Vector3 position, float worldDiameter, Color? tint = null)
        {
            if (prefab == null) return;
            var fx = Object.Instantiate(prefab);
            fx.name = "PatternExplosion";
            fx.transform.position = position;
            fx.transform.localRotation = Quaternion.identity;
            // 폭발 원본이 크므로(±4.8 규모) worldDiameter 기준으로 축소 [가정 — 원본 대략 4m]
            fx.transform.localScale = Vector3.one * (worldDiameter / 4f);
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            Object.Destroy(fx, 3f); // [가정] 폭발 잔류 상한
        }

        // 바닥 문양(Bottom)을 명중 지점에 잠깐 각인한다(폭발과 함께). 지름은 월드 기준.
        // faceCamera=true면 바닥에 눕히지 않고 세워서 항상 카메라를 향하게 한다(폭발용 — 플레이어 시점에서 원이 잘 보이게).
        //   false면 XZ 평면에 눕힌다(광역 파동·돔 등 바닥 진 기본).
        // alpha<1이면 문양 전체를 그만큼 투명하게(1=원본 그대로).
        public static void SpawnGroundStamp(GameObject bottomPrefab, Vector3 position, float worldDiameter, Color? tint = null, bool faceCamera = false, float alpha = 1f)
        {
            if (bottomPrefab == null) return;
            var fx = Object.Instantiate(bottomPrefab);
            fx.name = "GroundStamp";
            // Bottom 원본 지름을 실측해 목표 지름으로 정규화
            float src = MeasureDiameter(fx);
            fx.transform.localScale = Vector3.one * (worldDiameter / Mathf.Max(0.5f, src));
            if (faceCamera)
            {
                // 명중 지점에 두고, 원반 면(+Y 노멀)이 카메라를 향하도록 매 프레임 회전.
                fx.transform.position = position;
                fx.AddComponent<PatternFacingBillboard>();
            }
            else
            {
                fx.transform.position = position + Vector3.up * 0.05f; // 지면 살짝 위(z파이팅 방지)
                fx.transform.localRotation = Quaternion.identity; // Bottom은 XZ 평면(바닥)이 기본
            }
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            if (alpha < 1f) MultiplyAlpha(fx, Mathf.Clamp01(alpha)); // 살짝 투명하게(진법이 너무 진하지 않게)
            Object.Destroy(fx, 2.5f); // [가정] 각인 잔류 상한
        }

        // 부드러운 원형 글로우 텍스처 — InkBlobMaterial(먹 얼룩)이 재사용한다.
        private static Texture2D _glowTex;

        // 부드러운 원형 글로우 — 중심이 밝고 가장자리로 갈수록 투명해지는 방사형 falloff.
        private static Texture2D MakeSoftGlowTexture()
        {
            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 c = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
            float maxD = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / maxD; // 0=중심,1=가장자리
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a; // 부드럽게 감쇠(중심에 집중된 빛)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }

        // 파티클·렌더러 바운드로 대략적 지름을 잰다(스케일 1 기준).
        private static float MeasureDiameter(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return 1f;
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return Mathf.Max(b.size.x, b.size.z);
        }

        // 수묵담채 톤 — 담채(옅은 색)라 채도를 크게 낮추고, 명도도 눌러 네온기를 뺀다. [가정]
        // 진 이펙트 전역 룩 노브: 값이 작을수록 먹빛에 가까워진다.
        // 가산 혼합(Additive) 문양은 밝은 배경에서 흰색으로 날아가므로 명도를 특히 낮게 잡는다.
        private const float InkSaturation = 0.7f;   // [테스트] 채도 상향 — 오방색 잘 보이게(수묵담채는 나중 결정)
        private const float InkValueScale = 0.85f;  // [테스트] 명도 상향

        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId2 = Shader.PropertyToID("_BaseColor");

        // 문양의 파티클 startColor + 렌더러 머티리얼 색을 오방색 담채로 물들인다.
        // 흰 문양 선(Pattern 렌더러의 _Color=흰색/HDR)까지 눌러야 네온기가 빠진다.
        private static void ApplyTint(GameObject fx, Color tint)
        {
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                var sc = main.startColor;
                if (sc.mode == ParticleSystemGradientMode.Color)
                    main.startColor = InkTint(sc.color, tint);
                else if (sc.mode == ParticleSystemGradientMode.TwoColors)
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        InkTint(sc.colorMin, tint), InkTint(sc.colorMax, tint));
                else
                    main.startColor = InkTint(Color.white, tint);
            }

            // 렌더러 머티리얼 색(_Color/_TintColor/_BaseColor) — 흰 선·HDR 밝기를 담채로 억제.
            // 인스턴스 머티리얼로 복제해 원본 에셋을 오염시키지 않는다.
            foreach (var r in fx.GetComponentsInChildren<Renderer>(true))
            {
                var mat = r.material; // 인스턴스화
                RuntimeMaterialCleaner.Track(r.gameObject, mat);
                foreach (var id in new[] { ColorId, TintColorId, BaseColorId2 })
                {
                    if (!mat.HasProperty(id)) continue;
                    Color c = mat.GetColor(id);
                    mat.SetColor(id, InkTint(c, tint));
                }
            }
        }

        // 문양 전체를 factor만큼 투명하게(파티클 startColor 알파 + 렌더러 머티리얼 알파를 함께 낮춘다).
        // 진법이 너무 진할 때 살짝 비쳐 보이게 하는 용도.
        private static void MultiplyAlpha(GameObject fx, float factor)
        {
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                var sc = main.startColor;
                if (sc.mode == ParticleSystemGradientMode.Color)
                { var c = sc.color; c.a *= factor; main.startColor = c; }
                else if (sc.mode == ParticleSystemGradientMode.TwoColors)
                {
                    Color cmin = sc.colorMin, cmax = sc.colorMax;
                    cmin.a *= factor; cmax.a *= factor;
                    main.startColor = new ParticleSystem.MinMaxGradient(cmin, cmax);
                }
            }
            foreach (var r in fx.GetComponentsInChildren<Renderer>(true))
            {
                var mat = r.material; // 인스턴스화(원본 에셋 보호)
                RuntimeMaterialCleaner.Track(r.gameObject, mat);
                foreach (var id in new[] { ColorId, TintColorId, BaseColorId2 })
                {
                    if (!mat.HasProperty(id)) continue;
                    Color c = mat.GetColor(id);
                    c.a *= factor;
                    mat.SetColor(id, c);
                }
            }
        }

        // 원래 명도를 기준 삼되 눌러서, tint의 색상을 옅은 담채로 얹는다(수묵담채).
        // HDR 밝기(>1)는 1로 클램프해 네온 발광을 억제하고, 채도는 담채 상한으로 낮춘다.
        private static Color InkTint(Color original, Color tint)
        {
            float rawValue = Mathf.Max(original.r, Mathf.Max(original.g, original.b));
            float value = Mathf.Clamp01(rawValue) * InkValueScale;
            Color.RGBToHSV(tint, out float h, out float s, out _);
            Color tinted = Color.HSVToRGB(h, Mathf.Min(s, InkSaturation), value);
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

        // 적 사망 수묵 연출 — 먹이 확 번졌다 스며 사라지는 느낌(화염 폭발 대체).
        //   먹방울들이 사방으로 튀며 번지고(알파 블렌드), 위로 옅은 먹구름이 피어오른다. 오행부 먹 미학.
        public static void SpawnInkDeathBurst(Vector3 position, float scale = 1f)
        {
            var ink = new Color(0.09f, 0.08f, 0.075f);      // 먹빛(살짝 따뜻한 흑)
            var go = new GameObject("InkDeathBurst");
            go.transform.position = position + Vector3.up * 0.9f * scale; // 몸통 높이쯤에서 번짐

            // 튀는 먹방울 — 사방으로 번지며 감속해 얼룩처럼 멈춘다
            var splat = go.AddComponent<ParticleSystem>();
            ConfigureInk(splat, scale, ink, rising: false);

            // 피어오르는 먹구름 — 위로 옅게 번져 오른다
            var cloudGo = new GameObject("InkRise");
            cloudGo.transform.SetParent(go.transform, false);
            var cloud = cloudGo.AddComponent<ParticleSystem>();
            ConfigureInk(cloud, scale, ink, rising: true);

            splat.Play();
            cloud.Play();
            Object.Destroy(go, 2.2f);
        }

        private static void ConfigureInk(ParticleSystem ps, float scale, Color ink, bool rising)
        {
            ps.Stop();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(rising ? 1.0f : 0.6f, rising ? 1.6f : 1.0f);
            main.startSpeed = rising
                ? new ParticleSystem.MinMaxCurve(0.6f * scale, 1.4f * scale)
                : new ParticleSystem.MinMaxCurve(2.5f * scale, 5f * scale);
            main.startSize = rising
                ? new ParticleSystem.MinMaxCurve(0.7f * scale, 1.3f * scale)
                : new ParticleSystem.MinMaxCurve(0.35f * scale, 0.75f * scale);
            main.startColor = Color.white; // 실제 색은 gradient로(제곱 방지)
            main.gravityModifier = rising ? -0.03f : 0.06f; // 구름은 살짝 뜨고, 먹방울은 살짝 처짐
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, rising ? (short)6 : (short)16) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f * scale;

            var damp = ps.limitVelocityOverLifetime;
            damp.enabled = true;
            damp.dampen = rising ? 0.3f : 0.7f; // 먹방울은 강하게 감속해 얼룩처럼 멈춤

            // 번짐: 등장하며 커졌다 유지
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, rising ? 0.4f : 0.6f);
            sizeCurve.AddKey(0.4f, 1f);
            sizeCurve.AddKey(1f, rising ? 1.2f : 0.85f); // 구름은 계속 번져 커지고, 방울은 살짝 줄며 스밈
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(ink, 0f), new GradientColorKey(ink, 1f) },
                new[] { new GradientAlphaKey(rising ? 0.5f : 0.85f, 0f), new GradientAlphaKey(rising ? 0.4f : 0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = InkBlobMaterial(); // 부드러운 원형 먹 얼룩(알파 블렌드)
        }

        // 먹 얼룩용 소프트 원형 머티리얼 — 알파 블렌드(가산 아님, 배경을 덮는 먹).
        private static Material _inkBlobMat;
        private static Material InkBlobMaterial()
        {
            if (_inkBlobMat == null)
            {
                if (_glowTex == null) _glowTex = MakeSoftGlowTexture(); // 소프트 원형 재사용
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                _inkBlobMat = new Material(shader);
                _inkBlobMat.SetTexture("_BaseMap", _glowTex);
                _inkBlobMat.SetFloat("_Surface", 1f);
                _inkBlobMat.SetFloat("_Blend", 0f);
                _inkBlobMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _inkBlobMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _inkBlobMat.SetFloat("_ZWrite", 0f);
                _inkBlobMat.renderQueue = 3000;
            }
            return _inkBlobMat;
        }
    }

    // 런타임 인스턴스 자원(머티리얼 등)을 GameObject 파괴 시 함께 해제한다.
    // renderer.material 인스턴스는 오브젝트와 함께 자동 파괴되지 않아, 시전이 반복될수록
    // 세션 메모리에 누적되는 누수가 된다 — 인스턴스를 만든 쪽이 Track으로 등록해 둔다.
    // (이미 스스로 Destroy하는 소유자와 겹쳐도 Unity Destroy는 멱등이라 안전.)
    public sealed class RuntimeMaterialCleaner : MonoBehaviour
    {
        private readonly List<Object> _owned = new List<Object>(4);

        public static void Track(GameObject owner, Object resource)
        {
            if (owner == null || resource == null) return;
            var cleaner = owner.GetComponent<RuntimeMaterialCleaner>();
            if (cleaner == null) cleaner = owner.AddComponent<RuntimeMaterialCleaner>();
            if (!cleaner._owned.Contains(resource)) cleaner._owned.Add(resource);
        }

        private void OnDestroy()
        {
            foreach (var owned in _owned)
                if (owned != null) Destroy(owned);
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

    // 바닥 진(Bottom, XZ 평면에 누운 원반)을 세워서 항상 카메라를 향하게 한다.
    // 원반의 면 노멀은 +Y이므로, 그 +Y가 카메라 쪽을 보도록 최소 회전시킨다(원형 문양이라 롤은 무관).
    public sealed class PatternFacingBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toCam = cam.transform.position - transform.position;
            if (toCam.sqrMagnitude < 1e-6f) return;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, toCam.normalized);
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

    // 문양 진의 부드러운 시작·종료 — 판정 렌더러를 숨긴 문양은 색 페이드를 못 받으므로 별도 처리.
    // 시작: 스케일이 살짝 부풀며 등장(펼쳐지는 진). 종료: 방출을 끊어 입자가 자연 소멸(뚝 끊김 방지).
    public sealed class PatternFade : MonoBehaviour
    {
        private ParticleSystem[] _systems;
        private float _bornAt;
        private float _fadeOutAt = -1f;  // 이 시각부터 방출 중단
        private float _destroyAt = -1f;
        private const float FadeInSeconds = 0.35f; // 펼쳐지는 등장
        private Vector3 _targetScale;

        // lifeSeconds: 총 지속(0 이하면 무한 — 외부가 Destroy). tailSeconds: 방출 끊고 잔류 소멸 여유.
        public void Init(float lifeSeconds, float tailSeconds = 1.2f)
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _bornAt = Time.time;
            _targetScale = transform.localScale;
            if (lifeSeconds > 0f)
            {
                _fadeOutAt = _bornAt + Mathf.Max(0.1f, lifeSeconds - tailSeconds);
                _destroyAt = _bornAt + lifeSeconds;
            }
        }

        private void Update()
        {
            // 등장: 스케일 0.85→1.0 스무스
            float since = Time.time - _bornAt;
            if (since < FadeInSeconds)
            {
                float t = Mathf.SmoothStep(0f, 1f, since / FadeInSeconds);
                transform.localScale = _targetScale * Mathf.Lerp(0.85f, 1f, t);
            }
            else if (transform.localScale != _targetScale)
            {
                transform.localScale = _targetScale;
            }

            // 종료: 방출 중단 → 잔류 입자 자연 소멸
            if (_fadeOutAt > 0f && Time.time >= _fadeOutAt)
            {
                foreach (var ps in _systems)
                {
                    if (ps == null) continue;
                    var em = ps.emission;
                    if (em.enabled) em.enabled = false;
                }
            }
            if (_destroyAt > 0f && Time.time >= _destroyAt) Destroy(gameObject);
        }

        // 외부(방어막 소멸 등)가 즉시 페이드아웃을 걸 때
        public void BeginFadeOut(float tailSeconds = 1.2f)
        {
            _fadeOutAt = Time.time;
            _destroyAt = Time.time + tailSeconds;
        }
    }
}
