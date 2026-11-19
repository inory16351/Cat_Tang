// 로비 '그림이 그려지는' 연출용 UI 셰이더: 가면(_Mask, 흑백)의 값이 _Progress보다 작은 곳부터 드러남. 경계(_Soft)는 부드럽게,
// 막 드러나는 가장자리는 _EdgeColor로 살짝 물듦(젖은 잉크 · 물감 번짐 느낌)
Shader "CatTang/UIReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Mask ("Reveal Mask", 2D) = "black" {}
        _Progress ("Progress", Range(0, 1.3)) = 0
        _Soft ("Softness", Range(0.001, 0.5)) = 0.08
        _EdgeColor ("Edge Color", Color) = (1, 0.6, 0.2, 0)
        _Boil ("Line Boil (px)", Float) = 0
        _BoilFps ("Boil FPS", Float) = 12

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

            sampler2D _MainTex, _Mask;
            float4 _MainTex_TexelSize;
            float _Boil, _BoilFps;
            fixed4 _Color, _EdgeColor;
            float _Progress, _Soft;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x), lerp(hash(i + float2(0, 1)), hash(i + 1), f.x), f.y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 로토스코핑 '라인 보일': 초당 _BoilFps번 바뀌는 잡음으로 그림을 몇 픽셀 일렁임 (손으로 매 장 다시 따라 그린 느낌)
                float2 uv = i.uv;
                if (_Boil > 0)
                {
                    float st = floor(_Time.y * _BoilFps);
                    float2 n = float2(vnoise(uv * float2(7, 5) + st * 17.31), vnoise(uv * float2(6, 8) + st * 29.77 + 5.1)) - 0.5;
                    n += (float2(vnoise(uv * 23 + st * 3.7), vnoise(uv * 21 - st * 5.3)) - 0.5) * 0.5;
                    uv += n * 2 * _Boil * _MainTex_TexelSize.xy;
                }
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                float m = tex2D(_Mask, uv).r;
                float a = saturate((_Progress - m) / _Soft);
                float edge = (1 - a) * saturate((_Progress + _Soft - m) / _Soft) * saturate(_Progress * 20);   // 막 드러나려는 띠 (시작 전엔 없음)
                c.rgb = lerp(c.rgb, _EdgeColor.rgb, saturate(edge * _EdgeColor.a + (1 - a) * _EdgeColor.a * 0.6));
                c.a *= saturate(a + edge * _EdgeColor.a * 0.5);
                return c;
            }
            ENDCG
        }
    }
}
