// 유령 오라 셰이더 (2026-11-25 사용자: 미련 원혼 "주변에 오라처럼 광 효과 넣어서 연출")
// 몸 메시를 법선 방향으로 _Inflate만큼 부풀린 껍데기에 가장자리(프레넬) 빛을 더함 → 실루엣 둘레로 번지는 후광
// - FxPart가 넣는 _Heat(1 → 0 식음) · _Dissolve(0 → 1 사라짐)를 그대로 받아 몸과 같이 밝아졌다 흐려짐
// - 아지랑이: 높이 · 시간에 따라 껍데기가 살짝 일렁이고 빛 세기가 위로 흘러가며 깜빡임
Shader "CatTang/GhostAura"
{
    Properties
    {
        _Color ("Color", Color) = (0.45, 0.52, 1, 1)
        _CoreColor ("Core Color", Color) = (0.85, 0.9, 1, 1)
        _Intensity ("Intensity", Range(0, 8)) = 2.2
        _Inflate ("Inflate", Range(0, 0.3)) = 0.06
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.2
        _Wobble ("Wobble", Range(0, 0.1)) = 0.025
        _Heat ("Heat", Range(0, 1)) = 1
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+5" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Aura"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _CoreColor;
                half _Intensity, _Inflate, _RimPower, _Wobble, _Heat, _Dissolve, _Seed;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 nW : TEXCOORD0; float3 vW : TEXCOORD1; float h : TEXCOORD2; };

            V vert(A i)
            {
                V o;
                float t = _Time.y * 2.6 + _Seed;
                float w = sin(i.pos.y * 9.0 + t) * 0.5 + sin(i.pos.x * 13.0 - t * 1.3) * 0.5;
                float3 p = i.pos.xyz + i.n * (_Inflate + w * _Wobble);
                float3 pw = TransformObjectToWorld(p);
                o.pos = TransformWorldToHClip(pw);
                o.nW = TransformObjectToWorldNormal(i.n);
                o.vW = GetWorldSpaceViewDir(pw);
                o.h = i.pos.y;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float3 n = normalize(i.nW), v = normalize(i.vW);
                half facing = saturate(dot(n, v));
                half rim = pow(1 - facing, _RimPower);
                // 위로 흘러가는 빛 띠 (아지랑이)
                half flow = 0.75 + 0.25 * sin(i.h * 14.0 - _Time.y * 6.0 + _Seed);
                half life = saturate(_Heat * 0.7 + 0.3) * saturate(1 - _Dissolve * 1.2);
                half3 c = lerp(_Color.rgb, _CoreColor.rgb, rim * rim * _Heat);
                return half4(c * rim * flow * life * _Intensity, 0);
            }
            ENDHLSL
        }
    }
}
