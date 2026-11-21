// UV.x follows the swing (tail to head), UV.y crosses the full ribbon width.
// COLOR.r carries sample energy, COLOR.g carries age, COLOR.b selects a halo layer.
Shader "CatTang/SlashRibbon"
{
    Properties
    {
        _MainTex ("Flow (R grain G decay B intensity)", 2D) = "gray" {}
        _Color ("Base", Color) = (1, 0.1, 0.12, 1)
        _Intensity ("HDR boost", Float) = 2.1
        _Scroll ("Scroll", Float) = 0.55
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

            sampler2D _MainTex;
            float4 _Color;
            float _Intensity, _Scroll, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float4 frag (v2f i) : SV_Target
            {
                float q = saturate(i.uv.x), across = saturate(i.uv.y);
                float energy = i.color.r, age = i.color.g;
                if (i.color.b > 0.75)
                {
                    float halo = pow(saturate(sin(across * UNITY_PI)), 3.0) * saturate(sin(q * UNITY_PI));
                    return float4(_Color.rgb * halo * energy * 0.85, 0);
                }

                if (i.color.b > 0.25)
                {
                    float halo = pow(saturate(sin(across * UNITY_PI)), 2.5) * (1 - smoothstep(0.0, 0.85, age));
                    return float4(_Color.rgb * halo * energy * 0.42, 0);
                }

                float3 flow = tex2D(_MainTex, float2(q * 1.25 + _Seed - _Time.y * _Scroll,
                                                     across * 0.92 + 0.04)).rgb;
                float edge = smoothstep(0.0, 0.08, across) * (1 - smoothstep(0.72, 1.0, across));
                float fade = 1 - smoothstep(0.05, 1.0, age + (flow.g - 0.5) * age * 0.25);
                float body = edge * fade * lerp(0.68, 1.0, flow.b) * lerp(0.40, 1.0, energy);
                float rim = exp2(-12.0 * abs(across - 0.08)) * edge;
                float light = body * (0.70 + _Intensity * rim * energy) * lerp(0.88, 1.12, flow.r);
                return float4(_Color.rgb * light, saturate(body * 0.75));

            }
            ENDCG
        }
    }
}
