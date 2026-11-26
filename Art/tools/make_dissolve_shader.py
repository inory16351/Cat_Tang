# CatTangToon.shader → CatTangToonDissolve.shader (엔딩 시계판 녹는 섬 아랫면: 아래로 갈수록 모래 알갱이 무늬로 디졸브)
import io, sys
src, dst = sys.argv[1], sys.argv[2]
s = io.open(src, encoding='utf-8').read()

def rep(old, new, count=1):
    global s
    n = s.count(old)
    if n < count:
        raise SystemExit(f'not found ({n}/{count}): {old[:60]!r}')
    s = s.replace(old, new) if count > 1 else s.replace(old, new, 1)

header = ('// 엔딩 스토리 맵 \'자정의 시계판\' 녹는 섬 아랫면 전용 (2026-11-26 사용자: 아래로 내려가면서 점점 파티클과 디졸브)\n'
          '//   CatTang/Toon 을 그대로 복사 + 오브젝트 높이(_DissolveTop → _DissolveBottom)를 따라 모래 알갱이 무늬로 뚫려 사라짐 · 사라지는 가장자리는 빛나는 모래빛\n'
          '//   원본 툰을 고치면 Art/tools/make_dissolve_shader.py 로 다시 만듦\n')
s = header + s
rep('Shader "CatTang/Toon"', 'Shader "CatTang/ToonDissolve"')
# 속성
rep('    Properties\n    {\n', '    Properties\n    {\n'
    '        _DissolveTop ("Dissolve Top (object Y)", Float) = -2.5\n'
    '        _DissolveBottom ("Dissolve Bottom (object Y)", Float) = -11.5\n'
    '        _DissolveScale ("Dissolve Grain Scale", Float) = 1.3\n'
    '        _DissolveEdge ("Dissolve Edge Width", Float) = 0.12\n'
    '        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1, 0.82, 0.5, 1)\n')
rep('            half _KeepColor;\n        CBUFFER_END',
    '            half _KeepColor;\n            float _DissolveTop, _DissolveBottom, _DissolveScale, _DissolveEdge;\n            half4 _DissolveEdgeColor;\n        CBUFFER_END')
# 공용 함수 (HLSLINCLUDE 끝)
funcs = '''
        // ---- 디졸브: 오브젝트 공간 위치 기준 (섬이 무너져 움직여도 무늬가 따라감) ----
        float DHash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
        float DNoise(float3 x)
        {
            float3 i = floor(x); float3 f = frac(x); f = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(lerp(DHash(i), DHash(i + float3(1, 0, 0)), f.x), lerp(DHash(i + float3(0, 1, 0)), DHash(i + float3(1, 1, 0)), f.x), f.y),
                        lerp(lerp(DHash(i + float3(0, 0, 1)), DHash(i + float3(1, 0, 1)), f.x), lerp(DHash(i + float3(0, 1, 1)), DHash(i + float3(1, 1, 1)), f.x), f.y), f.z);
        }
        float DissolveT(float3 pos) { return saturate((_DissolveTop - pos.y) / max(_DissolveTop - _DissolveBottom, 0.01)); }
        // > 0 = 남김. 큰 덩어리 무늬 + 잔 모래 알갱이 (모래가 알갱이째 떨어져 나가는 느낌)
        float DissolveValue(float3 pos)
        {
            float3 q = pos * _DissolveScale;
            float n = DNoise(q) * 0.55 + DNoise(q * 3.3 + 7.1) * 0.3 + DHash(floor(q * 11.0)) * 0.15;
            float t = DissolveT(pos);
            return n + 0.02 - t * 1.08;
        }
        void DissolveClip(float3 pos) { clip(DissolveValue(pos)); }
        half3 DissolveGlow(half3 c, float3 pos)
        {
            float v = DissolveValue(pos);
            half e = 1.0h - saturate(v / max(_DissolveEdge, 1e-3));
            return lerp(c, _DissolveEdgeColor.rgb, e * e * _DissolveEdgeColor.a * saturate(DissolveT(pos) * 6.0));
        }
        ENDHLSL'''
rep('            return lerp(rgb, to, w * _RecolorAmount);\n        }\n        ENDHLSL',
    '            return lerp(rgb, to, w * _RecolorAmount);\n        }\n' + funcs)
# 1. 본체
rep('                float fogFactor : TEXCOORD3;\n', '                float fogFactor : TEXCOORD3;\n                float3 positionOS : TEXCOORD4;\n')
rep('                o.fogFactor = ComputeFogFactor(p.positionCS.z);\n', '                o.fogFactor = ComputeFogFactor(p.positionCS.z);\n                o.positionOS = v.positionOS.xyz;\n')
rep('                UNITY_SETUP_INSTANCE_ID(i);\n                if (_CatTangBrawl > 0.5) return FragBrawl(i);',
    '                UNITY_SETUP_INSTANCE_ID(i);\n                DissolveClip(i.positionOS);\n                if (_CatTangBrawl > 0.5) return FragBrawl(i);')
rep('                color = Desat(color);\n', '                color = DissolveGlow(color, i.positionOS);\n                color = Desat(color);\n', count=2)
# 2. 외곽선 · 바깥 테두리
rep('                float fogFactor : TEXCOORD0;\n            };', '                float fogFactor : TEXCOORD0;\n                float3 positionOS : TEXCOORD1;\n            };', count=2)
rep('                o.fogFactor = ComputeFogFactor(posCS.z);\n', '                o.fogFactor = ComputeFogFactor(posCS.z);\n                o.positionOS = v.positionOS.xyz;\n', count=2)
rep('            {\n                half3 oc = ', '            {\n                DissolveClip(i.positionOS);\n                half3 oc = ')
rep('            {\n                return half4(MixFog(_OuterOutlineColor.rgb', '            {\n                DissolveClip(i.positionOS);\n                return half4(MixFog(_OuterOutlineColor.rgb')
# 3~5. 그림자 · 깊이 · 깊이+법선: 같은 구멍
a = s.index('        // ---------- 3. 그림자 만들기 ----------')
b = s.index('    FallBack')
tail = '''        // ---------- 3. 그림자 만들기 (디졸브 구멍 그대로) ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = posCS;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { DissolveClip(i.positionOS); return 0; }
            ENDHLSL
        }

        // ---------- 4. 깊이 ----------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { DissolveClip(i.positionOS); return 0; }
            ENDHLSL
        }

        // ---------- 5. 깊이 + 법선 ----------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionOS : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { DissolveClip(i.positionOS); return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }

'''
s = s[:a] + tail + s[b:]
io.open(dst, 'w', encoding='utf-8', newline='').write(s)
print('ok', dst)

# ---- 2차: 모래 질감 (2026-11-26 사용자: 오브젝트가 모래 질감이 아니라 이질적) ----
s = io.open(dst, encoding='utf-8').read()
def rep2(old, new, count=1):
    global s
    n = s.count(old)
    if n < count: raise SystemExit(f'not found ({n}/{count}): {old[:60]!r}')
    s = s.replace(old, new) if count > 1 else s.replace(old, new, 1)
rep2('        _DissolveTop ("Dissolve Top (object Y)", Float) = -2.5\n',
     '        _SandGrain ("Sand Grain Strength", Range(0, 1)) = 0.22\n        _SandScale ("Sand Grain Scale", Float) = 26\n        _DissolveTop ("Dissolve Top (object Y)", Float) = -2.5\n')
rep2('            float _DissolveTop, _DissolveBottom, _DissolveScale, _DissolveEdge;\n',
     '            float _DissolveTop, _DissolveBottom, _DissolveScale, _DissolveEdge, _SandGrain, _SandScale;\n')
rep2('        void DissolveClip(float3 pos) { clip(DissolveValue(pos)); }\n',
     '''        void DissolveClip(float3 pos) { clip(DissolveValue(pos)); }
        // 모래 질감: 잔 알갱이 밝고 어두운 점 + 큰 얼룩 + 가로 결(쌓인 모래 층) — 오브젝트 공간이라 움직여도 붙어 있음
        half3 SandTex(half3 c, float3 pos)
        {
            float3 q = pos * _SandScale;
            float g = DHash(floor(q)) - 0.5;                                     // 알갱이 (약 4cm)
            float g2 = DHash(floor(q * 2.3 + 3.7)) - 0.5;                        // 더 잔 알갱이
            float blot = DNoise(pos * 1.7) - 0.5;                                // 큰 얼룩
            float strata = sin(pos.y * 9.0 + DNoise(pos * 0.9) * 6.0) * 0.5;     // 가로 결
            half k = (half)(g * 0.55 + g2 * 0.35 + blot * 0.6 + strata * 0.25);
            return saturate(c * (1.0h + k * _SandGrain * 1.6h));
        }
''')
rep2('                half3 albedo = HsvToRgb(hsv);\n', '                half3 albedo = SandTex(HsvToRgb(hsv), i.positionOS);\n')
rep2('                half3 shadeCol = HsvToRgb(', '                half3 shadeCol = SandTex(HsvToRgb(')
rep2('hsv.z * (actor ? 0.62 : 0.62)));\n', 'hsv.z * (actor ? 0.62 : 0.62))), i.positionOS);\n')
rep2('                half3 albedo = lerp(dot(tex.rgb, half3(0.299, 0.587, 0.114)).xxx, tex.rgb, _Saturation) * _Brightness;\n',
     '                half3 albedo = SandTex(lerp(dot(tex.rgb, half3(0.299, 0.587, 0.114)).xxx, tex.rgb, _Saturation) * _Brightness, i.positionOS);\n')
io.open(dst, 'w', encoding='utf-8', newline='').write(s)
print('sand texture added')
