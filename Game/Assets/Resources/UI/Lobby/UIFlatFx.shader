// 로비 UI 효과 (2026-11-18 사용자: "플랫하게 뽑은 뒤 코딩으로 효과 입혀봐")
// 플랫 그림(단색 칠 + 검은 외곽선)에 키 일러스트 같은 느낌을 셰이더로 입힘:
//  - 금색 칠(노랑 · 주황 계열) = 녹은 금: 위는 흰빛 노랑 → 아래는 진한 주황, 천천히 일렁이는 열기 얼룩
//  - 어두운 칠 · 진홍 = 사선 해칭(화면 픽셀 기준 잉크 줄) + 아래로 갈수록 더 어둡게 (강한 명암)
//  - 윗변 하이라이트(안쪽 베벨): 바로 위 픽셀이 투명한 곳은 뜨거운 금빛 테
//  - 빛 훑기: 몇 초마다 사선 빛 띠가 금 부분을 지나감
//  - _Hover(0~1): 금이 더 밝고 어두운 안쪽이 진홍으로 달아오름, 빛 훑기가 계속
Shader "CatTang/UIFlatFx"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Hatch ("Hatch Strength", Range(0, 1)) = 0.55
        _HatchSpacing ("Hatch Spacing (px)", Float) = 4
        _Shade ("Bottom Shade", Range(0, 1)) = 0.45
        _Molten ("Molten Gold", Range(0, 1)) = 1
        _Bevel ("Top Bevel", Range(0, 1)) = 0.8
        _ShineEvery ("Shine Period (s)", Float) = 3.5
        _ShineDur ("Shine Sweep Time (s, 0 = whole period)", Float) = 0
        _Hover ("Hover", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0

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
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Hatch, _HatchSpacing, _Shade, _Molten, _Bevel, _ShineEvery, _ShineDur, _Hover, _Seed;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + 1), f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float a = c.a;
                float3 col = c.rgb;
            #ifndef UNITY_COLORSPACE_GAMMA
                col = LinearToGammaSpace(col);                       // 프로젝트가 리니어라 감마 값으로 계산 (상수 색이 바래 보이지 않게)
            #endif
                float lum = dot(col, float3(0.3, 0.59, 0.11));
                float t = _Time.y + _Seed * 7.3;
                float y = i.uv.y;                                   // 0 = 아래, 1 = 위

                // 금색 판별 (노랑 · 주황: r > g > b, 채도 있음)
                float gold = saturate((col.r - col.b) * 2.2) * saturate((col.g - col.b) * 3.0) * step(0.25, lum);
                // 어두운 칠 · 진홍 (잉크 외곽선은 제외: 아주 검은 곳)
                float ink = step(lum, 0.06);
                float darkFill = (1 - gold) * (1 - ink) * saturate(1.2 - lum * 1.6);

                // 1) 녹은 금: 위 흰빛 노랑 → 아래 진한 주황 + 일렁이는 열기
                float heat = noise(i.uv * float2(6, 9) + float2(0, -t * 0.35)) * 0.6 + noise(i.uv * 20 + t * 0.2) * 0.4;
                float3 hot = lerp(float3(0.72, 0.2, 0.03), float3(1.0, 0.8, 0.3), saturate(y * 1.05 - 0.05 + (heat - 0.5) * 0.4));
                hot *= 0.88 + heat * 0.22 + _Hover * 0.2;
                col = lerp(col, hot, gold * _Molten);

                // 1.5) 어두운 칠: 위쪽은 진홍빛으로 살짝 떠오르게 (해칭이 보일 바탕)
                col = lerp(col, col + float3(0.17, 0.015, 0.06) * saturate(y * 1.2), darkFill);

                // 2) 아래로 갈수록 어둡게 (강한 명암)
                col *= lerp(1, saturate(0.35 + y), _Shade * (1 - gold * 0.6));

                // 3) 해칭: 어두운 칠과 진홍에 사선 잉크 줄, 아래쪽 · 어두울수록 촘촘
                float2 px = i.pos.xy;
                float sp = max(2, _HatchSpacing);
                float line1 = step(frac((px.x + px.y) / sp), 0.32);
                float line2 = step(frac((px.x - px.y) / (sp * 1.3)), 0.25);
                float dens = saturate((1 - y) * 1.3 + (0.5 - lum)) ;
                float hatch = saturate(line1 * step(0.25, dens) + line2 * step(0.6, dens));
                col *= 1 - hatch * _Hatch * (darkFill + gold * 0.25 * step(y, 0.45));

                // 4) 윗변 하이라이트 (위쪽 픽셀이 비었거나 검은 외곽선이면 금빛 테)
                float2 up = float2(0, _MainTex_TexelSize.y * 5);
                fixed4 above = tex2D(_MainTex, i.uv + up);
            #ifndef UNITY_COLORSPACE_GAMMA
                above.rgb = LinearToGammaSpace(above.rgb);
            #endif
                float edge = (1 - ink) * saturate(1 - above.a + step(dot(above.rgb, float3(0.3, 0.59, 0.11)), 0.06) * above.a);
                col = lerp(col, float3(1, 0.9, 0.55), edge * _Bevel * (0.35 + gold * 0.3));

                // 5) 빛 훑기 (금 부분만, 마우스 올리면 계속)
                float period = max(0.5, _ShineEvery);
                // _ShineDur > 0: 한 번 훑는 데 그만큼 걸리고 나머지는 쉼 (2026-11-26 사용자: 로고 광택이 너무 빠름)
                float sweep = _ShineDur > 0 ? min(_ShineDur, period) : period;
                float ph = saturate(frac(t / period) * period / sweep) * 2.2 - 0.6;
                float band = saturate(1 - abs((i.uv.x + (1 - y) * 0.35) - ph) * 9);
                float hoverBand = saturate(1 - abs(frac(i.uv.x * 0.8 - t * 0.9) - 0.5) * 6) * _Hover;
                col += float3(1, 0.75, 0.35) * (band * 0.45 + hoverBand * 0.3) * gold;

                // 6) 마우스 올림: 어두운 안쪽이 진홍으로 달아오름
                col = lerp(col, col + float3(0.32, 0.05, 0.02) * (1 - y * 0.5), darkFill * _Hover);

            #ifndef UNITY_COLORSPACE_GAMMA
                col = GammaToLinearSpace(max(col, 0));
            #endif
                return fixed4(col, a) * i.color;
            }
            ENDCG
        }
    }
}
