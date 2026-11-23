// 스토리 몽타주 레이어용 UI 블러 (2026-11-23 사용자: 심도를 연출에 따라 조절)
// UI Image 재질: _Blur(화면 비율 반지름)만큼 12점 원판 샘플링 → 레이어마다 초점 흐림을 실시간으로 바꿈 (초점 이동 = 값 애니메이션)
Shader "CatTang/UIBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Blur ("Blur", Range(0, 0.05)) = 0
        _Edge ("Edge Blur", Range(0, 0.05)) = 0
        _Sat ("Saturation", Range(0, 1.5)) = 1
        _Paint ("Watercolor Blend", Range(0, 1)) = 0
        _PaperTint ("Paper Tint", Color) = (0.95, 0.9, 0.82, 1)
        _Reveal ("Paint Reveal Front", Float) = 2
        _RevealDir ("Paint Reveal Dir", Vector) = (1, 0, 0, 0)
        _RevealSoft ("Paint Reveal Softness", Float) = 0.12
        _Sepia ("Grade Sepia", Range(0, 1)) = 0
        _Invert ("Grade Invert", Range(0, 1)) = 0
        _Duo ("Grade Duotone", Range(0, 1)) = 0
        _DuoA ("Duotone Shadow", Color) = (0.05, 0.1, 0.3, 1)
        _DuoB ("Duotone Light", Color) = (0.9, 0.95, 1, 1)
        _Hue ("Grade Hue Shift (rad)", Float) = 0
        _Contrast ("Grade Contrast", Float) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
            sampler2D _MainTex; float4 _MainTex_TexelSize; fixed4 _Color; float _Blur; float _Edge; float _Sat; float _Paint; fixed4 _PaperTint; float _Reveal; float4 _RevealDir; float _RevealSoft;
            float _Sepia, _Invert, _Duo, _Hue, _Contrast; fixed4 _DuoA, _DuoB;
            /// 배색 변주 (2026-11-23 사용자: "배색이랑 질감이 동일해서 … 세피아 필터나 색반전 같은 것도"):
            ///  감마 공간에서 색상 돌리기 → 대비 → 두 색 판화(듀오톤) → 세피아 → 색 반전. 전부 0이면 그대로
            fixed3 Grade(fixed3 c)
            {
                if (_Sepia + _Invert + _Duo + abs(_Hue) + abs(_Contrast - 1) < 0.001) return c;
            #ifndef UNITY_COLORSPACE_GAMMA
                c = LinearToGammaSpace(c);
            #endif
                if (abs(_Hue) > 0.001) { const float3 k = float3(0.57735, 0.57735, 0.57735); float ca = cos(_Hue); c = c * ca + cross(k, c) * sin(_Hue) + k * dot(k, c) * (1 - ca); }
                c = saturate((c - 0.5) * _Contrast + 0.5);
                float l = dot(c, float3(0.3, 0.59, 0.11));
                c = lerp(c, lerp(_DuoA.rgb, _DuoB.rgb, smoothstep(0.04, 0.96, l)), _Duo);
                float3 sp = float3(dot(c, float3(0.393, 0.769, 0.189)), dot(c, float3(0.349, 0.686, 0.168)), dot(c, float3(0.272, 0.534, 0.131)));
                c = lerp(c, saturate(sp), _Sepia);
                c = lerp(c, 1 - c, _Invert);
            #ifndef UNITY_COLORSPACE_GAMMA
                c = GammaToLinearSpace(saturate(c));
            #endif
                return c;
            }
            fixed4 Graded(fixed4 c) { c.rgb = Grade(c.rgb); return c; }
            // 물감 섞임용 값 노이즈
            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p) { float2 i = floor(p), f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + 1), f.x), f.y); }
            /// 오려 낸 오브젝트가 수채화 바탕에 물감처럼 섞이게 (2026-11-23 사용자: "오브젝트들이 배경에 안 섞이니까 물감이 섞인 느낌을 셰이더로")
            ///  · 가장자리가 노이즈로 들쭉날쭉 풀림(번짐) · 가장자리에 안료가 고여 짙어짐 · 안쪽은 얼룩덜룩 비침(입자) · 종이 결 · 종이빛으로 살짝 물듦
            fixed4 Paint(fixed4 c, float aSoft, float2 uv)
            {
                if (_Paint <= 0.001) return c;
                float asp = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 q = uv * float2(asp, 1);
                float n = vnoise(q * 9) * 0.55 + vnoise(q * 31) * 0.3 + vnoise(q * 90) * 0.15;
                float a = saturate(aSoft * (1 + 0.8 * _Paint) - n * 0.75 * _Paint);
                float edge = saturate(aSoft * (1 - aSoft) * 4);
                c.rgb *= 1 - _Paint * 0.38 * edge * (0.6 + 0.8 * n);                 // 젖은 가장자리에 고인 안료
                float grain = vnoise(q * 220) * 0.6 + vnoise(q * 70) * 0.4;
                c.rgb *= lerp(1, 0.86 + 0.26 * grain, _Paint * 0.7);                    // 종이 결
                c.rgb = lerp(c.rgb, c.rgb * _PaperTint.rgb * 1.05, _Paint * 0.35);      // 종이빛으로 물듦
                c.a = a * lerp(1, 0.9, _Paint);
                return c;
            }
            /// 그려짐 (2026-11-23 사용자: "파노라마적 이동 … 이동과 맞추어 그림이 그려지는 느낌"):
            ///  물감 경계(_Reveal, 방향 _RevealDir)가 번지며 지나간 곳만 칠해짐 · 경계는 들쭉날쭉 젖은 테 · 아직 안 칠한 곳은 연한 밑그림만
            fixed4 Reveal(fixed4 c, float2 uv)
            {
                if (_Reveal > 1.5) return c;
                float asp = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 dir = normalize(_RevealDir.xy + float2(1e-4, 0));
                float d = dot(uv - 0.5, dir) + 0.5;
                float2 q = uv * float2(asp, 1);
                float n = vnoise(q * 6) * 0.6 + vnoise(q * 23) * 0.3 + vnoise(q * 70) * 0.1;
                float e = d - _Reveal + (n - 0.5) * 0.22;                            // < 0 = 칠해진 쪽
                float m = 1 - smoothstep(-_RevealSoft, 0, e);
                float band = exp(-pow((e + _RevealSoft * 0.35) / (_RevealSoft * 0.45), 2));
                c.rgb *= 1 - 0.28 * band * m;                                         // 번져 가는 젖은 테 (안료가 고임)
                float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
                float3 sketch = lerp(_PaperTint.rgb, lum.xxx * float3(0.62, 0.55, 0.62), 0.5);
                c.rgb = lerp(sketch, c.rgb, m);
                c.a *= lerp(0.16, 1, m);                                              // 밑그림만 옅게
                return c;
            }
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                // 꿈 · 회상 표식: 가장자리로 갈수록 더 흐림(_Edge) · 채도 빠짐(_Sat)
                float2 d = i.uv - 0.5;
                float b = _Blur + _Edge * smoothstep(0.18, 0.62, length(d * float2(1.0, 1.25)));
                b = max(b, _Paint * 0.004);   // 물감 섞임: 가장자리를 부드럽게 풀 여유
                if (b <= 0.0001) { fixed4 c0 = tex2D(_MainTex, i.uv) * i.color; c0.rgb = lerp(dot(c0.rgb, float3(0.3, 0.59, 0.11)).xxx, c0.rgb, _Sat); return Graded(Reveal(c0, i.uv)); }
                // 가로세로 비율 보정 (텍스처 비율)
                float2 r = float2(b, b * _MainTex_TexelSize.z / _MainTex_TexelSize.w);
                static const float2 k[12] = {
                    float2(-0.326,-0.406), float2(-0.840,-0.074), float2(-0.696, 0.457), float2(-0.203, 0.621),
                    float2( 0.962,-0.195), float2( 0.473,-0.480), float2( 0.519, 0.767), float2( 0.185,-0.893),
                    float2( 0.507, 0.064), float2( 0.896, 0.412), float2(-0.322,-0.933), float2(-0.792,-0.598) };
                // 알파 가중 평균 (오려 낸 부품의 투명한 곳 색이 가장자리에 회색 테로 번지지 않게)
                float4 t0 = tex2D(_MainTex, i.uv); float4 acc = float4(t0.rgb * t0.a, t0.a);
                [unroll] for (int n = 0; n < 12; n++) { float4 t = tex2D(_MainTex, i.uv + k[n] * r); acc += float4(t.rgb * t.a, t.a); }
                [unroll] for (int m = 0; m < 12; m++) { float4 t = tex2D(_MainTex, i.uv + k[m] * r * 0.5); acc += float4(t.rgb * t.a, t.a); }
                float4 avg = float4(acc.rgb / max(acc.a, 0.0001), acc.a / 25.0);
                fixed4 c = avg * i.color;
                c.rgb = lerp(dot(c.rgb, float3(0.3, 0.59, 0.11)).xxx, c.rgb, _Sat);
                if (_Paint > 0.001) { fixed4 p = Paint(fixed4(c.rgb, 1), avg.a, i.uv); c = fixed4(p.rgb, p.a * i.color.a); }
                return Graded(Reveal(c, i.uv));
            }
            ENDCG
        }
    }
}
