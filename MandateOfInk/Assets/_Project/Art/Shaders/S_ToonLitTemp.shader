// 임시 셀 셰이딩 (에셋 도입 전 스탠드인) — Flat Kit 구매 시 교체 예정.
// URP Lit과 프로퍼티 이름을 맞춰(_BaseMap/_BaseColor) 텍스처·MPB 틴트가 그대로 동작한다.
// 수묵 몰골 명암(사용자 레퍼런스 기반, 2026-07-24): Dissolvable Ink Painting Effects류 에셋의
// "돌 표면" 느낌을 근사 — 셀 경계(threshold)를 매끈한 등고선이 아니라 월드 좌표 기반 프랙탈
// 노이즈로 일그러뜨려 붓터치처럼 보이게 하고, 가장 밝은 구간은 거의 순백으로 밀어붙여
// 먹이 마른 하이라이트(비백)처럼 날리게 한다.
Shader "MandateOfInk/Temp/ToonLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1
        _CelSteps("Cel Steps", Range(2, 5)) = 3
        _ShadowTint("Shadow Tint", Color) = (0.62, 0.58, 0.52, 1)
        _AmbientBoost("Ambient Boost", Range(0, 1)) = 0.35

        _InkCelNoiseScale("Ink Cel Noise Scale (world units)", Range(0.2, 10)) = 2.2
        _InkCelNoiseStrength("Ink Cel Noise Strength (Edge Wobble)", Range(0, 1)) = 0.35
        _InkCelSoftness("Ink Cel Softness (Brush Band)", Range(0, 1)) = 0.3
        _InkHighlightBoost("Ink Highlight Boost (Dry Brush Whiteout)", Range(0, 1)) = 0.5

        // 먹 실체화(사용자 결정 2026-07-27) — 필드 설치물이 먹물로 생겨나 실체로 굳는 연출.
        // 0=온통 먹물, 1=완전 실체(기본 — 기존 머티리얼 무영향). InkMaterializer가 MPB로 굴린다.
        _MaterializeProgress("Materialize Progress (0=Ink, 1=Solid)", Range(0, 1)) = 1
        _MaterializeInkColor("Materialize Ink Color", Color) = (0.1, 0.09, 0.08, 1)
        _MaterializeNoiseScale("Materialize Noise Scale (world units)", Range(0.2, 10)) = 1.5
        // 실패 침식 — 지정 위치 밖 시전 시 먹물이 실체화되지 못하고 흩어져 사라진다(0=온전, 1=소멸)
        _MaterializeErode("Materialize Erode (Failure Dissolve)", Range(0, 1)) = 0

        // 환경(나무/풀/건물)=0 먹-종이 톤 램프, 캐릭터/이펙트=1 채색 — 아트 컨셉 구분(사용자 결정 2026-07-24).
        // 인왕제색도 레퍼런스: 단순 흑백(회색)이 아니라 「밝음=칠하지 않은 한지(여백), 어두움=먹」으로
        // 다시 매핑한다. _PaperPoint 이상 밝기는 순수 종이색으로 눌러 넓은 여백을 만든다.
        _Saturation("Saturation (0=Ink Wash Environment, 1=Full Color)", Range(0, 1)) = 1
        _PaperToneColor("Paper Tone (Blank Hanji)", Color) = (0.93, 0.89, 0.80, 1)
        _InkToneColor("Ink Tone (Darkest Ink)", Color) = (0.13, 0.12, 0.11, 1)
        _PaperPoint("Paper Point (Luma -> Blank Paper)", Range(0.3, 1.2)) = 0.68
        _InkPoint("Ink Point (Luma -> Full Ink)", Range(0, 0.3)) = 0.04
        _TintRetain("Tint Retain (Faint Wash Color)", Range(0, 0.5)) = 0.12

        // 잉크 코팅 — 환경 오브젝트가 먹/잉크에 적셔진 듯 보이게(밴디류 룩, 사용자 결정 2026-07-24).
        // 기본값 전부 0 = 효과 없음: 이 셰이더를 같이 쓰는 캐릭터/적 머티리얼에 영향이 없도록 한다.
        // 환경 머티리얼 일괄 적용은 에디터 메뉴(MandateOfInk/잉크 코팅 적용)로.
        _InkCoatColor("Ink Coat Color", Color) = (0.09, 0.08, 0.07, 1)
        _InkCoatBase("Ink Coat Base (Overall Soak)", Range(0, 1)) = 0
        _InkCoatTop("Ink Coat Top (Poured From Above)", Range(0, 1)) = 0
        _InkDripAmount("Ink Drip Amount (Vertical Streaks)", Range(0, 1)) = 0
        _InkDripScale("Ink Drip Scale (world units)", Range(0.2, 10)) = 1.5
        _InkGlossStrength("Ink Wet Gloss Strength", Range(0, 2)) = 0
        _InkGlossSharpness("Ink Wet Gloss Sharpness", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 tangentWS : TEXCOORD1; // xyz=tangent, w=bitangent 부호
                float2 uv : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _CelSteps;
                half4 _ShadowTint;
                half _AmbientBoost;
                half _InkCelNoiseScale;
                half _InkCelNoiseStrength;
                half _InkCelSoftness;
                half _InkHighlightBoost;
                half _MaterializeProgress;
                half4 _MaterializeInkColor;
                half _MaterializeNoiseScale;
                half _MaterializeErode;
                half _Saturation;
                half4 _PaperToneColor;
                half4 _InkToneColor;
                half _PaperPoint;
                half _InkPoint;
                half _TintRetain;
                half4 _InkCoatColor;
                half _InkCoatBase;
                half _InkCoatTop;
                half _InkDripAmount;
                half _InkDripScale;
                half _InkGlossStrength;
                half _InkGlossSharpness;
            CBUFFER_END

            // 정적 값 노이즈 + 2옥타브 Fbm — 시간항 없음(먹은 마르면 고정된다), 월드 좌표 기반이라
            // 오브젝트가 회전해도 표면에 들러붙은 붓자국처럼 고정되어 보인다(UV 왜곡 문제 회피).
            float Hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash3(i + float3(0, 0, 0));
                float n100 = Hash3(i + float3(1, 0, 0));
                float n010 = Hash3(i + float3(0, 1, 0));
                float n110 = Hash3(i + float3(1, 1, 0));
                float n001 = Hash3(i + float3(0, 0, 1));
                float n101 = Hash3(i + float3(1, 0, 1));
                float n011 = Hash3(i + float3(0, 1, 1));
                float n111 = Hash3(i + float3(1, 1, 1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float InkFbm3(float3 p)
            {
                float sum = ValueNoise3(p) * 0.65;
                sum += ValueNoise3(p * 2.13 + 11.7) * 0.35;
                return sum; // 대략 0~1
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.tangentWS = float4(tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 실패 침식 — 노이즈를 따라 픽셀을 버려 먹이 흩어져 사라지는 연출(값이 오를수록 소멸)
                if (_MaterializeErode > 0.001h)
                {
                    float eNoise = InkFbm3(i.positionWS * _MaterializeNoiseScale + 3.3);
                    clip((half)eNoise - _MaterializeErode * 1.1h);
                }

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                // 노멀맵 — 정접 공간 샘플링(URP UnpackNormalScale 표준 경로), 셀 경계 계산에 표면 디테일 반영
                float3 geomNormalWS = normalize(i.normalWS);
                float3 tangentWS = normalize(i.tangentWS.xyz);
                float3 bitangentWS = cross(geomNormalWS, tangentWS) * i.tangentWS.w;
                half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv);
                half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);
                float3 n = normalize(
                    tangentWS * normalTS.x + bitangentWS * normalTS.y + geomNormalWS * normalTS.z);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light light = GetMainLight(shadowCoord);
                half ndl = saturate(dot(n, light.direction)) * light.shadowAttenuation;

                // 몰골 경계 노이즈 — 표면에 들러붙은 붓자국처럼 월드 좌표 기반 Fbm으로 명암 경계를 흔든다.
                float noise = InkFbm3(i.positionWS * _InkCelNoiseScale); // 0~1
                half wobble = (half)(noise - 0.5) * _InkCelNoiseStrength;
                half ndlWobbled = saturate(ndl + wobble);

                // 셀 양자화: 연속 음영을 _CelSteps 단계로 끊되, 각 계단 경계를 soft-band로 풀어
                // 칼같은 계단 대신 붓이 스친 그러데이션으로 만든다(_InkCelSoftness가 클수록 폭 넓음).
                half steps = max(_CelSteps, 2.0h);
                half scaled = min(ndlWobbled, 0.999h) * steps;
                half stepIndex = floor(scaled);
                half stepFrac = scaled - stepIndex;
                // 다음 계단으로 넘어가는 구간만 soft-band로 풀어 붓이 스친 그러데이션을 만든다.
                half softEdge = smoothstep(1.0h - max(_InkCelSoftness, 0.001h), 1.0h, stepFrac);
                half cel = saturate((stepIndex + softEdge) / (steps - 1.0h));

                // 가장 밝은 계단은 순백에 더 가깝게 밀어붙여 마른 붓의 하이라이트(비백)처럼 날린다.
                half topStepMask = saturate((scaled - (steps - 1.0h)) + 1.0h) * step(steps - 1.5h, scaled);
                cel = lerp(cel, 1.0h, topStepMask * _InkHighlightBoost);

                half3 shading = lerp(_ShadowTint.rgb, half3(1, 1, 1), cel) * light.color.rgb;
                half3 ambient = SampleSH(n) * _AmbientBoost;
                half3 color = albedo.rgb * (shading + ambient);

                // 잉크 코팅(밴디류 룩, 사용자 결정 2026-07-24) — 세 겹 마스크를 합쳐 잉크색으로 덮는다:
                //   base = 전체 착색(잉크에 담갔다 뺀 정도) / top = 윗면일수록 진하게(위에서 부은 잉크)
                //   drip = 옆면의 세로 줄무늬(흘러내린 자국 — xz만 쓰는 노이즈라 y방향으로 이어지고,
                //          y를 낮은 배율로 섞은 컷 노이즈를 곱해 줄기가 중간중간 끊긴다)
                half coat = 0.0h;
                if (_InkCoatBase + _InkCoatTop + _InkDripAmount > 0.001h)
                {
                    half up = saturate(n.y);
                    float dripStreak = ValueNoise3(float3(i.positionWS.x, 0, i.positionWS.z) * _InkDripScale);
                    float dripCut = ValueNoise3(i.positionWS * float3(_InkDripScale, _InkDripScale * 0.35, _InkDripScale) + 53.1);
                    half drip = smoothstep(1.0h - max(_InkDripAmount, 0.001h), 1.0h, (half)dripStreak)
                              * smoothstep(0.3h, 0.6h, (half)dripCut) * (1.0h - up);
                    coat = saturate(_InkCoatBase + up * _InkCoatTop + drip);
                    color = lerp(color, _InkCoatColor.rgb, coat);
                }

                // 아트 컨셉(사용자 결정 2026-07-24, 인왕제색도 레퍼런스): 환경(_Saturation=0)은
                // 「먹-종이 톤 램프」 — 밝은 곳은 칠하지 않은 한지(여백)로 비우고, 어두운 곳만 먹이 쌓인다.
                // _PaperPoint 이상 밝기는 순수 종이색(평평한 여백), _TintRetain으로 옅은 담채 기운만 남긴다.
                // 캐릭터·이펙트(_Saturation=1)는 원색 그대로.
                half luma = dot(color, half3(0.299, 0.587, 0.114));
                half tone = smoothstep(_InkPoint, _PaperPoint, luma);
                half3 inkWash = lerp(_InkToneColor.rgb, _PaperToneColor.rgb, tone);
                inkWash = lerp(inkWash, color, _TintRetain);
                color = lerp(inkWash, color, _Saturation);

                // 먹 실체화(사용자 결정 2026-07-27) — 진행도가 낮으면 먹물, 노이즈 얼룩을 따라
                // 먹이 마르듯 실체가 차오른다. 경계 부근은 먹빛으로 살짝 물들어 번짐이 남는다.
                if (_MaterializeProgress < 0.999h)
                {
                    float mNoise = InkFbm3(i.positionWS * _MaterializeNoiseScale + 7.7);
                    half sweep = _MaterializeProgress * 1.3h - 0.15h; // 0->모두 먹, 1->모두 실체가 되도록 범위 보정
                    half solid = smoothstep((half)mNoise - 0.08h, (half)mNoise + 0.08h, sweep);
                    half edgeBand = smoothstep(0.0h, 0.3h, solid) - smoothstep(0.7h, 1.0h, solid);
                    color = lerp(_MaterializeInkColor.rgb, color, solid);
                    color = lerp(color, _MaterializeInkColor.rgb, edgeBand * 0.35h);
                }

                // 젖은 잉크 광택 — 잉크가 덮인 부위만 번들거린다(마르지 않은 잉크의 핵심 인상).
                // 하이라이트는 smoothstep으로 뚝 끊어 셀 룩과 결을 맞춘다.
                if (_InkGlossStrength > 0.001h && coat > 0.001h)
                {
                    float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                    float3 halfDir = normalize(light.direction + viewDir);
                    half spec = pow(saturate(dot(n, halfDir)), lerp(16.0h, 128.0h, _InkGlossSharpness));
                    spec = smoothstep(0.4h, 0.5h, spec);
                    color += spec * coat * _InkGlossStrength * light.color.rgb;
                }

                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float4 positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionHCS.z = min(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionHCS.z = max(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionHCS = positionHCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
