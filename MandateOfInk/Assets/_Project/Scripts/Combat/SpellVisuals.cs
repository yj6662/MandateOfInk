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
            NormalizePatternScale(fx.transform, parent, worldDiameter);
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            return fx;
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

        // 바닥 문양(Bottom)을 명중 지점 바닥에 눕혀 잠깐 각인한다(폭발과 함께). 지름은 월드 기준.
        public static void SpawnGroundStamp(GameObject bottomPrefab, Vector3 position, float worldDiameter, Color? tint = null)
        {
            if (bottomPrefab == null) return;
            var fx = Object.Instantiate(bottomPrefab);
            fx.name = "GroundStamp";
            fx.transform.position = position + Vector3.up * 0.05f; // 지면 살짝 위(z파이팅 방지)
            fx.transform.localRotation = Quaternion.identity; // Bottom은 XZ 평면(바닥)이 기본
            // Bottom 원본 지름을 실측해 목표 지름으로 정규화
            float src = MeasureDiameter(fx);
            fx.transform.localScale = Vector3.one * (worldDiameter / Mathf.Max(0.5f, src));
            if (tint.HasValue) ApplyTint(fx, tint.Value);
            Object.Destroy(fx, 2.5f); // [가정] 각인 잔류 상한
        }

        // [임시] 목(木) 나뭇가지 폭발 — 갈색 주가지가 뻗고, 그 끝에서 잔가지가 갈라져 나오며, 끝갈래는 초록(새순).
        // 정식 에셋/생성 API로 교체 예정. 2단 구조를 Sub Emitter(주가지 소멸 시 잔가지 방출)로 만든다.
        // tint(오방색)는 여기서 무시 — 목은 갈색→초록 고정 색이 정체성이라 [가정].
        public static void SpawnBranchBurst(Vector3 position, float worldDiameter, Color tint)
        {
            var bark = new Color(0.55f, 0.38f, 0.2f);    // 나뭇가지 갈색(밝게 — 배경 대비)
            var sprout = new Color(0.45f, 0.9f, 0.35f);  // 새순 초록(선명하게)

            var go = new GameObject("BranchBurst");
            go.transform.position = position + Vector3.up * 0.1f;

            // 잔가지(Sub Emitter) — 주가지 파티클이 죽는 지점에서 갈라져 나온다
            var twigGo = new GameObject("Twigs");
            twigGo.transform.SetParent(go.transform, false);
            var twig = twigGo.AddComponent<ParticleSystem>();
            ConfigureBranch(twig, worldDiameter * 0.55f, 0.35f, 0.55f, bark, sprout, burstCount: 2, isSub: true);

            // 주가지 — 크게 뻗도록 reach 확대
            var ps = go.AddComponent<ParticleSystem>();
            ConfigureBranch(ps, worldDiameter * 1.8f, 0.5f, 0.8f, bark, sprout, burstCount: 12, isSub: false);
            // Sub Emitter 연결 — 주가지가 죽을 때(Death) 잔가지 방출
            var sub = ps.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(twig, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritColor);

            ps.Play();
            Object.Destroy(go, 1.6f);
        }

        // 가지 파티클 하나를 구성 — stretched billboard로 뻗고, 끝에서 초록으로 물든다.
        private static void ConfigureBranch(ParticleSystem ps, float reach, float lifeMin, float lifeMax,
            Color bark, Color sprout, int burstCount, bool isSub)
        {
            ps.Stop();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(reach * 3f, reach * 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(reach * 0.05f, reach * 0.1f);
            main.startColor = bark;
            main.gravityModifier = isSub ? 0.02f : 0f; // 잔가지는 살짝 처짐
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            if (!isSub) // 주가지만 자체 버스트, 잔가지는 Sub Emitter가 트리거
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount, (short)(burstCount + 6)) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = isSub ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Sphere;
            if (isSub) { shape.angle = 55f; shape.radius = 0.01f; } // 잔가지는 부모 방향 원뿔로 갈라짐
            else shape.radius = reach * 0.06f;

            var damp = ps.limitVelocityOverLifetime;
            damp.enabled = true;
            damp.dampen = 0.45f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.32f;
            renderer.lengthScale = 4.5f;
            renderer.material = BranchMaterial();

            // 굵기: 밑동 굵고 끝 뾰족 (뾰족 텍스처 + 끝 가늘어짐)
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.4f);
            sizeCurve.AddKey(0.35f, 1f);
            sizeCurve.AddKey(1f, 0.05f); // 끝 뾰족
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // 색: 갈색 밑동 → 끝갈래(수명 끝) 초록 새순
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(bark, 0f), new GradientColorKey(bark, 0.55f), new GradientColorKey(sprout, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
        }

        // 뾰족한 가지 텍스처 — 세로로 긴 다이아몬드(끝이 뾰족한 잎/가지 실루엣)를 코드로 생성.
        private static Material _branchMat;
        private static Texture2D _branchTex;
        private static Material BranchMaterial()
        {
            if (_branchMat == null)
            {
                if (_branchTex == null) _branchTex = MakeSpikeTexture();
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                _branchMat = new Material(shader);
                _branchMat.SetTexture("_BaseMap", _branchTex);
                _branchMat.SetFloat("_Surface", 1f);
                _branchMat.SetFloat("_Blend", 0f);
                _branchMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _branchMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _branchMat.SetFloat("_ZWrite", 0f);
                _branchMat.renderQueue = 3000;
            }
            return _branchMat;
        }

        // 세로 방향으로 뾰족한 실루엣(위 끝이 뾰족, 아래는 넓음) — stretch 방향과 맞물려 가지 끝이 뾰족해진다.
        private static Texture2D MakeSpikeTexture()
        {
            int w = 32, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);       // 0=밑동, 1=끝
                float halfWidth = Mathf.Lerp(0.5f, 0.02f, Mathf.Pow(t, 0.7f)); // 위로 갈수록 뾰족
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((float)x / (w - 1) - 0.5f);
                    float a = dx <= halfWidth ? 1f - Mathf.SmoothStep(halfWidth * 0.6f, halfWidth, dx) : 0f;
                    a *= Mathf.SmoothStep(0f, 0.15f, t); // 밑동 끝 살짝 페이드
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
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
                foreach (var id in new[] { ColorId, TintColorId, BaseColorId2 })
                {
                    if (!mat.HasProperty(id)) continue;
                    Color c = mat.GetColor(id);
                    mat.SetColor(id, InkTint(c, tint));
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
