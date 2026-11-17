// 실루엣 잔상: 단색 + 가장자리 밝은 림. 깊이를 같이 써서 안쪽 면이 겹쳐 비치지 않게 (URP는 태그 없는 패스를 하나만 그림 → 한 패스로)
Shader "CatTang/Silhouette"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.82, 0.38, 0.7)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.2
        _RimBoost ("Rim Boost", Range(0, 2)) = 0.9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }   // Vivid(후처리 뒤 RenderObjects)에서도 그려지게
            ZWrite On
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _RimPower, _RimBoost;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float rim = pow(1 - saturate(abs(dot(normalize(i.n), normalize(i.v)))), _RimPower);
                float3 c = _Color.rgb * (0.85 + rim * _RimBoost) + rim * 0.25;
                return float4(c, saturate(_Color.a * (0.75 + rim * 0.6)));
            }
            ENDCG
        }
    }
}
