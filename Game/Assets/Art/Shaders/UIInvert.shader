// 반전 조준선 (UI, 2026-11-17 사용자: 실제 FPS 조준선처럼 밑 화면 색을 반전해 영역별로 확실히 구분)
// 블렌드 Exclusion: 결과 = src × (1 − 밑색) + 밑색 × (1 − src)
//  - src = 1 → 밑색 반전, src = 0.5 → 밑색과 상관없이 중간 회색, src = 0 → 그대로
// 그림(cursor_target_fill)의 알파 = 반전 세기 (칠한 곳 1 = 완전 반전). 외곽선은 따로 검은 그림(cursor_target_edge)을 위에 그냥 그림
// 정점 색 알파(Image.color.a)로 세기 조절 (장전 중 흐림)
Shader "CatTang/UIInvert"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (반전할 칠, 알파)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend OneMinusDstColor OneMinusSrcColor

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
                half v = t.a * i.color.a;                                  // 칠한 곳 1 → 밑색 반전, 바깥 0 → 그대로
                return fixed4(v, v, v, 1);
            }
            ENDCG
        }
    }
}
