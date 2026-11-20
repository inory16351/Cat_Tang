// 5장 바닥 '무너지는 시간의 끝' (URP)
// - v6 시간 균열 (2026-11-20 사용자: 시간의 뒤틀림과 균열이 안 보임 / 보라 별로 → 푸른 계열 몽환적): 조각 = 시간의 허공 위에 떠 있는 깨진 판.
//   경계마다 폭이 다른 틈(가는 금 ~ 1m)으로 시차가 있는 허공(별 두 겹 · 성운 · 떠도는 시계 눈금 고리)이 보이고, 판 가장자리에 새는 청록 빛이 판마다 다른 박자로 초마다 똑딱.
//   24m마다 확률로 시계 소용돌이(안쪽 공간 · 판 모양이 천천히 감겼다 풀림 + 눈금 고리 · 20초 초침), 일부 판은 무늬 잔상이 밀렸다 돌아옴
// - v5 조각 (2026-11-20 사용자: 어지러운 패턴을 한 그림에 섞으니 가시성만 떨어짐 → 패턴 하나씩 담은 바닥 여러 장을 불규칙한 도형으로 합쳐 섞음):
//   패턴 하나씩 담은 조용한 바닥 6장(_BaseMap + _Tex2~6, Art/tools/ground_texture_ch5.py)을 가중 보로노이 다각형 조각(곧은 변, 크기 제각각)마다 하나씩.
//   패턴은 넓은 노이즈로 뭉치게 골라 같은 패턴 이웃 조각은 이음매 없이 합쳐짐. 패턴이 바뀌는 변 = 가는 금 균열 또는 별이 보이는 허공 틈
// - 시간 · 차원이 뒤틀린 느낌 (가시성 해치지 않는 선에서): 일부 조각 = 깨진 거울(잔조각마다 어긋난 하늘 반사 · 잔금), 일부 = 가라앉은 조각(무늬 어긋남 · 가장자리 그림자)
// - 주의: 무작위 값은 정수 칸 번호 + 정수 오프셋으로만 해시 (소수 오프셋은 컴파일러 상수 접기로 칸마다 반올림이 달라져 칸 경계에 직선 이음매가 생김)
// - 그림 속 금빛(노랑 · 금색 = R · G 높고 B 낮음)은 HDR로 맥동, 아주 어두운 곳은 별이 반짝임, 일렁임은 아주 약하게만
// - 툰 1단 명암 + 주 광원 그림자 받음 (CatTang/Toon 브롤풍 바닥과 같은 문턱)
Shader "CatTang/RiftFloor"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _WarpAmount ("Warp Amount (UV)", Range(0, 0.05)) = 0.012
        _WarpFreq ("Warp Frequency", Float) = 1.7
        _WarpSpeed ("Warp Speed", Float) = 0.45
        _SwirlScale ("Swirl Region Scale (1/m)", Float) = 0.09
        _GlowColor ("Crack Glow Color", Color) = (1, 0.78, 0.3, 1)
        _Glow ("Crack Glow (HDR x)", Range(0, 4)) = 1.6
        _GlowSpeed ("Glow Flow Speed", Float) = 1.4
        _StarTwinkle ("Void Star Twinkle", Range(0, 2)) = 0.8
        _ShadeDark ("Shade Darkness", Range(0, 1)) = 0.62
        [Header(Readability)]
        _Desat ("Desaturate (0 = 그림 그대로)", Range(0, 1)) = 0.5
        _Darken ("Brightness x", Range(0, 1.5)) = 0.78
        _Tint ("Cool Tint (채도 뺀 뒤 곱함)", Color) = (0.82, 0.8, 1.0, 1)
        _Contrast ("Local Contrast (1 = 그림 그대로)", Range(0, 1)) = 0.6
        _Soft ("Detail Softness (mip bias)", Range(0, 4)) = 1.6
        _Mid ("Mid Value (명암 압축 기준)", Range(0, 1)) = 0.24
        [Header(Chunks)]
        // 2026-11-20 사용자: 어지러운 패턴을 한 그림에 섞으니 가시성만 떨어짐 → 패턴 하나씩만 담은 바닥 그림 여러 장을
        // 크기가 제각각인 네모 청크로 불규칙하게 섞음 (8m 칸이 4m · 2m로 무작위로 쪼개짐). 청크 이음매 = 가는 금 균열
        _Tex2 ("Chunk 2", 2D) = "black" {}
        _Tex3 ("Chunk 3", 2D) = "black" {}
        _Tex4 ("Chunk 4", 2D) = "black" {}
        _Tex5 ("Chunk 5", 2D) = "black" {}
        _Tex6 ("Chunk 6", 2D) = "black" {}
        _ChunkCount ("Chunk Texture Count (1~6)", Range(1, 6)) = 6
        _ChunkSize ("Shard Spacing (m, 조각 평균 간격)", Float) = 5
        _ChunkSplit ("Split Chance (작은 칸으로 쪼개질 확률)", Range(0, 1)) = 0.55
        _ChunkTile ("Texture Size (m, 그림 한 장이 덮는 넓이)", Float) = 6
        _SeamWidth ("Seam Width (m)", Range(0, 0.2)) = 0.05
        _SeamGlow ("Seam Glow", Range(0, 1)) = 0.6
        [Header(Rift)]
        // 2026-11-20 사용자: 깨진 거울 같은 것도, 시간과 차원이 뒤틀린 공간 느낌 — 가시성을 해치지 않는 선에서
        _MirrorChance ("Mirror Shard Chance", Range(0, 0.5)) = 0.12
        _MirrorDark ("Mirror Dark", Color) = (0.34, 0.34, 0.52, 1)
        _MirrorLight ("Mirror Light", Color) = (0.62, 0.6, 0.8, 1)
        _MirrorFacet ("Mirror Facet Size (m)", Float) = 1.1
        _SinkChance ("Sunk Shard Chance", Range(0, 0.6)) = 0.28
        _SinkShade ("Sunk Edge Shadow", Range(0, 1)) = 0.45
        _VoidSeam ("Void Seam Chance", Range(0, 1)) = 0.35
        _VoidColor ("Void Color", Color) = (0.08, 0.06, 0.18, 1)
        [Header(Time Rift v6)]
        // 2026-11-20 사용자: 시간의 뒤틀림과 균열이 안 보임 → 허공 위에 떠 있는 깨진 판 · 틈으로 보이는 깊은 시간의 허공(시차) ·
        // 판 가장자리에서 새는 빛(초마다 똑딱) · 천천히 도는 시계 소용돌이(안쪽 바닥이 휨) · 시간 잔상
        _GapChance ("Wide Gap Chance", Range(0, 1)) = 0.45
        _GapMax ("Wide Gap Width (m)", Range(0, 1)) = 0.55
        _VoidDepth ("Void Depth (m, 시차)", Range(0, 10)) = 3.5
        _RimWidth ("Rim Light Width (m)", Range(0, 0.2)) = 0.05
        _TickFlash ("Tick Flash", Range(0, 1)) = 0.6
        _VortexChance ("Clock Vortex Chance", Range(0, 1)) = 0.55
        _VortexTwist ("Vortex Twist", Range(0, 2)) = 0.9
        _EchoChance ("Time Echo Chance", Range(0, 1)) = 0.3
        [Header(Prime Meridian)]
        // 2026-11-20 사용자: 천문대 컨셉, 시간은 유지 (그리니치 = 표준시) → 방들을 남북으로 꿰는 놋쇠 본초 자오선. 판마다 어긋나 끊김 = 깨진 표준시
        _MeridianX ("Meridian World X", Float) = 0
        _MeridianWidth ("Meridian Width (m)", Range(0, 0.5)) = 0.16
        _MeridianShift ("Meridian Break Offset (m)", Range(0, 1)) = 0.35
        _MeridianColor ("Meridian Brass", Color) = (0.95, 0.74, 0.38, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_Tex2); TEXTURE2D(_Tex3); TEXTURE2D(_Tex4); TEXTURE2D(_Tex5); TEXTURE2D(_Tex6);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _GlowColor;
            half _WarpAmount, _WarpFreq, _WarpSpeed, _SwirlScale, _Glow, _GlowSpeed, _StarTwinkle, _ShadeDark, _Desat, _Darken, _Contrast, _Soft, _Mid;
            half4 _Tint;
            half _ChunkCount, _ChunkSplit, _SeamWidth, _SeamGlow;
            half4 _MirrorDark, _MirrorLight, _VoidColor;
            half _MirrorChance, _MirrorFacet, _SinkChance, _SinkShade, _VoidSeam;
            float _MeridianX; half _MeridianWidth, _MeridianShift; half4 _MeridianColor;
            half _GapChance, _GapMax, _VoidDepth, _RimWidth, _TickFlash, _VortexChance, _VortexTwist, _EchoChance;
            float _ChunkSize, _ChunkTile;
        CBUFFER_END

        float Hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
        float Noise2(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float2 u = f * f * (3 - 2 * f);
            return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x), lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x), u.y);
        }
        // 조각(가중 보로노이 점): 칸 c의 점 위치 · 가중치(크기) · 정체(같으면 합쳐짐)
        float2 ShardPoint(float2 c, float cs) { return (c + 0.5 + (float2(Hash21(c + float2(13, 71)), Hash21(c + float2(79, 23))) - 0.5) * 0.85) * cs; }
        float ShardWeight(float2 c, float cs) { float w = Hash21(c + float2(44, 91)); return w * w * cs * cs * 0.55 * _ChunkSplit; }
        // 정체: 0~5 = 패턴(뭉치게 고름), +10 = 가라앉은 조각(따로 둠), 100+ = 깨진 거울(조각마다 따로)
        // 무작위 값은 정수 칸 번호 c로만 뽑음 (점 위치 같은 소수로 해시하면 GPU 반올림 차이로 칸 경계마다 값이 튐)
        float ShardId(float2 c, float2 pnt)
        {
            if (Hash21(c + float2(7, 101)) < _MirrorChance) return 100.0 + Hash21(c + float2(53, 17));
            float pick = frac(Hash21(c + float2(31, 73)) * 0.45 + Noise2(c * (_ChunkSize * 0.045) + 3.3) * 1.6);   // 넓은 노이즈도 칸 번호로 (뭉침)
            float idx = min(_ChunkCount - 1, floor(pick * _ChunkCount));
            return idx + (Hash21(c + float2(92, 29)) < _SinkChance ? 10.0 : 0.0);
        }
        half3 SamplePat(int idx, float2 uv, float bias)
        {
            if (idx == 0) return SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, uv, bias).rgb;
            if (idx == 1) return SAMPLE_TEXTURE2D_BIAS(_Tex2, sampler_BaseMap, uv, bias).rgb;
            if (idx == 2) return SAMPLE_TEXTURE2D_BIAS(_Tex3, sampler_BaseMap, uv, bias).rgb;
            if (idx == 3) return SAMPLE_TEXTURE2D_BIAS(_Tex4, sampler_BaseMap, uv, bias).rgb;
            if (idx == 4) return SAMPLE_TEXTURE2D_BIAS(_Tex5, sampler_BaseMap, uv, bias).rgb;
            return SAMPLE_TEXTURE2D_BIAS(_Tex6, sampler_BaseMap, uv, bias).rgb;
        }
        // 시간의 허공: 짙은 남색 + 두 겹 별(깊이가 달라 시차가 다름) + 흐르는 성운 + 멀리 떠도는 시계 눈금 고리
        half3 VoidAt(float2 xz, float2 par, float t)
        {
            float2 a = xz + par * _VoidDepth;
            float2 b = xz + par * _VoidDepth * 2.2;
            half neb = Noise2(a * 0.25 + float2(t * 0.02, -t * 0.015)) * 0.6 + Noise2(a * 0.7 - t * 0.03) * 0.4;
            half3 col = _VoidColor.rgb * (0.7h + neb * 0.9h) + _GlowColor.rgb * smoothstep(0.6, 0.95, neb) * 0.08h;
            float2 c1 = floor(a * 2.5); float2 f1 = frac(a * 2.5) - 0.5;
            float s1 = step(0.9, Hash21(c1 + float2(5, 7))) * (1 - smoothstep(0.03, 0.1, length(f1))) * (0.6 + 0.4 * sin(t * 2.0 + Hash21(c1) * 20.0));
            float2 c2 = floor(b * 5.0); float2 f2 = frac(b * 5.0) - 0.5;
            float s2 = step(0.93, Hash21(c2 + float2(11, 3))) * (1 - smoothstep(0.02, 0.07, length(f2))) * 0.5;
            // 멀리 떠도는 시계 눈금 고리 (허공 속, 천천히 돎)
            float2 rc = floor(a / 7.0); float2 rcen = (rc + 0.5 + (float2(Hash21(rc + float2(2, 9)), Hash21(rc + float2(8, 1))) - 0.5) * 0.4) * 7.0;
            float2 rd = a - rcen; float rr = length(rd); float rR = 1.6 + Hash21(rc + float2(4, 4)) * 1.2;
            float ra = atan2(rd.y, rd.x) + t * 0.1 * (Hash21(rc + float2(6, 2)) - 0.5);
            float tick = step(0.82, frac(ra / 6.2832 * 12.0 + 0.09)) * (1 - smoothstep(0.0, 0.08, abs(rr - rR)));
            float ring = 1 - smoothstep(0.01, 0.035, abs(rr - rR * 0.86));
            col += (s1 * 0.9 + s2) * half3(0.85, 0.95, 1.0) + _GlowColor.rgb * (tick * 0.35 + ring * 0.15);
            return col;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float3 normalWS : TEXCOORD2; float fogFactor : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float t = _Time.y;
                float2 xz0 = i.positionWS.xz;
                // 허공 시차: 카메라 쪽 방향으로 깊이만큼 밀린 자리를 봄 → 판 밑으로 깊은 공간이 보임
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                float2 par = -V.xz / max(V.y, 0.25);

                // ---- 시계 소용돌이: 24m 칸마다 확률로 하나. 안쪽 공간이 천천히 비틀려 판 모양까지 휨 + 눈금 고리 · 초침 ----
                float2 vc = floor(xz0 / 24.0);
                float vh = Hash21(vc + float2(19, 43));
                float2 vcen = (vc + 0.5 + (float2(Hash21(vc + float2(3, 71)), Hash21(vc + float2(61, 5))) - 0.5) * 0.25) * 24.0;
                float vR = 3.2 + Hash21(vc + float2(27, 9)) * 2.3;
                float2 vd = xz0 - vcen; float vr = length(vd);
                bool vortex = vh < _VortexChance;
                float2 xz = xz0;
                float vInside = 0;
                if (vortex && vr < vR * 1.25)
                {
                    vInside = 1 - saturate(vr / (vR * 1.25));
                    float tw = vInside * vInside * _VortexTwist * sin(t * 0.22 + vh * 6.0);   // 천천히 감겼다 풀림
                    float cs0 = cos(tw), sn0 = sin(tw);
                    xz = vcen + float2(vd.x * cs0 - vd.y * sn0, vd.x * sn0 + vd.y * cs0);
                }

                // ---- 다각형 조각 (가중 보로노이), 같은 패턴 이웃은 합침 ----
                float cs = _ChunkSize;
                float2 gc = floor(xz / cs);
                float best = 1e9; float2 bp = 0; float2 bc = 0;
                [unroll] for (int oy = -2; oy <= 2; oy++)
                [unroll] for (int ox = -2; ox <= 2; ox++)
                {
                    float2 c = gc + float2(ox, oy);
                    float2 pnt = ShardPoint(c, cs);
                    float d = dot(xz - pnt, xz - pnt) - ShardWeight(c, cs);
                    if (d < best) { best = d; bp = pnt; bc = c; }
                }
                float sid = ShardId(bc, bp);
                bool mirror = sid >= 100.0;
                bool sunk = !mirror && sid >= 10.0;
                int idx = mirror ? 0 : (int)fmod(sid, 10.0);
                float edge = 1e9; float2 nc = bc;
                [unroll] for (int ey = -2; ey <= 2; ey++)
                [unroll] for (int ex = -2; ex <= 2; ex++)
                {
                    float2 c = gc + float2(ex, ey);
                    float2 pnt = ShardPoint(c, cs);
                    float2 dv = pnt - bp;
                    float len2 = dot(dv, dv);
                    if (len2 < 1e-4 || abs(ShardId(c, pnt) - sid) < 1e-3) continue;
                    float dj = dot(xz - pnt, xz - pnt) - ShardWeight(c, cs);
                    float e = (dj - best) / (2.0 * sqrt(len2));
                    if (e < edge) { edge = e; nc = c; }
                }

                // ---- 틈: 경계마다 폭이 다름 (가는 금 ~ 넓게 벌어진 틈). 양쪽 판이 반씩 깎임 ----
                float gh = Hash21(bc + nc + float2(37, 113));
                float gapW = gh < _GapChance ? lerp(0.18, _GapMax, Hash21(bc + nc + float2(71, 7))) : lerp(0.04, 0.09, gh);
                float jag = (Noise2(xz * 2.3) - 0.5) * gapW * 0.5 + (Noise2(xz * 9.0) - 0.5) * 0.02;   // 깨진 가장자리
                float e2 = edge + jag;
                float halfGap = gapW * 0.5;
                if (e2 < halfGap)
                {
                    // 허공: 판이 드리운 그림자(가장자리 가까울수록 어두움) + 시간의 허공
                    half3 vcol = VoidAt(xz0, par, t);
                    vcol *= lerp(0.35h, 1.0h, smoothstep(0.0, halfGap * 0.9, halfGap - e2));
                    half3 outc = MixFog(vcol, i.fogFactor);
                    return half4(outc, 1);
                }

                // ---- 판 무늬 ----
                float ang = Hash21(float2(idx, 2.0)) * 6.2832;
                float2 puv = xz / _ChunkTile;
                puv = float2(puv.x * cos(ang) - puv.y * sin(ang), puv.x * sin(ang) + puv.y * cos(ang)) + float2(Hash21(float2(idx, 5.0)), Hash21(float2(idx, 9.0))) * 7.0;
                if (sunk) puv += float2(0.025, -0.035);
                half3 raw = SamplePat(idx, puv, 0) * _BaseColor.rgb;
                half3 rawSoft = SamplePat(idx, puv, _Soft) * _BaseColor.rgb;
                // 시간 잔상: 일부 판은 무늬가 천천히 밀렸다 돌아오는 옅은 겹상
                bool echo = !mirror && Hash21(bc + float2(13, 131)) < _EchoChance;
                if (echo)
                {
                    float ep = Hash21(bc + float2(29, 3)) * 6.2832;
                    float2 eo = float2(cos(ep), sin(ep)) * (0.06 + 0.05 * sin(t * 0.7 + ep)) ;
                    half3 ghost = SamplePat(idx, puv + eo, _Soft);
                    rawSoft = lerp(rawSoft, ghost * 1.25h + _GlowColor.rgb * 0.06h, 0.35h);
                }
                half3 flat = lerp(_Mid.xxx, rawSoft, _Contrast);
                half3 tex = lerp(flat, dot(flat, half3(0.299h, 0.587h, 0.114h)).xxx, _Desat) * _Tint.rgb * _Darken;
                if (mirror)
                {
                    float2 mq = (xz - bp) / _MirrorFacet;
                    float2 mg = floor(mq); float f1 = 9, f2 = 9; float2 fid = 0;
                    [unroll] for (int my = -1; my <= 1; my++)
                    [unroll] for (int mx = -1; mx <= 1; mx++)
                    {
                        float2 mc = mg + float2(mx, my);
                        float2 mpt = mc + float2(Hash21(mc + bc * 17.0), Hash21(mc + bc * 17.0 + float2(33, 5)));
                        float md = length(mq - mpt);
                        if (md < f1) { f2 = f1; f1 = md; fid = mc; } else if (md < f2) f2 = md;
                    }
                    float fh = Hash21(fid + bc * 17.0 + float2(3, 37));
                    // 거울 = 잔조각마다 어긋난 하늘 + 그 속에 비친 시간의 허공(별)
                    float2 refl = xz * 0.09 + float2(fh, Hash21(fid + float2(17, 61))) * 0.8 + float2(t * 0.025, t * 0.012);
                    float sky = Noise2(refl) * 0.65 + Noise2(refl * 2.7 + 5.1) * 0.35;
                    half3 mcol = lerp(_MirrorDark.rgb, _MirrorLight.rgb, smoothstep(0.25, 0.8, sky));
                    mcol = lerp(mcol, VoidAt(xz0 + float2(fh, fh * 1.7) * 3.0, -par, t) * 1.6h, 0.35h);
                    mcol *= 0.92 + fh * 0.16;
                    float crack = 1 - smoothstep(0.02, 0.06, f2 - f1);
                    mcol = lerp(mcol, _MirrorLight.rgb * 1.2, crack * 0.7);
                    tex = mcol;
                }
                if (sunk) tex *= 0.88h;

                // 본초 자오선: 놋쇠 띠 + 1m 눈금. 판마다 옆으로 어긋나서 이음매에서 끊김
                float mx = xz.x - _MeridianX - (Hash21(bc + float2(83, 47)) - 0.5) * 2.0 * _MeridianShift;
                float hw = _MeridianWidth * 0.5;
                float mLine = 1 - smoothstep(hw, hw + 0.02, abs(mx));
                float mEdge = (1 - smoothstep(hw + 0.02, hw + 0.06, abs(mx))) - mLine;     // 띠 양옆 어두운 홈
                float mTick = (1 - smoothstep(hw * 2.6, hw * 2.6 + 0.02, abs(mx))) * (1 - smoothstep(0.03, 0.05, abs(frac(xz.y) - 0.5)));
                float mTick5 = (1 - smoothstep(hw * 4.0, hw * 4.0 + 0.02, abs(mx))) * (1 - smoothstep(0.05, 0.07, abs(frac(xz.y / 5.0) - 0.5) * 5.0));
                half3 brass = _MeridianColor.rgb * (0.85h + 0.15h * Noise2(xz * 4.0));
                tex = lerp(tex, tex * 0.55h, mEdge);
                tex = lerp(tex, brass, saturate(mLine + max(mTick, mTick5) * 0.85));

                // 판 가장자리: 아래 허공에서 새어 나오는 빛 + 판마다 다른 박자로 초마다 똑딱 밝아짐
                float rim = 1 - smoothstep(halfGap, halfGap + _RimWidth, e2);
                float phase = Hash21(bc + float2(41, 17));
                float tick = exp(-frac(t + phase) * 5.0);
                half3 rimCol = _GlowColor.rgb * (0.55h + _TickFlash * tick);
                // 판 두께: 허공 쪽 아래 가장자리가 어둡게 (떠 있는 판)
                float lip = 1 - smoothstep(halfGap, halfGap + 0.18 + gapW * 0.4, e2);
                tex *= 1 - lip * (sunk ? _SinkShade : 0.25h);

                // 툰 1단 명암 + 그림자
                float3 N = normalize(i.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(N, mainLight.direction));
                half shadow = smoothstep(0.45h, 0.55h, mainLight.shadowAttenuation);
                half lit = smoothstep(0.27h, 0.31h, ndl * shadow);
                half3 color = lerp(tex * _ShadeDark, tex, lit) * mainLight.color + tex * SampleSH(N) * 0.25h;
                color = lerp(color, rimCol * _Glow, rim * 0.85h);

                // 시계 소용돌이 눈금 고리 · 초침 (옅게, 판 위에 빛으로)
                if (vortex && vr < vR * 1.3)
                {
                    float va = atan2(vd.y, vd.x);
                    float rot = t * 0.12 * (vh < _VortexChance * 0.5 ? 1 : -1);
                    float ticks60 = step(0.86, frac((va + rot) / 6.2832 * 60.0)) * (1 - smoothstep(0.0, 0.06, abs(vr - vR)));
                    float ticks12 = step(0.7, frac((va + rot) / 6.2832 * 12.0 + 0.15)) * (1 - smoothstep(0.0, 0.16, abs(vr - vR + 0.2)));
                    float ringL = 1 - smoothstep(0.012, 0.03, abs(vr - vR * 1.08));
                    float handA = frac(t / 20.0 + vh) * 6.2832;                            // 초침: 20초에 한 바퀴
                    float2 hd = float2(cos(handA), sin(handA));
                    float along = dot(vd, hd);
                    float hand = (1 - smoothstep(0.015, 0.045, abs(vd.x * hd.y - vd.y * hd.x))) * step(0, along) * step(along, vR * 0.95);
                    color += _GlowColor.rgb * (ticks60 * 0.35 + ticks12 * 0.5 + ringL * 0.3 + hand * 0.35) * _Glow * 0.6;
                }

                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(Attributes v) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(v); return TransformObjectToHClip(v.positionOS.xyz); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings vert(Attributes v) { Varyings o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 frag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
