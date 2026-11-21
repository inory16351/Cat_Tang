// Cat_Tang 툰 셰이더 (URP)
// - 명암 3단: 밝은 면 / 1차 그림자 / 2차 그림자 (경계는 _ShadeSoftness로 딱 끊기게)
// - 림 라이트: 빛 받는 쪽 가장자리에만
// - 외곽선: 뒤집은 겹(inverted hull), 화면 기준 픽셀 굵기 유지
// - 브롤 스타즈풍 (2026-11-17 사용자: 하데스 말고 플랫 · 가시성 높은 그림체 → 기본으로 확정): 전역 _CatTangBrawl = 1 (BrawlStyle가 넣음). 0이면 예전 하데스풍 툰.
//   그림자 1단 · 같은 색상으로 어둡게(보라 그림자 X) · 채도 높은 단색 면 · 굵은 검은 외곽선 · 작고 단단한 림/하이라이트.
//   _BrawlActor = 1 (고양이 · 적, BrawlStyle가 렌더러에 표시) / 0 = 배경: 배경은 한 톤 낮추고 텍스처 무늬를 뭉개 단순하게
Shader "CatTang/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map (손으로 칠한 텍스처)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Saturation ("Saturation (배경 톤 낮추기 < 1)", Range(0, 1.5)) = 1
        _Brightness ("Brightness (배경 톤 낮추기 < 1)", Range(0, 1.5)) = 1
        // 색 범위 치환 (적 배색): 원래 색상 범위(From ± Width)의 덜 선명한 몸 색만 목표 색상으로 돌림. 금속 금빛 · 선명한 진홍 포인트는 유지
        _RecolorAmount ("Recolor Amount", Range(0, 1)) = 0
        _RecolorFrom ("Recolor From Hue", Range(0, 1)) = 0.97
        _RecolorWidth ("Recolor Hue Width", Range(0, 0.5)) = 0.1
        _RecolorHue ("Recolor To Hue", Range(0, 1)) = 0.5
        _RecolorSat ("Recolor Saturation x", Range(0, 2)) = 1
        _RecolorVal ("Recolor Value x", Range(0, 2)) = 1

        [Header(Shade)]
        _ShadeColor ("1st Shade Color", Color) = (0.62, 0.55, 0.72, 1)
        _ShadeThreshold ("1st Shade Threshold", Range(0, 1)) = 0.45
        _SecondShadeColor ("2nd Shade Color", Color) = (0.38, 0.32, 0.5, 1)
        _SecondShadeThreshold ("2nd Shade Threshold", Range(0, 1)) = 0.12
        _ShadeSoftness ("Shade Softness", Range(0.001, 0.2)) = 0.015
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.25

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 0.78, 0.35, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.55
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.8

        [Header(Ink and Hatching)]
        _InkColor ("Ink Color", Color) = (0.12, 0.07, 0.14, 1)
        _InkWidth ("Terminator Ink Width", Range(0, 0.2)) = 0.0
        _InkStrength ("Terminator Ink Strength", Range(0, 1)) = 0.0
        _HatchStrength ("Hatch Strength", Range(0, 1)) = 0.0
        _HatchDensity ("Hatch Density", Range(1, 80)) = 28
        _HatchWidth ("Hatch Line Width", Range(0.05, 0.6)) = 0.28

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.08, 0.05, 0.08, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 10)) = 2.5
        // 바깥 테두리 (적): 몸 · 외곽선이 스텐실을 찍고, 렌더러 기능(CatTangOuterOutline)이 그 바깥에만 색 띠를 그림 → 형체 안쪽에는 안 생김
        _OuterOutlineColor ("Outer Outline Color", Color) = (0.78, 0.07, 0.12, 1)
        _OuterOutlineWidth ("Outer Outline Width (px, 0 = 끔)", Range(0, 12)) = 0
        _OuterStencilRef ("Outer Outline Stencil Ref", Int) = 0
        [Toggle] _OutlineVertexAlpha ("Vertex Alpha = Width", Float) = 0
        [HideInInspector] _HitWhite ("Hit White", Range(0, 1)) = 0
        [HideInInspector] _SwingGlow ("Swing Glow", Color) = (0, 0, 0, 0)
        [HideInInspector] _SwingGlowStart ("Swing Glow Start", Vector) = (0, 0, 0, 0)
        [HideInInspector] _SwingGlowEnd ("Swing Glow End", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BrawlActor ("Brawl Actor (1 = 캐릭터 · 적)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half _ShadeThreshold;
            half4 _SecondShadeColor;
            half _SecondShadeThreshold;
            half _ShadeSoftness;
            half _AmbientStrength;
            half4 _RimColor;
            half _RimPower;
            half _RimThreshold;
            half _RimStrength;
            half4 _InkColor;
            half _InkWidth;
            half _InkStrength;
            half _HatchStrength;
            half _HatchDensity;
            half _HatchWidth;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _OutlineVertexAlpha;
            half _Saturation;
            half _Brightness;
            half4 _OuterOutlineColor;
            half _OuterOutlineWidth;
            half _RecolorAmount, _RecolorFrom, _RecolorWidth, _RecolorHue, _RecolorSat, _RecolorVal;
            half _BrawlActor;
            half _HitWhite;
            half4 _SwingGlow;
            float4 _SwingGlowStart, _SwingGlowEnd;
        CBUFFER_END

        // 브롤 스타즈풍 시험 모드 스위치 (전역, Shader.SetGlobalFloat). 0 = 원래 모습
        float _CatTangBrawl;
        // 3장 거울 바닥 반사 카메라가 그리는 중 (MirrorReflection): 외곽선 껍질은 뒤집힌 시점에서 검은 덩어리가 되니 건너뜀
        float _CatTangMirrorPass;

        half3 RgbToHsv(half3 c)
        {
            half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
            half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
            half d = q.x - min(q.w, q.y);
            half e = 1.0e-4;
            return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }
        half3 HsvToRgb(half3 c)
        {
            half3 p = abs(frac(c.xxx + half3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
            return c.z * lerp(1.0.xxx, saturate(p - 1.0), c.y);
        }
        half3 Recolor(half3 rgb)
        {
            if (_RecolorAmount <= 0.001) return rgb;
            half3 hsv = RgbToHsv(rgb);
            half dh = abs(frac(hsv.x - _RecolorFrom + 0.5) - 0.5);           // 색상 거리 (둘레)
            half w = 1 - smoothstep(_RecolorWidth * 0.7, _RecolorWidth, dh);
            w *= smoothstep(0.08, 0.18, hsv.y);                              // 회색 · 검정은 그대로
            w *= 1 - smoothstep(0.62, 0.8, hsv.y * step(0.35, hsv.z));       // 밝고 선명한 진홍 포인트는 유지
            half3 to = HsvToRgb(half3(_RecolorHue, saturate(hsv.y * _RecolorSat), saturate(hsv.z * _RecolorVal)));
            return lerp(rgb, to, w * _RecolorAmount);
        }
        ENDHLSL

        // ---------- 1. 본체 ----------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Stencil { Ref [_OuterStencilRef] Comp Always Pass Replace }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // 빛의 세기(0~1)를 3단 명암 색으로 바꾼다
            half3 ToonShade(half3 albedo, half light)
            {
                half s = _ShadeSoftness;
                half lit = smoothstep(_ShadeThreshold - s, _ShadeThreshold + s, light);
                half deep = smoothstep(_SecondShadeThreshold - s, _SecondShadeThreshold + s, light);
                half3 shade1 = albedo * _ShadeColor.rgb;
                half3 shade2 = albedo * _SecondShadeColor.rgb;
                return lerp(shade2, lerp(shade1, albedo, lit), deep);
            }

            half3 SwingEmission(half3 surface, float3 positionWS, half facing)
            {
                if (_SwingGlow.a <= 0.0h) return surface;
                float3 blade = _SwingGlowEnd.xyz - _SwingGlowStart.xyz;
                float3 rel = positionWS - _SwingGlowStart.xyz;
                float lengthSq = max(dot(blade, blade), 1e-4);
                float u = dot(rel, blade) / lengthSq;
                float3 offset = rel - saturate(u) * blade;
                half mask = (1.0h - smoothstep(0.026, 0.16, dot(offset, offset) / lengthSq))
                          * smoothstep(-0.08, 0.15, u);
                half rim = pow(1.0h - saturate(facing), 2.0h);
                half glow = saturate(_SwingGlow.a * mask * 1.36h);
                half3 heated = surface * 0.38h + _SwingGlow.rgb * (0.90h + rim * 1.55h);
                return lerp(surface, heated, glow);
            }

            // 브롤 스타즈풍: 그림자 1단(같은 색상을 어둡게) · 선명한 단색 면 · 작고 단단한 림 · 하이라이트. 배경은 한 톤 낮고 무늬를 뭉갬
            half4 FragBrawl(Varyings i)
            {
                bool actor = _BrawlActor > 0.5;
                half4 tex = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, i.uv, actor ? 0.25 : 0.75) * _BaseColor;
                tex.rgb = Recolor(tex.rgb);
                half3 hsv = RgbToHsv(tex.rgb);
                if (actor) { hsv.y = saturate(hsv.y * 1.25 + 0.04); hsv.z = saturate(hsv.z * 1.0); }
                else       { hsv.y = saturate(hsv.y * 1.08);       hsv.z = saturate(hsv.z * 0.95); }
                half3 albedo = HsvToRgb(hsv);
                // 그림자 색: 같은 색상, 조금 더 진하고 어둡게 (보랏빛 그림자 X)
                half3 shadeCol = HsvToRgb(half3(hsv.x, saturate(hsv.y * (actor ? 1.12 : 1.0) + (actor ? 0.06 : 0.0)), hsv.z * (actor ? 0.62 : 0.62)));

                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(N, mainLight.direction));
                half shadow = smoothstep(0.45h, 0.55h, mainLight.shadowAttenuation);
                half lit = smoothstep(0.27h, 0.31h, ndl * shadow);
                half3 lightTint = mainLight.color;
                half3 color = lerp(shadeCol, albedo, lit) * lightTint;
                color += albedo * SampleSH(N) * _AmbientStrength;

                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = N;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                    half al = saturate(dot(N, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
                    color += albedo * l.color * step(0.3h, al) * 0.6h;
                LIGHT_LOOP_END
                #endif

                if (actor)
                {
                    // 림: 빛 받는 쪽 가장자리에 얇고 단단한 흰 띠
                    half fresnel = pow(1.0h - saturate(dot(N, V)), 4.0h);
                    color += 0.28h * step(0.42h, fresnel) * lit;
                    // 하이라이트: 위쪽 빛 반사 작은 흰 점
                    half spec = step(0.985h, saturate(dot(N, normalize(mainLight.direction + V))));
                    color = lerp(color, 1.0h.xxx, spec * lit * 0.4h);
                }
                // 채도 살짝 (2026-11-17 사용자: 파스텔 톤이라 전체가 뿌옇다 → 밝기 올리기 · 배경 뭉개기를 줄이고 채도만 조금. 대비 늘리기는 빛 세기 > 1이라 과해져 뺌)
                half lum = dot(color, half3(0.299h, 0.587h, 0.114h));
                color = max(0, lerp(lum.xxx, color, 1.06h));
                color = SwingEmission(color, i.positionWS, dot(N, V));
                color = lerp(color, half3(1.16h, 1.16h, 1.16h), _HitWhite);
                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                if (_CatTangBrawl > 0.5) return FragBrawl(i);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                tex.rgb = Recolor(tex.rgb);
                half3 albedo = lerp(dot(tex.rgb, half3(0.299, 0.587, 0.114)).xxx, tex.rgb, _Saturation) * _Brightness;
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // 주 광원
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(N, mainLight.direction));
                // 그림자도 딱 끊기게 (부드러운 그림자 가장자리의 줄무늬 방지)
                half shadow = smoothstep(0.45h, 0.55h, mainLight.shadowAttenuation);
                half light = ndl * shadow;
                half3 color = ToonShade(albedo, light) * mainLight.color;

                // 명암 경계선에 잉크 띠 (손으로 그은 듯한 경계)
                half inkBand = 1.0h - smoothstep(0.0h, max(_InkWidth, 1e-3h), abs(light - _ShadeThreshold));
                color = lerp(color, _InkColor.rgb * mainLight.color, inkBand * _InkStrength * step(1e-4h, _InkWidth));

                // 깊은 그림자 안에만 사선 해칭 (월드 공간이라 카메라가 움직여도 미끄러지지 않음)
                half deepShade = 1.0h - smoothstep(_SecondShadeThreshold - _ShadeSoftness, _ShadeThreshold, light);
                half hatchCoord = (i.positionWS.x + i.positionWS.y * 0.6h - i.positionWS.z) * _HatchDensity;
                half hatchLine = step(frac(hatchCoord), _HatchWidth);
                color = lerp(color, _InkColor.rgb, hatchLine * deepShade * _HatchStrength);

                // 환경광 (아주 약하게, 그림자가 새까매지지 않게)
                color += albedo * SampleSH(N) * _AmbientStrength;

                // 추가 광원 (등불 등): 같은 명암 규칙으로 더한다
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = N;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                    half al = saturate(dot(N, l.direction)) * l.distanceAttenuation * l.shadowAttenuation;
                    half step1 = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, al);
                    color += albedo * l.color * step1;
                LIGHT_LOOP_END
                #endif

                // 림 라이트: 빛을 받는 쪽 가장자리만
                half fresnel = pow(1.0h - saturate(dot(N, V)), _RimPower);
                half rim = smoothstep(_RimThreshold - 0.02h, _RimThreshold + 0.02h, fresnel) * step(0.05h, light);
                color += _RimColor.rgb * rim * _RimStrength;

                color = SwingEmission(color, i.positionWS, dot(N, V));
                color = lerp(color, half3(1.16h, 1.16h, 1.16h), _HitWhite);
                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ---------- 2. 외곽선 (뒤집은 겹) ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            Stencil { Ref [_OuterStencilRef] Comp Always Pass Replace }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float fogFactor : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                if (_CatTangMirrorPass > 0.5) { o.positionCS = float4(0, 0, -2, 1); return o; }   // 거울 반사에선 외곽선 X
                float4 posCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = length(nCS);
                float2 dir = len > 1e-5 ? nCS / len : float2(0, 0);
                half width = _OutlineWidth * (_OutlineVertexAlpha > 0.5 ? v.color.a : 1.0h);
                // 브롤풍: 캐릭터 · 적은 굵고 일정한 검은 외곽선 3.5px (배경은 원래 선)
                if (_CatTangBrawl > 0.5 && _BrawlActor > 0.5) width = 3.5h * (_OutlineVertexAlpha > 0.5 ? v.color.a : 1.0h);
                // 화면 픽셀 기준 굵기: 카메라 거리와 상관없이 같은 두께
                posCS.xy += dir * width * 2.0 * posCS.w / _ScreenParams.xy;
                o.positionCS = posCS;
                o.fogFactor = ComputeFogFactor(posCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 oc = _CatTangBrawl > 0.5 && _BrawlActor > 0.5 ? half3(0.05h, 0.04h, 0.06h) : _OutlineColor.rgb;   // 브롤풍: 캐릭터 · 적은 검은 선
                return half4(MixFog(oc, i.fogFactor), 1);
            }
            ENDHLSL
        }

        // ---------- 2-1. 바깥 테두리 (스텐실 바깥에만, 렌더러 기능이 불투명 뒤에 그림) ----------
        Pass
        {
            Name "OuterOutline"
            Tags { "LightMode" = "CatTangOuterOutline" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Stencil { Ref [_OuterStencilRef] Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float fogFactor : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                if (_OuterOutlineWidth <= 0.01 || _CatTangMirrorPass > 0.5) { o.positionCS = float4(0, 0, -2, 1); return o; }   // 끔: 화면 밖
                float4 posCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = length(nCS);
                float2 dir = len > 1e-5 ? nCS / len : float2(0, 0);
                half baseW = _CatTangBrawl > 0.5 && _BrawlActor > 0.5 ? 3.5h : _OutlineWidth;
                posCS.xy += dir * (baseW + _OuterOutlineWidth) * 2.0 * posCS.w / _ScreenParams.xy;
                o.positionCS = posCS;
                o.fogFactor = ComputeFogFactor(posCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(MixFog(_OuterOutlineColor.rgb, i.fogFactor), 1);
            }
            ENDHLSL
        }

        // ---------- 3. 그림자 만들기 ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return posCS;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------- 4. 깊이 ----------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                return TransformObjectToHClip(v.positionOS.xyz);
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------- 5. 깊이 + 법선 (SSAO 등) ----------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(i.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
