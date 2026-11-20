// 스타일 VFX 셰이더 (2026-11-20, 사용자가 준 블렌더 VFX 영상 기법을 유니티로 옮김: "같은 노드 5개로 90%")
//  1) 패턴 = 절차 노이즈 또는 보로노이 (텍스처 없음). 4D W를 움직이지 않고 매핑 Location(= UV 오프셋)을 흘려서 움직임 → 싸고 엔진으로 옮기기 쉬움
//  2) 컬러 램프 = 계단식 3색 (심지 흰빛 → 몸 색 → 테두리 색) + 사라지는 경계에 얇은 잉크 띠 → 툰 이펙트
//  3) 나이(_Age 0~1)를 문턱값으로 → 구멍이 뚫리며 사라짐 (영상의 Attribute 'age' → threshold)
//  4) 돔 · 구는 프레넬 테두리로 빛남, 띠 모양(고리 · 베기 궤적)은 V 가장자리를 가늘게 깎아 끝이 뾰족해짐
// 메시: Resources/FX/Meshes5/fx5_*.fbx (블렌더, U = 흐르는 방향 · V = 가로 방향). 값은 StylizedFx가 MaterialPropertyBlock으로 넣음.
// 재질 하나는 단순하게, 화려함은 여러 겹을 타이밍 · 색으로 겹쳐서 (영상: less is more per material, layering makes the difference)
Shader "CatTang/StylizedVfx"
{
    Properties
    {
        _ColCore ("Core (HDR)", Color) = (2.2, 2.0, 2.2, 1)
        _ColMid ("Body (HDR)", Color) = (1.6, 0.25, 1.1, 1)
        _ColEdge ("Edge", Color) = (0.35, 0.12, 0.7, 1)
        _ColInk ("Ink", Color) = (0.08, 0.03, 0.14, 1)
        _Pattern ("Pattern 0 Noise / 1 Voronoi / 2 Stripes", Float) = 0
        _Scale ("Pattern Scale (U, V)", Vector) = (6, 2, 0, 0)
        _Scroll ("Scroll (U/s, V/s)", Vector) = (-1.2, 0, 0, 0)
        _Offset ("Offset (스크립트가 넣는 매핑 Location)", Vector) = (0, 0, 0, 0)
        _Age ("Age 0~1 (문턱값)", Range(0, 1)) = 0
        _Band1 ("Core Band", Range(0, 1)) = 0.32
        _Band2 ("Body Band", Range(0, 1)) = 0.14
        _Ink ("Ink Band", Range(0, 0.2)) = 0.035
        _TaperV ("Taper V edges", Range(0, 0.5)) = 0.22
        _TaperU ("Taper U ends", Range(0, 0.5)) = 0
        _Fresnel ("Fresnel Rim", Range(0, 3)) = 0
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Opacity ("Opacity (0 = 더해지는 빛 · 1 = 덮는 색)", Range(0, 1)) = 0.3
        _Sweep ("Sweep (U 0~1 중 보이는 부분 = 그어지는 머리)", Range(0, 1.2)) = 1.2
        _TailFade ("Tail Fade (머리에서 멀수록 빨리 흩어짐)", Range(0, 3)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha          // 미리 곱한 알파: _Opacity 0이면 빛이 더해지고(에너지), 1이면 덮음(연기 · 유리)
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColCore, _ColMid, _ColEdge, _ColInk;
                float _Pattern;
                float4 _Scale, _Scroll, _Offset;
                half _Age, _Band1, _Band2, _Ink, _TaperV, _TaperU, _Fresnel, _Alpha, _Opacity, _Sweep, _TailFade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 positionWS : TEXCOORD2; };

            float2 Hash22(float2 p) { p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))); return frac(sin(p) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p), u = f * f * (3 - 2 * f);
                float a = Hash22(i).x, b = Hash22(i + float2(1, 0)).x, c = Hash22(i + float2(0, 1)).x, d = Hash22(i + float2(1, 1)).x;
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            float Fbm(float2 p) { return Noise(p) * 0.6 + Noise(p * 2.03 + 7.1) * 0.3 + Noise(p * 4.1 + 3.3) * 0.1; }
            // 보로노이 = 세포 경계선 (F2 − F1이 작을수록 1) → 점박이가 아니라 깨진 유리 · 금 간 그물 무늬 (2026-11-20: 세포 안쪽을 쓰면 표범 무늬처럼 보여 바꿈)
            float Voronoi(float2 p)
            {
                float2 ip = floor(p), fp = frac(p); float d1 = 8, d2 = 8;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y); float2 o = Hash22(ip + g); float d = length(g + o - fp);
                    if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
                }
                return saturate(1 - (d2 - d1) * 2.2) * 0.85 + Noise(p * 0.5) * 0.15;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                float2 q = i.uv * _Scale.xy + _Offset.xy + _Scroll.xy * _Time.y;
                float n = _Pattern < 0.5 ? Fbm(q) : _Pattern < 1.5 ? Voronoi(q) : 0.5 + 0.5 * sin((q.x + Noise(q * 0.7) * 1.5) * 6.2832);
                float taper = 1;
                if (_TaperV > 0) taper *= smoothstep(0, _TaperV, i.uv.y) * smoothstep(1, 1 - _TaperV, i.uv.y);
                if (_TaperU > 0) taper *= smoothstep(0, _TaperU, frac(i.uv.x)) * smoothstep(1, 1 - _TaperU, frac(i.uv.x));
                float v = n * taper;
                float3 N = normalize(i.normalWS) * (front ? 1 : -1);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float rim = pow(1 - saturate(abs(dot(N, V))), 2.5) * _Fresnel;
                v = saturate(v + rim * 0.6);
                // 그어지는 머리: U가 _Sweep보다 앞이면 아직 안 그어짐, 머리에서 멀수록(꼬리) 문턱이 높아져 먼저 흩어짐
                float behind = _Sweep - i.uv.x;
                clip(behind + 0.0001);
                float headGlow = saturate(1 - behind * 6);                      // 머리 바로 뒤가 가장 밝음
                float k = v - _Age - behind * _TailFade + headGlow * 0.12;
                clip(k);
                half4 c = k > _Band1 ? _ColCore : k > _Band2 ? _ColMid : k > _Ink ? _ColEdge : _ColInk;
                c.rgb += _ColMid.rgb * rim * 0.5 + _ColCore.rgb * headGlow * 0.35;
                // 가장자리(잉크 띠)는 덮는 이펙트에서만 — 빛 이펙트는 테두리가 부드럽게 사라짐
                half a = _Alpha * (k > _Ink ? 1 : _Opacity * 0.85);
                half soft = saturate(k / max(_Ink * 2, 0.01));                  // 문턱 근처는 옅게 (덩어리 경계가 딱딱하지 않게)
                a *= lerp(soft, 1, _Opacity);
                return half4(c.rgb * a, a * _Opacity);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
