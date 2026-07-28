// 임시 풀스크린 먹 포스트 (에셋 도입 전 스탠드인) — Snapshot Shaders Pro 구매 시 교체 예정.
// URP 내장 Full Screen Pass Renderer Feature에 얹는다 (Render Graph 네이티브 호환).
// 구성: 뎁스 기반 외곽선(먹색) + 한지 세피아 톤 + 정적 그레인(종이 섬유) + 옅은 비네트.
Shader "MandateOfInk/Temp/InkPost"
{
    Properties
    {
        _PaperColor("Paper Color", Color) = (0.968, 0.945, 0.894, 1)   // #F7F1E4
        _SepiaStrength("Sepia Strength", Range(0, 1)) = 0.35
        _InkColor("Outline Ink Color", Color) = (0.165, 0.149, 0.133, 1) // #2A2622
        _OutlineStrength("Outline Strength", Range(0, 1)) = 0.85
        _OutlineThreshold("Outline Depth Threshold", Range(0.1, 8)) = 1.2
        _GrainStrength("Grain Strength", Range(0, 0.5)) = 0.10
        _GrainScale("Grain Scale", Range(0.5, 8)) = 2.5
        _VignetteStrength("Vignette Strength", Range(0, 1)) = 0.25

        // 원경 여백(인왕제색도 레퍼런스, 사용자 결정 2026-07-24) — 멀수록 한지로 녹아든다.
        // 하늘(무한 깊이·밝음)은 자동으로 완전한 종이 여백이 되고, 어두운 먹 덩어리는
        // _SilhouetteKeep 비율만큼 남아 「형태는 뭉개져도 보이는」 원산 실루엣이 된다(사용자 피드백 2026-07-27).
        _PaperFadeStart("Paper Fade Start (m)", Range(1, 200)) = 50
        _PaperFadeEnd("Paper Fade End (m)", Range(5, 500)) = 160
        _PaperFadeStrength("Paper Fade Strength", Range(0, 1)) = 0.9
        _SilhouetteKeep("Distant Silhouette Keep (Dark Ink Stays)", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "InkPost"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 _PaperColor;
            half _SepiaStrength;
            half4 _InkColor;
            half _OutlineStrength;
            half _OutlineThreshold;
            half _GrainStrength;
            half _GrainScale;
            half _VignetteStrength;
            half _PaperFadeStart;
            half _PaperFadeEnd;
            half _PaperFadeStrength;
            half _SilhouetteKeep;

            float LinearDepthAt(float2 uv)
            {
                float raw = SampleSceneDepth(uv);
                return LinearEyeDepth(raw, _ZBufferParams);
            }

            // 정적 해시 노이즈 — 시간항이 없어 필름이 아니라 「종이 섬유」로 읽힌다
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // 1) 뎁스 소벨 외곽선 — 깊이가 급변하는 곳에 먹선을 얹는다
                float2 texel = _ScreenSize.zw; // 1/width, 1/height
                float dC = LinearDepthAt(uv);
                float dL = LinearDepthAt(uv - float2(texel.x, 0));
                float dR = LinearDepthAt(uv + float2(texel.x, 0));
                float dD = LinearDepthAt(uv - float2(0, texel.y));
                float dU = LinearDepthAt(uv + float2(0, texel.y));
                float grad = abs(dL - dR) + abs(dD - dU);
                // 먼 물체일수록 깊이 차가 커지므로 거리로 정규화해 선 굵기를 고르게
                float edge = smoothstep(_OutlineThreshold * 0.5, _OutlineThreshold, grad / max(dC * 0.08, 0.02));
                color = lerp(color, _InkColor.rgb, edge * _OutlineStrength);

                // 1.5) 원경 여백 — 멀수록 한지로 녹아든다(인왕제색도의 안개·하늘 여백).
                //      외곽선까지 계산한 뒤에 적용해 원경의 먹선도 함께 옅어진다(원산의 흐릿한 윤곽).
                //      밝은 픽셀(하늘·밝은 면)은 종이로 완전히 사라지지만, 어두운 픽셀(먹 덩어리)은
                //      _SilhouetteKeep 비율만큼 덜 사라져 씻긴 실루엣으로 남는다(사용자 피드백 2026-07-27).
                float fade = smoothstep(_PaperFadeStart, _PaperFadeEnd, dC);
                half preLuma = dot(color, half3(0.299, 0.587, 0.114));
                half darkness = saturate(1.0h - preLuma * 1.4h); // 밝음=0, 진한 먹=1에 가깝게
                half fadeAmount = fade * _PaperFadeStrength * (1.0h - _SilhouetteKeep * darkness);
                color = lerp(color, _PaperColor.rgb, fadeAmount);

                // 2) 한지 세피아 톤 — 명도를 종이색으로 물들인다
                half luma = dot(color, half3(0.299, 0.587, 0.114));
                half3 toned = _PaperColor.rgb * luma;
                color = lerp(color, toned, _SepiaStrength);

                // 3) 정적 그레인 — 종이 섬유
                float grain = Hash(floor(uv * _ScreenSize.xy / _GrainScale));
                color *= 1.0 - _GrainStrength * (grain - 0.5);

                // 4) 옅은 비네트
                float d = distance(uv, float2(0.5, 0.5));
                color *= 1.0 - _VignetteStrength * smoothstep(0.42, 0.78, d);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
