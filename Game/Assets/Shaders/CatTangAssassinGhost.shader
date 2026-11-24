// 거울 망토 자객 잔상 (2026-11-24): 검은 몸 + 보라 림 + 노이즈 디졸브(경계 띠 밝은 보라 광)
//   _Dissolve 0 = 다 보임, 1 = 다 사라짐. 나타날 때 1 → 0, 사라질 때 0 → 1 (CostumeFx.ShadowStrike가 MaterialPropertyBlock으로)
//   돌진 잔상(ShadowGhost)은 같은 셰이더 · ZWrite Off 복사본, _Color 알파 · _Dissolve로 걷힘
//   블렌더 미리보기 Art/tools/blender_fx_costume.py ghost_tex와 같은 규칙
Shader "CatTang/AssassinGhost"
{
    Properties
    {
        _Color ("Body", Color) = (0.07, 0.04, 0.12, 0.95)
        _RimColor ("Rim", Color) = (0.7, 0.58, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _EdgeColor ("Dissolve Edge", Color) = (0.89, 0.83, 1, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.07
        _NoiseScale ("Noise Scale", Float) = 9
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1                 // 돌진 잔상(겹겹이 반투명)은 Off
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color, _RimColor, _EdgeColor;
            float _RimPower, _Dissolve, _EdgeWidth, _NoiseScale;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float3 op : TEXCOORD2; };

            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float vnoise(float3 x)
            {
                float3 i = floor(x); float3 f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(hash(i), hash(i + float3(1,0,0)), f.x), lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x), lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            float fbm(float3 p) { return 0.55 * vnoise(p) + 0.3 * vnoise(p * 2.1) + 0.15 * vnoise(p * 4.3); }

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = normalize(WorldSpaceViewDir(v.vertex));
                o.op = v.vertex.xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float nz = fbm(i.op * _NoiseScale);
                clip(nz - _Dissolve * 1.02);
                float rim = pow(1 - saturate(abs(dot(normalize(i.n), normalize(i.v)))), _RimPower);
                float3 c = lerp(_Color.rgb, _RimColor.rgb, saturate(rim * 1.3));
                float edge = (_Dissolve > 0.001 && nz < _Dissolve + _EdgeWidth) ? 1 : 0;
                c = lerp(c, _EdgeColor.rgb, edge);
                return float4(c, _Color.a);
            }
            ENDCG
        }
    }
}
