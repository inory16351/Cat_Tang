// 3장 '거울 속에 비친 시계' (신기루 마술사 진짜 찾기, 2026-11-18 사용자: 거울 속에 시계가 비친 느낌)
// 숫자 고리 그림(FX/boss_floor_clock)의 밝기만 가져와 거울빛(옅은 하늘 · 흰빛 + 분홍 노을)으로 다시 칠하고,
// 물결처럼 일렁이게 UV를 흔들고, 비스듬한 반짝임 띠가 흘러감. 가운데에서 바깥으로 번지며 떠오름(_Rise) · 사라짐(_Fade).
// 좌우 반전은 C#에서 판 크기 x를 음수로 (숫자 자체가 거울 글자로 뒤집혀 보임)
Shader "CatTang/MirrorClock"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Fade ("Fade", Range(0, 1)) = 1
        _Rise ("Rise", Range(0, 1)) = 1
        _Wobble ("Wobble", Range(0, 0.02)) = 0.006
        _Deep ("Deep color", Color) = (0.42, 0.45, 0.78, 1)
        _Bright ("Bright color", Color) = (0.88, 1, 1, 1)
        _Blush ("Blush color", Color) = (1, 0.66, 0.86, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "MirrorClock"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Fade, _Rise, _Wobble;
                half4 _Deep, _Bright, _Blush;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = TRANSFORM_TEX(i.uv, _MainTex); return o; }

            half4 frag(V i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float2 d = uv - 0.5;
                float r = length(d);
                // 물결 일렁임: 가로 · 세로 사인 + 가운데에서 퍼지는 동심 물결
                float2 wob = float2(sin(uv.y * 38 + t * 2.7), cos(uv.x * 33 - t * 2.3)) * _Wobble
                           + d / max(r, 1e-3) * sin(r * 60 - t * 4.5) * _Wobble * 0.6;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + wob);
                half lum = dot(tex.rgb, half3(0.3, 0.59, 0.11));
                half3 col = lerp(_Deep.rgb, _Bright.rgb, smoothstep(0.15, 0.85, lum));
                // 분홍 노을이 고리를 따라 천천히 돎
                float ang = atan2(d.y, d.x);
                col = lerp(col, _Blush.rgb, (0.5 + 0.5 * sin(ang * 2 + t * 0.8)) * 0.35);
                // 비스듬히 흘러가는 반짝임 띠
                float band = frac((uv.x + uv.y) * 0.8 - t * 0.35);
                col += smoothstep(0.06, 0.0, abs(band - 0.5)) * 0.55;
                // 가운데에서 바깥으로 번지며 떠오름
                half reveal = smoothstep(_Rise * 0.75 + 0.02, _Rise * 0.75 - 0.08, r);
                half a = tex.a * _Fade * reveal * (0.72 + 0.1 * sin(t * 3 + r * 20));
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
