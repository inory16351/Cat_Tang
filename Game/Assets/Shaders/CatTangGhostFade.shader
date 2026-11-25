// 스토리 맵 그림자 유령 (2026-11-25 사용자: 꼬리가 아래로 갈수록 디졸브되어 배경에 섞이게)
// CatTang/Silhouette(단색 + 가장자리 밝은 림)과 같고, 블렌더가 구운 정점 색 알파로 꼬리 끝을 투명하게.
// 알파가 낮은 곳은 노이즈 구멍으로 먼저 흩어져 연기처럼 녹아 사라짐. 깊이는 불투명한 곳만 써서 꼬리 너머 배경이 비침
Shader "CatTang/GhostFade"
{
    Properties
    {
        _Color ("Color", Color) = (0.16, 0.08, 0.2, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.2
        _RimBoost ("Rim Boost", Range(0, 2)) = 1.2
        _NoiseScale ("Dissolve Noise Scale", Float) = 7
        _NoiseSpeed ("Dissolve Noise Speed", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color;
            float _RimPower, _RimBoost, _NoiseScale, _NoiseSpeed;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float3 wp : TEXCOORD2; float fade : TEXCOORD3; };
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = normalize(WorldSpaceViewDir(v.vertex));
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.fade = v.color.a;
                return o;
            }
            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float noise(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash(i), hash(i + float3(1,0,0)), f.x), lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x), lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float rim = pow(1 - saturate(abs(dot(normalize(i.n), normalize(i.v)))), _RimPower);
                float3 c = _Color.rgb * (0.85 + rim * _RimBoost) + rim * 0.25;
                float a = saturate(_Color.a * (0.75 + rim * 0.6));
                // 꼬리: 정점 알파로 흐려지고, 위로 흘러가는 노이즈가 먼저 구멍을 내 연기처럼 녹음
                float n = noise(i.wp * _NoiseScale + float3(0, -_Time.y * _NoiseSpeed, 0));
                float f = saturate(sqrt(i.fade) * 1.5 - n * 0.4);                    // 꼬리 대부분은 보이고 끝 쪽에서 천천히 흩어짐
                a *= f;
                clip(a - 0.02);
                return float4(c, a);
            }
            ENDCG
        }
    }
}
