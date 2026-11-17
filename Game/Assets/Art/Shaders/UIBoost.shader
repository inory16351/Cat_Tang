// 반전 조준선 밝히기 (UI, 2026-11-17 사용자: 반전 색을 채도 · 명도 최대로 선명하게, 흰색에 가깝게)
// CatTang/UIInvert로 반전한 자리 위에 같은 그림을 한 번 더: 블렌드 2배 곱하기 → 결과 = 밑색 × src × 2
//  - src = 1 → 밑색 2배 (밝아지고, 넘친 채널이 잘리며 채도도 올라감), src = 0.5 → 그대로
// _Gain: 몇 배로 밝힐지 (1 = 그대로, 2 = 최대 — 블렌드 한 번으로는 2배까지)
Shader "CatTang/UIBoost"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (밝힐 칠, 알파)", 2D) = "white" {}
        _Gain ("Gain (1~2)", Range(1, 2)) = 2
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend DstColor SrcColor

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Gain;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                half k = lerp(1.0h, _Gain, t.a * i.color.a);   // 칠한 곳만 _Gain배, 바깥은 1배(그대로)
                half v = k * 0.5h;                             // 2배 곱하기 블렌드: 0.5 = 그대로
                return fixed4(v, v, v, 1);
            }
            ENDCG
        }
    }
}
