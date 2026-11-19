// 무기 칼빛 (BladeStreak): 3D로 실제 칼이 쓸고 지나간 면(칼 밑동 ~ 칼끝)에 그리는 사실적인 빨간 궤적 (2026-11-19 사용자: 3D 특성을 살린 사실적인 빨간 궤적)
// uv.x = 길이 (0 꼬리 → 1 머리 = 지금 칼끝 자리), uv.y = 폭 (0 칼 밑동 → 1 칼날 끝선), 정점 색 a = 나이에 따른 사그라듦
// 미리 곱한 알파 합성(Blend One OneMinusSrcAlpha): 붉은 몸은 바닥을 덮어 밝은 바닥(사막)에서도 또렷하고, 흰 심지는 HDR로 더해져 블룸이 번짐. 칼날 끝선이 가장 밝은 흰 심지, 안쪽 · 꼬리로 갈수록 붉게 옅어짐, 길이 방향 잔결
Shader "CatTang/BladeTrail"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.08, 0.1, 1)
        _Core ("Core", Color) = (1, 0.85, 0.8, 1)
        _Intensity ("Intensity", Float) = 3
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color, _Core;
            float _Intensity, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash(float x) { return frac(sin(x * 127.1 + _Seed * 31.7) * 43758.5453); }
            float vnoise(float x) { float i = floor(x), f = frac(x); f = f * f * (3 - 2 * f); return lerp(hash(i), hash(i + 1), f); }

            float4 frag (v2f i) : SV_Target
            {
                float u = saturate(i.uv.x), w = saturate(i.uv.y);
                // 폭 방향: 칼날 끝선(w=1)에 몰린 밝기 + 안쪽은 부드럽게 사라짐, 끝선 바깥은 살짝 둥글게 닫음
                float body = pow(w, 1.6) * smoothstep(1.0, 0.93, w);
                float edge = pow(w, 14) * smoothstep(1.0, 0.97, w) * 2.2;
                // 길이 방향: 머리(막 지나간 곳)가 가장 밝고 꼬리는 가늘게 사그라듦
                float along = pow(u, 1.4);
                float head = smoothstep(0.75, 1.0, u);
                // 잔결: 길이 방향으로 흐르는 가는 줄무늬 (빠르게 쓸린 빛)
                float streak = 0.72 + 0.28 * vnoise(w * 18 + u * 3);
                float a = i.color.a;
                float glow = (body * streak + edge) * along * a;
                // 색: 붉은 몸 → 끝선 · 머리는 흰빛에 가까운 심지
                float hot = saturate(edge * 0.8 + head * w * 0.6) * a;
                float3 c = lerp(_Color.rgb, _Core.rgb, hot);
                float cover = saturate((body * 0.95 + edge * 0.4) * along * a);       // 바닥을 덮는 정도 (붉은 몸)
                return float4(c * glow * _Intensity * 0.6 + _Color.rgb * cover * 0.55, cover);
            }
            ENDCG
        }
    }
}
