// 색 반전 덮개 (2026-11-23 꿈 연출: 박자마다 화면이 네거티브로 번쩍)
// 하얀 판을 Blend OneMinusDstColor OneMinusSrcColor로 겹침 → 결과 = 밑색 + a·(1 − 2·밑색) = 밑색과 반전색 사이 a만큼
Shader "CatTang/UIInvert"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend OneMinusDstColor OneMinusSrcColor
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; };
            fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target { float a = i.color.a; return fixed4(a, a, a, a); }
            ENDCG
        }
    }
}
