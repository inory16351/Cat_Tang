// 빛 더하기 셰이더 (총알 궤적 · 빛 번짐용). 텍스처 × 정점 색 × 세기를 화면에 더함 → 겹칠수록 밝아짐
Shader "CatTang/AdditiveGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 8)) = 1.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Intensity;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; half4 col : COLOR; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; half4 col : COLOR; };

            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = TRANSFORM_TEX(i.uv, _MainTex); o.col = i.col; return o; }

            half4 frag(V i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = t.a * i.col.a;
                return half4(t.rgb * i.col.rgb * a * _Intensity, 0);
            }
            ENDHLSL
        }
    }
}
