// 3장 상인 '거울 속 고양이' (2026-11-18 사용자: 거울에서 반사된 존재라는 걸 알 수 있게 반투명 + 오라)
// 원래 색 텍스처를 그대로 쓰되 반투명 · 거울빛(옅은 분홍 흰빛)으로 물들이고, 가장자리는 금빛 림으로 빛남.
// 위아래로 흐르는 가는 반짝임 띠(_ScanSpeed)로 거울 표면처럼 일렁임. 깊이를 먼저 써서 안쪽 면이 겹쳐 비치지 않게 (패스 2개)
Shader "CatTang/MirrorGhost"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Tint ("Mirror Tint", Color) = (1, 0.9, 0.96, 1)
        _TintAmount ("Tint Amount", Range(0, 1)) = 0.35
        _Alpha ("Alpha", Range(0, 1)) = 0.55
        _RimColor ("Rim Color", Color) = (1, 0.85, 0.5, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.4
        _ScanSpeed ("Scan Speed", Float) = 0.8
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _Tint, _RimColor;
            half _TintAmount, _Alpha, _RimPower, _ScanSpeed;
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };
        Varyings vert(Attributes v)
        {
            Varyings o;
            VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
            return o;
        }
        ENDHLSL

        // 1. 깊이만 (색 안 씀) — 뒤쪽 면이 겹쳐 진해지지 않게
        Pass
        {
            Name "GhostDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            half4 fragDepth(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        // 2. 반투명 색
        Pass
        {
            Name "GhostColor"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                albedo = lerp(albedo, _Tint.rgb, _TintAmount);
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half rim = pow(1.0h - saturate(abs(dot(N, V))), _RimPower);
                // 거울 표면 반짝임 띠: 높이 따라 위로 흐름
                half scan = smoothstep(0.92h, 1.0h, frac(i.positionWS.y * 0.6 - _Time.y * _ScanSpeed));
                half3 col = albedo * 1.1h + _RimColor.rgb * rim * 1.2h + scan * 0.35h;
                half a = saturate(_Alpha + rim * 0.4h + scan * 0.2h);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
