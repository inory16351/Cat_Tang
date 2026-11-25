// 스토리 맵 3 호수 물 (2026-11-25 사용자: "물 느낌 확실하게")
// - 물결: 방향이 다른 사인 다섯 + 잔물결 노이즈로 법선을 흔듦 (시간에 따라 흐름)
// - 굴절: 불투명 화면(_CameraOpaqueTexture)을 물결만큼 비틀어 물속 바닥이 일렁이며 비침
// - 깊이 색: 깊이 텍스처로 물 두께를 재서 얕은 곳 = 맑은 청록, 깊은 곳 = 짙은 남색
// - 물가 거품: 물이 땅 · 기둥 · 보트와 만나는 곳에 끊기는 흰 거품 띠가 밀려왔다 빠짐
// - 반사: MirrorReflection의 거꾸로 비친 세상(_CatTangMirrorTex)을 물결로 비틀어 섞음 (프레넬), 반사가 없는 곳은 하늘색(_CatTangSkyColor)
// - 햇빛 반짝임: 잔물결 법선의 날카로운 하이라이트 → 툰처럼 또렷한 흰 점
// PC 렌더 설정(깊이 · 불투명 텍스처 켬) 기준. 모바일(꺼짐)에선 깊은 물 색 + 반사만
Shader "CatTang/LakeWater"
{
    Properties
    {
        _Shallow ("Shallow Color", Color) = (0.5, 0.9, 0.86, 1)
        _Deep ("Deep Color", Color) = (0.12, 0.38, 0.6, 1)
        _DepthRange ("Depth Range (m)", Float) = 1.4
        _Clarity ("Refraction Clarity", Range(0, 1)) = 0.7
        _ReflStrength ("Reflection Strength", Range(0, 1)) = 0.72
        _ReflTint ("Reflection Tint", Color) = (0.95, 0.98, 1, 1)
        _Distort ("Reflection Distortion", Range(0, 0.06)) = 0.022
        _WaveScale ("Wave Scale", Float) = 0.42
        _WaveSpeed ("Wave Speed", Float) = 0.7
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.42
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamDist ("Foam Distance (m)", Float) = 0.45
        _Sparkle ("Sun Sparkle", Range(0, 3)) = 1.4
        _SparkleColor ("Sparkle Color", Color) = (1, 0.97, 0.88, 1)
        _GlintAmount ("Glint Amount", Range(0, 1)) = 1
        _CrestAmount ("Wave Crest Lines", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_CatTangMirrorTex); SAMPLER(sampler_CatTangMirrorTex);
        float _CatTangMirrorOn;
        half4 _CatTangSkyColor;
        CBUFFER_START(UnityPerMaterial)
            half4 _Shallow, _Deep, _ReflTint, _FoamColor, _SparkleColor;
            half _DepthRange, _Clarity, _ReflStrength, _Distort, _WaveScale, _WaveSpeed, _WaveStrength, _FoamDist, _Sparkle, _GlintAmount, _CrestAmount;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x), lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
            }

            // 물결 높이의 기울기 (x, z): 사인 다섯 개의 해석적 미분
            float2 WaveSlope(float2 w, float t)
            {
                float2 g = 0;
                const float2 dirs[5] = { float2(0.94, 0.34), float2(-0.52, 0.85), float2(0.21, -0.98), float2(-0.86, -0.51), float2(0.66, 0.75) };
                const float freq[5] = { 1.0, 1.37, 1.9, 2.63, 3.7 };
                const float amp[5] = { 1.0, 0.72, 0.5, 0.34, 0.22 };
                [unroll] for (int k = 0; k < 5; k++)
                {
                    float f = freq[k] * _WaveScale;
                    float ph = dot(dirs[k], w) * f + t * _WaveSpeed * (1.0 + k * 0.37);
                    g += dirs[k] * cos(ph) * f * amp[k];
                }
                return g * 0.25;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 w = i.positionWS.xz;
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);

                // 법선: 큰 물결 + 흐르는 잔물결 (높은 주파수 사인 셋, 부드럽게)
                float2 slope = WaveSlope(w, t) * _WaveStrength;
                float2 fine = 0;
                {
                    const float2 fd[3] = { float2(0.8, 0.6), float2(-0.45, 0.89), float2(0.3, -0.95) };
                    [unroll] for (int k = 0; k < 3; k++)
                    {
                        float f = (4.1 + k * 1.7) * _WaveScale;
                        fine += fd[k] * cos(dot(fd[k], w) * f + t * (1.4 + k * 0.5)) * f * 0.06;
                    }
                }
                float3 n = normalize(float3(-(slope.x + fine.x * 0.5), 1, -(slope.y + fine.y * 0.5)));

                // 물 두께 (눈 공간 깊이 차)
                float sceneZ = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float surfZ = i.positionCS.w;
                float thick = max(0, sceneZ - surfZ);
                float depth01 = saturate(thick / _DepthRange);

                // 굴절: 얕을수록 바닥이 또렷하게 비침
                float2 refrUV = suv + n.xz * 0.035 * saturate(thick * 0.8);
                float refrZ = LinearEyeDepth(SampleSceneDepth(refrUV), _ZBufferParams);
                if (refrZ < surfZ) refrUV = suv;                                   // 물 위에 있는 것까지 비틀어 끌어오지 않게
                half3 below = SampleSceneColor(refrUV);
                half3 water = lerp(below * _Shallow.rgb * 1.15, _Deep.rgb, saturate(depth01 * (1.2 - _Clarity * 0.5)));
                water = lerp(water, _Deep.rgb, smoothstep(0.6, 1.0, depth01) * 0.6);

                // 반사 (프레넬)
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fres = 0.08 + 0.92 * pow(1 - saturate(dot(n, v)), 4);
                half3 sky = _CatTangSkyColor.a > 0 ? _CatTangSkyColor.rgb : _Deep.rgb * 1.6;
                half3 refl = sky;
                if (_CatTangMirrorOn > 0.5)
                {
                    half4 r = SAMPLE_TEXTURE2D(_CatTangMirrorTex, sampler_CatTangMirrorTex, suv + n.xz * _Distort);
                    refl = lerp(sky, r.rgb, saturate(r.a));
                }
                half reflAmt = _ReflStrength * lerp(0.55, 1.0, fres);
                half3 col = lerp(water, refl * _ReflTint.rgb, reflAmt);

                // 햇빛 반짝임 (2026-11-25 사용자: 각진 흰 반짝임 '조금은 넣되 덜하게'): 잔물결 노이즈 법선의 또렷한 흰 조각을
                // 큰 무늬 마스크로 군데군데만 남김 (처음 판의 약 40%)
                Light ml = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 hv = normalize(ml.direction + v);
                float2 chop = float2(vnoise(w * 2.3 + t * 0.6) - vnoise(w * 2.3 + float2(0.37, 0) + t * 0.6),
                                     vnoise(w * 2.1 - t * 0.5) - vnoise(w * 2.1 + float2(0, 0.37) - t * 0.5)) * 0.9;
                float3 n2 = normalize(float3(-(slope.x + chop.x) * 2.2, 1, -(slope.y + chop.y) * 2.2));
                float spec = pow(saturate(dot(n2, hv)), 260);
                float patch = smoothstep(0.42, 0.62, vnoise(w * 0.32 + float2(t * 0.12, -t * 0.08)));
                float glint = smoothstep(0.28, 0.34, spec) * patch * _GlintAmount;
                float sheen = pow(saturate(dot(n, hv)), 80) * 0.18;
                col += _SparkleColor.rgb * ml.color * (glint + sheen) * _Sparkle * lerp(0.4, 1, ml.shadowAttenuation);

                // 물결 줄: 물결 마루를 따라 가는 옅은 밝은 줄이 천천히 흘러감 (툰 물결)
                float wob = vnoise(w * 0.7 + t * 0.2) * 3.0;
                float crestA = sin(dot(w, float2(0.55, 0.83)) * 1.9 + t * 1.1 + slope.x * 5 + wob);
                float crestB = sin(dot(w, float2(-0.78, 0.62)) * 1.5 - t * 0.9 + slope.y * 5 - wob);
                float crest = smoothstep(0.975, 0.995, crestA) + smoothstep(0.98, 0.997, crestB) * 0.7;
                crest *= smoothstep(0.35, 0.7, vnoise(w * 0.45 + t * 0.15)) * _CrestAmount;
                col += half3(0.16, 0.19, 0.22) * saturate(crest);

                // 물가 거품: 땅 · 기둥 · 보트와 만나는 곳, 밀려왔다 빠지는 띠 + 끊김
                float edge = 1 - saturate(thick / _FoamDist);
                float band = 0.5 + 0.5 * sin(thick * 18 - t * 2.2);
                float breakup = vnoise(w * 3.1 + float2(t * 0.25, -t * 0.18));
                float foam = step(0.5, edge * (0.55 + 0.45 * band) + breakup * 0.35 - 0.12) * step(0.001, thick);
                col = lerp(col, _FoamColor.rgb, foam * 0.85);

                // 그림자는 옅게
                col *= lerp(1.0, ml.shadowAttenuation, 0.25);
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
