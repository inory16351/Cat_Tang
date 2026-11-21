// 3장 거울 호수 바닥 (2026-11-18 사용자: 모래사장 느낌 말고 거울 느낌)
// - 바닥 그림(_BaseMap = 노을 하늘이 비친 판석, Art/tools/ground_texture_ch3.py) 위에
//   반사 카메라(MirrorReflection)가 그린 '거꾸로 비친 세상'(_CatTangMirrorTex, 전역)을 화면 좌표로 섞음
// - 반사 그림은 잔물결로 살짝 일렁이고(_Ripple), 반사가 없는 곳(알파 0)은 바닥 그림 그대로
// - 그림자는 옅게만 받음 (거울이라 진한 그림자는 어색)
Shader "CatTang/MirrorFloor"
{
    Properties
    {
        _BaseMap ("Sky Reflection (tiling)", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _ReflStrength ("Reflection Strength", Range(0, 1)) = 0.62
        _ReflTint ("Reflection Tint", Color) = (0.92, 0.94, 1, 1)
        _Ripple ("Ripple Distortion", Range(0, 0.02)) = 0.004
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_CatTangMirrorTex); SAMPLER(sampler_CatTangMirrorTex);
        float _CatTangMirrorOn;
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _ReflStrength;
            half4 _ReflTint;
            half _Ripple;
            half _ShadowStrength;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                if (_CatTangMirrorOn > 0.5)
                {
                    float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                    float t = _Time.y;
                    float2 w = i.positionWS.xz;
                    // 잔물결: 서로 다른 방향 사인 두 개 → 반사가 살짝 일렁임
                    float2 ripple = float2(sin(w.x * 1.7 + w.y * 0.6 + t * 1.3), cos(w.y * 1.9 - w.x * 0.5 + t * 1.1)) * _Ripple;
                    half4 refl = SAMPLE_TEXTURE2D(_CatTangMirrorTex, sampler_CatTangMirrorTex, suv + ripple);
                    col = lerp(col, refl.rgb * _ReflTint.rgb, saturate(refl.a) * _ReflStrength);
                }
                Light ml = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half sh = lerp(1.0h, ml.shadowAttenuation, _ShadowStrength);
                half3 reflectionBase = col;
                col *= sh;
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = half3(0, 1, 0);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint count = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light localLight = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                    half localShadow = lerp(1.0h, localLight.shadowAttenuation, _ShadowStrength);
                    col += reflectionBase * localLight.color * saturate(localLight.direction.y)
                           * localLight.distanceAttenuation * localShadow * 0.65h;
                LIGHT_LOOP_END
                #endif
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
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
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
