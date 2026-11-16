// Cat_Tang 툰 불꽃 셰이더 (URP, 3D 메시 이펙트용)
// - 빛 계산 없음. 가운데(카메라를 정면으로 보는 면)일수록 뜨겁고, 가장자리일수록 붉고 어두움 → 4단으로 딱 끊김
// - _Heat 1 → 0 으로 식으면 흰 심지가 사라지고 붉은색 → 그을린 갈색으로
// - _Dissolve 0 → 1 로 3D 노이즈 구멍이 뚫리며 사라짐 (구멍 가장자리는 주황 불테)
// - 외곽선: 뒤집은 겹, 같은 구멍으로 잘림 → 잉크 선이 있는 만화 불꽃
Shader "CatTang/ToonFire"
{
    Properties
    {
        _ColCore ("Core (white hot)", Color) = (1, 0.98, 0.86, 1)
        _ColHot ("Hot (yellow)", Color) = (1, 0.82, 0.25, 1)
        _ColMid ("Mid (orange)", Color) = (1, 0.45, 0.1, 1)
        _ColEdge ("Edge (red)", Color) = (0.78, 0.13, 0.08, 1)
        _ColAsh ("Ash (cooled)", Color) = (0.3, 0.17, 0.15, 1)
        _Heat ("Heat", Range(0, 1)) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 3.2
        _Seed ("Seed", Float) = 0
        _Intensity ("HDR Intensity", Range(1, 4)) = 1.6
        _OutlineColor ("Outline Color", Color) = (0.16, 0.04, 0.05, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 10)) = 2.5
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest+20" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _ColCore, _ColHot, _ColMid, _ColEdge, _ColAsh;
            half _Heat, _Dissolve, _NoiseScale, _Seed, _Intensity;
            half4 _OutlineColor;
            half _OutlineWidth;
        CBUFFER_END

        float Hash(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float Noise(float3 x)
        {
            float3 i = floor(x), f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(Hash(i + float3(0, 0, 0)), Hash(i + float3(1, 0, 0)), f.x),
                             lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                             lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
        }

        // 물체 기준 좌표 노이즈 (크기 · 방향이 바뀌어도 무늬가 따라감)
        float Fbm(float3 p)
        {
            p = p * _NoiseScale + _Seed * 7.13;
            return Noise(p) * 0.65 + Noise(p * 2.1 + 3.7) * 0.35;
        }
        ENDHLSL

        Pass
        {
            Name "Fire"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionOS = v.positionOS.xyz;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float n = Fbm(i.positionOS);
                clip(n - _Dissolve * 1.05);
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float facing = saturate(dot(N, V));
                // 열: 정면일수록 높고, 노이즈로 경계를 일렁이게, 식을수록 전체가 내려감
                float h = lerp(0.3, 1.0, facing) * sqrt(_Heat) + (n - 0.5) * 0.3;
                half3 c = _ColAsh.rgb;
                c = h > 0.12 ? _ColEdge.rgb : c;
                c = h > 0.38 ? _ColMid.rgb : c;
                c = h > 0.6 ? _ColHot.rgb : c;
                c = h > 0.8 ? _ColCore.rgb : c;
                // 녹아 사라지는 구멍 가장자리 = 주황 불테
                float rim = 1 - step(0.06, n - _Dissolve * 1.05);
                c = lerp(c, _ColMid.rgb * 1.2, rim * step(0.001, _Dissolve) * _Heat);
                half glow = lerp(1.0, _Intensity, saturate(h));
                return half4(c * glow, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionOS = v.positionOS.xyz;
                float4 posCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = length(nCS);
                float2 dir = len > 1e-5 ? nCS / len : float2(0, 0);
                posCS.xy += dir * _OutlineWidth * 2.0 * posCS.w / _ScreenParams.xy;
                o.positionCS = posCS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                clip(Fbm(i.positionOS) - _Dissolve * 1.05);
                return half4(lerp(_ColAsh.rgb * 0.5, _OutlineColor.rgb, _Heat), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
