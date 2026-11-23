// 1장 보스 달리오스 '시간 지연 구역' 돔 (2026-11-23 사용자: 히오스 제라툴 궁처럼 구역을 덮는 돔)
// _Solid 0 = 껍질: 옅은 보랏빛 반투명 + 계단진 림(만화풍) + 위에서 아래로 천천히 가라앉는 띠(시간이 느려짐) + 바닥 테두리
// _Solid 1 = 뼈대 · 시계판 · 바늘 · 구슬: 단색 빛 (_Color), 가장자리만 살짝 어둡게
// 꼭대기 시계판 (2026-11-23 사용자: 돔 윗원을 시계처럼 써서 지속 시간 표시): v > _CapV 영역이 시계판.
//   12시(+Z)부터 시계 방향으로 지난 시간은 짙게, 남은 시간(_Remain 0~1)은 밝은 부채꼴. 테두리 안쪽에 눈금 12개. 바늘은 TimeDome이 같은 각도로 돌림
// _Fade 0~1 전체 투명도 (TimeDome이 나타남 · 사라짐에 씀), _Flash 0~1 흰 번쩍임 (착지 · 폭발 순간)
Shader "CatTang/TimeDome"
{
    Properties
    {
        _Color ("Color", Color) = (0.62, 0.42, 1, 1)
        _RimColor ("Rim Color", Color) = (0.92, 0.8, 1, 1)
        _Alpha ("Base Alpha", Range(0, 1)) = 0.16
        _Solid ("Solid Part", Range(0, 1)) = 0
        _BandSpeed ("Band Speed", Float) = 0.18
        _Fade ("Fade", Range(0, 1)) = 1
        _Flash ("Flash", Range(0, 1)) = 0
        _Remain ("Remain", Range(0, 1)) = 1
        _CapV ("Clock Cap V", Range(0, 1)) = 0.6
        _CapColor ("Clock Remain Color", Color) = (1, 0.78, 0.3, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "TimeDome"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _RimColor, _CapColor;
                half _Alpha, _Solid, _BandSpeed, _Fade, _Flash, _Remain, _CapV;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half facing = saturate(abs(dot(N, V)));
                if (_Solid > 0.5)
                {
                    half3 c = _Color.rgb * lerp(0.7h, 1.15h, step(0.35h, facing));
                    c = lerp(c, 1.0h, _Flash);
                    return half4(c, _Fade * _Color.a);
                }
                // 계단진 림: 두 칸
                half r = 1.0h - facing;
                half rim = step(0.55h, r) * 0.45h + step(0.8h, r) * 0.55h;
                // 가라앉는 띠: v가 시간에 따라 아래로 흐름 (느리게), 띠 굵기 일정
                half v = i.uv.y;
                half bandPos = frac(v * 4.0 + _Time.y * _BandSpeed);
                half band = step(0.86h, bandPos) * (1.0h - v * 0.6h);
                // 바닥 테두리 (짙은 보라 → 밝은 선)
                half baseLine = step(v, 0.035h);
                half3 col = _Color.rgb;
                col = lerp(col, _RimColor.rgb, saturate(rim + band * 0.7h + baseLine));
                half a = _Alpha + rim * 0.45h + band * 0.22h + baseLine * 0.6h;
                // 꼭대기 시계판: u = 둘레 (12시 = 0.75, 시계 방향으로 줄어듦)
                if (v > _CapV)
                {
                    half p = frac(0.75h - i.uv.x);                          // 12시 0 → 시계 방향 1
                    half remain = step(1.0h - _Remain, p) * step(0.001h, _Remain);
                    half tick = step(frac(p * 12.0h + 0.03h), 0.06h) * step(v, _CapV + 0.07h);
                    col = lerp(_Color.rgb * 0.28h, _CapColor.rgb, remain);   // 지난 시간 = 짙은 보라 · 남은 시간 = 밝은 금빛 흰색
                    col = lerp(col, 1.0h, tick);
                    a = lerp(0.3h, 0.48h, remain) + tick * 0.5h;            // 안의 고양이가 비쳐 보이게 (너무 진하면 가려짐)
                }
                col = lerp(col, 1.0h, _Flash * 0.8h);
                a += _Flash * 0.4h;
                return half4(col, saturate(a) * _Fade);
            }
            ENDHLSL
        }
    }
}
