// 물감이 화면을 뒤덮으며 넘어가는 전환 (2026-11-23 사용자: "물감이 뒤덮이면서 화면전환 되는 느낌도 한번 줘봐 중간중간")
// _Prog 0 → 1: 여러 곳에서 물감 덩어리가 번져 앞 장면(_MainTex)을 덮음 (젖은 가장자리 · 꽃 모양 번짐 · 안료 섞임 · 종이 결)
// _Prog 1 → 2: 앞 장면은 사라지고, 물감이 다른 모양으로 걷히며 밑의 다음 장면이 드러남
Shader "CatTang/PaintWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Prev Screen", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Prog ("Progress", Range(0, 2)) = 0
        _Seed ("Seed", Float) = 0
        _ColA ("Pigment A", Color) = (0.82, 0.52, 0.57, 1)
        _ColB ("Pigment B", Color) = (0.55, 0.45, 0.64, 1)
        _ColC ("Pigment C", Color) = (0.9, 0.74, 0.55, 1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex; fixed4 _Color; float _Prog, _Seed; fixed4 _ColA, _ColB, _ColC;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + 1), f.x), f.y); }
            float fbm(float2 p) { return vnoise(p) * 0.5 + vnoise(p * 2.1) * 0.27 + vnoise(p * 4.3) * 0.15 + vnoise(p * 9.1) * 0.08; }

            // 번짐 지형: 낮을수록 먼저 칠해짐 — 무작위 물감 방울 몇 개(가까울수록 낮음) + 꽃 모양으로 들쭉날쭉한 가장자리
            float Field(float2 uv, float seed)
            {
                float2 q = uv * float2(1.78, 1);
                float d = 9;
                [unroll] for (int k = 0; k < 5; k++)
                {
                    float2 c = float2(hash21(float2(k, seed)) * 1.78, hash21(float2(seed, k + 7.3)));
                    d = min(d, length(q - c) * (0.8 + 0.5 * hash21(float2(k * 3.1, seed + 1))));
                }
                return d * 0.75 + fbm(q * 3.2 + seed * 5.7) * 0.45 + fbm(q * 11 + seed) * 0.08;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                bool cover = _Prog < 1;
                float seed = cover ? _Seed : _Seed + 17.0;
                float cov = cover ? _Prog : 2 - _Prog;                     // 덮는 양
                float f = Field(i.uv, seed);
                float t = cov * 1.35 - 0.05;
                float m = 1 - smoothstep(t - 0.05, t, f);                  // 물감이 덮은 곳
                float edge = exp(-pow((f - t + 0.02) / 0.025, 2));         // 젖은 가장자리 (안료가 고여 짙음)
                float2 q = i.uv * float2(1.78, 1);
                float n1 = fbm(q * 2.3 + seed * 3.1), n2 = fbm(q * 5.0 - seed);
                float3 paint = lerp(lerp(_ColA.rgb, _ColB.rgb, smoothstep(0.35, 0.7, n1)), _ColC.rgb, smoothstep(0.62, 0.85, n2) * 0.6);
                paint *= 0.92 + 0.16 * vnoise(q * 160);                      // 종이 결
                paint *= 1 - 0.3 * edge;
                paint = lerp(paint, paint * 0.82, smoothstep(0.4, 0.9, fbm(q * 7 + 3)) * 0.5);   // 얼룩 (마르며 생긴 농담)
                fixed4 prev = tex2D(_MainTex, i.uv);
                if (cover) return fixed4(lerp(prev.rgb, paint, m), 1) * i.color;
                return fixed4(paint, m) * i.color;                           // 걷힘: 물감만 남아 사라짐
            }
            ENDCG
        }
    }
}
