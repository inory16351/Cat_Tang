// 칼빛 리본 v3 (2026-11-20 사용자: "블렌더로 깔끔하게" · 레퍼런스 = 스타일라이즈 에너지 이펙트의 색감 · 결, 빨간 참격 = 얇은 호 여러 겹)
// 결 텍스처 Resources/FX/Tex/slash_flow.png (Art/tools/blender_slash_tex.py로 구움): R 길게 늘인 보로노이 결 · G fbm 노이즈 · B 덩어리 보로노이
// uv.x = 길이 (0 꼬리 → 1 머리), uv.y = 폭 (0 칼끝 선 = 바깥 가장자리 → 1 안쪽)
// 정점 색: r = 열기 (휘두르는 동안 1 → 끝난 뒤 식음), g = 사라짐 (0 → 1, 결대로 갈라져 지워짐), b = 1이면 '칼날 레이저'(폭 가운데가 심지인 띠)
// 색 = 툰 단계: 흰 심지 → 뜨거운 색(바탕 + 흰빛) → 바탕 → 어두운 테두리 → 버림. 흰 · 뜨거운 단계만 _Intensity로 HDR (블룸)
Shader "CatTang/SlashRibbon"
{
    Properties
    {
        _MainTex ("Flow (R streak G erode B chunk)", 2D) = "gray" {}
        _Color ("Base", Color) = (1, 0.1, 0.12, 1)
        _Intensity ("HDR boost", Float) = 2.6
        _Scroll ("Scroll", Float) = 2.2
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _Intensity, _Scroll, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float4 frag (v2f i) : SV_Target
            {
                float q = saturate(i.uv.x), o = saturate(i.uv.y);
                float heat = i.color.r, gone = i.color.g, beam = i.color.b;
                // 폭 방향 모양: 리본 = 바깥(칼끝 선)이 가장 뜨겁고 안쪽으로 식음 / 레이저 = 가운데가 심지
                float across = lerp(pow(1 - o, 1.35), 1 - abs(o * 2 - 1), beam);
                // 결: 길이 방향으로 흐르는 줄무늬 (머리 쪽이 빠르게 흘러감)
                float2 fuv = float2(q * 1.6 - _Time.y * _Scroll + _Seed, o * 0.92 + 0.04);
                float4 f = tex2D(_MainTex, fuv);
                float streak = lerp(0.55, 1.15, f.r);
                // 머리 쪽이 가장 셈, 꼬리는 바늘 끝
                float along = lerp(pow(q, 0.8), 1, beam);
                float v = across * streak * along * (0.75 + 0.45 * heat);
                // 사라짐: 노이즈 결 문턱이 올라가며 찢겨 지워짐 (가장자리는 덩어리 보로노이로 먼저)
                float erode = f.g * 0.7 + f.b * 0.3;
                float cut = gone * 1.15 + (1 - across) * 0.25 * gone;
                clip(erode - cut + 0.02);
                clip(v - 0.07);
                // 툰 단계
                float3 base = _Color.rgb;
                float3 hot = lerp(base, float3(1, 1, 1), 0.5);
                float3 dark = base * 0.38;
                float3 c = v > 0.78 - 0.18 * heat ? float3(1, 1, 1) * _Intensity
                         : v > 0.5 - 0.1 * heat ? hot * (1 + (_Intensity - 1) * 0.6)
                         : v > 0.24 ? base * 1.15
                         : dark;
                // 찢기는 경계는 어둡게 한 겹 (레퍼런스의 어두운 테두리)
                c = (erode - cut) < 0.07 && gone > 0.01 ? dark : c;
                return float4(c, 1);
            }
            ENDCG
        }
    }
}
