// 5장 바닥 '무너지는 시간의 끝' (URP, 2026-11-19 사용자: 4장 바닥 재활용 말고 깨지고 일그러진 느낌을 훨씬 강하게)
// - 바닥 그림(Codex, 깨진 거울 판석 + 허공 틈 + 금 균열)을 그대로 쓰되 UV를 두 겹 물결 + 자리마다 다른 느린 소용돌이로 일렁이게 → 바닥이 녹아 일그러지는 느낌
//   (일렁임 세기는 월드 노이즈로 자리마다 달라서 반복 무늬가 덜 보임)
// - 금빛 균열(그림에서 노랑 · 금색 = R · G 높고 B 낮음)을 찾아 HDR로 맥동시킴 → 블룸에 빛나며 균열을 따라 빛이 흐름
// - 허공 틈(아주 어두운 남보라)은 별이 반짝임
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
        [Header(Patchwork)]
        // 2026-11-20 사용자: 바닥 이미지 · 재질을 새로, 여러 패턴을 불규칙하게 섞어서, 초현실주의 → 앞 장 바닥(대리석 체커 · 판자 · 역 타일 · 거울 사막)을
        // 녹아 흘러내린 듯 일그러진 경계로 군데군데 섞음. 경계는 금 균열 이음매. 색은 하나의 보랏빛 톤으로 묶음
        _Tex2 ("Patch 2 (4장 대리석)", 2D) = "black" {}
        _Tex3 ("Patch 3 (술집 판자)", 2D) = "black" {}
        _Tex4 ("Patch 4 (역 타일)", 2D) = "black" {}
        _Tex5 ("Patch 5 (거울 사막)", 2D) = "black" {}
        _PatchAmount ("Patch Amount (0 = 바탕만)", Range(0, 1)) = 0.5
        _PatchScale ("Patch Size (1/m, 작을수록 큼)", Float) = 0.07
        _PatchWarp ("Patch Boundary Melt", Range(0, 3)) = 1.4
        _SeamWidth ("Seam Width", Range(0, 0.1)) = 0.018
        _PatchTile ("Patch Tiling x (패턴마다 크기 다르게)", Vector) = (1.3, 0.8, 1.7, 1.1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_Tex2); TEXTURE2D(_Tex3); TEXTURE2D(_Tex4); TEXTURE2D(_Tex5);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _GlowColor;
            half _WarpAmount, _WarpFreq, _WarpSpeed, _SwirlScale, _Glow, _GlowSpeed, _StarTwinkle, _ShadeDark, _Desat, _Darken, _Contrast, _Soft, _Mid;
            half4 _Tint;
            half _PatchAmount, _PatchScale, _PatchWarp, _SeamWidth;
            float4 _PatchTile;
        CBUFFER_END

        float Hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
        float Noise2(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float2 u = f * f * (3 - 2 * f);
            return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x), lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x), u.y);
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
                float2 xz = i.positionWS.xz;

                // 자리마다 다른 일렁임 세기 (0.25 ~ 1.4배) + 두 겹 물결 + 느린 소용돌이
                float region = Noise2(xz * _SwirlScale + t * 0.03);
                float amt = _WarpAmount * lerp(0.25, 1.4, region * region);
                float2 uv = i.uv;
                float2 w1 = float2(sin(uv.y * 6.2832 * _WarpFreq + t * _WarpSpeed), sin(uv.x * 6.2832 * _WarpFreq * 1.31 - t * _WarpSpeed * 0.83));
                float2 w2 = float2(sin((uv.x + uv.y) * 6.2832 * _WarpFreq * 2.7 - t * _WarpSpeed * 1.7), cos((uv.x - uv.y) * 6.2832 * _WarpFreq * 2.3 + t * _WarpSpeed * 1.3)) * 0.45;
                // 느린 소용돌이 = 월드 노이즈로 미는 도메인 왜곡 (끊김 없이 이어짐)
                float2 swirl = float2(Noise2(xz * 0.15 + t * 0.05) - 0.5, Noise2(xz * 0.15 + 17.3 - t * 0.04) - 0.5);
                uv += (w1 + w2) * amt + swirl * amt * 3.0;

                half3 raw = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                half3 rawSoft = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, uv, _Soft).rgb * _BaseColor.rgb;
                // ---- 패치워크: 녹아 흘러내린 경계로 앞 장 바닥 4종을 섞음 (경계 = 도메인 왜곡한 저주파 노이즈의 문턱) ----
                float2 pq = xz * _PatchScale;
                float2 dw = float2(Noise2(pq * 1.9 + 3.1), Noise2(pq * 1.9 + 9.7)) - 0.5;
                pq += dw * _PatchWarp + float2(Noise2(pq * 4.3 + 1.7), Noise2(pq * 4.3 + 5.3)) * 0.18 * _PatchWarp;
                float pv = Noise2(pq) * 0.7 + Noise2(pq * 2.1 + 13.0) * 0.3;
                float th = 1 - _PatchAmount * 0.55;                                     // pv < th = 바탕(새 5장 그림), 위는 패치 4종을 나눠 가짐
                float seam = 0;
                if (pv >= th)
                {
                    float k = saturate((pv - th) / max(1e-3, 1 - th)) * 4;
                    int idx = min(3, (int)k);
                    float fr = frac(k);
                    seam = 1 - smoothstep(0, _SeamWidth * 6, min(fr, 1 - fr) * (1 - th));
                    // 패턴마다 크기 · 방향 · 흐름이 다름 (같은 체커도 휘어 보이게 왜곡 UV)
                    float2 puv = xz / 8.0 * _PatchTile[idx] + (w1 + w2) * amt * 4.0 + dw * 0.35;
                    float ang = idx * 0.9 + 0.4;
                    puv = float2(puv.x * cos(ang) - puv.y * sin(ang), puv.x * sin(ang) + puv.y * cos(ang));
                    half3 pc = idx == 0 ? SAMPLE_TEXTURE2D_BIAS(_Tex2, sampler_BaseMap, puv, _Soft * 0.6).rgb
                             : idx == 1 ? SAMPLE_TEXTURE2D_BIAS(_Tex3, sampler_BaseMap, puv, _Soft * 0.6).rgb
                             : idx == 2 ? SAMPLE_TEXTURE2D_BIAS(_Tex4, sampler_BaseMap, puv, _Soft * 0.6).rgb
                             :            SAMPLE_TEXTURE2D_BIAS(_Tex5, sampler_BaseMap, puv, _Soft * 0.6).rgb;
                    raw = pc; rawSoft = pc;
                }
                seam = max(seam, 1 - smoothstep(0, _SeamWidth, abs(pv - th)));            // 바탕 ↔ 패치 경계
                // 가시성 (2026-11-20 사용자: 색이 가시성이 떨어짐 — 보라 · 금 적이 보라 바닥에 묻힘 / 바닥 가시성 너무 안 좋음):
                //  ① 판석 무늬는 흐리게(밉 바이어스) 한 그림을 쓰고 ② 명암 차를 중간값 쪽으로 눌러(_Contrast) 잔무늬가 적 · 탄과 겹쳐 보이지 않게
                //  ③ 채도를 빼고 어두운 청회색으로 ④ 금 균열만 또렷한 원래 그림 · 원래 색 → 바닥은 조용한 배경, 금 균열만 장소 느낌
                half3 soft = rawSoft;
                half crackKeep = saturate((raw.r + raw.g * 0.8h - raw.b * 1.7h - 0.75h) * 3.0h) * step(pv, th);   // 앞 장 바닥의 노랑(흙 · 판자)은 균열로 안 봄
                half3 flat = lerp(_Mid.xxx, soft, _Contrast);                                        // 명암 압축
                half3 cool = lerp(flat, dot(flat, half3(0.299h, 0.587h, 0.114h)).xxx, _Desat) * _Tint.rgb * _Darken;
                half3 tex = lerp(cool, raw, crackKeep);
                tex = lerp(tex, _GlowColor.rgb * 0.9h, seam);                          // 패치 경계 = 금 균열 이음매

                // 툰 1단 명암 + 그림자
                float3 N = normalize(i.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(N, mainLight.direction));
                half shadow = smoothstep(0.45h, 0.55h, mainLight.shadowAttenuation);
                half lit = smoothstep(0.27h, 0.31h, ndl * shadow);
                half3 color = lerp(tex * _ShadeDark, tex, lit) * mainLight.color + tex * SampleSH(N) * 0.25h;

                // 금빛 균열: 노랑 · 금색 픽셀을 찾아 균열을 따라 흐르는 맥동
                half goldMask = max(saturate((tex.r + tex.g * 0.8h - tex.b * 1.7h - 0.75h) * 3.0h) * step(pv, th), seam);
                half flow = 0.55h + 0.45h * sin(t * _GlowSpeed - dot(xz, float2(0.35, 0.27)) + region * 5.0);
                color += _GlowColor.rgb * goldMask * _Glow * flow;

                // 허공 틈: 아주 어두운 곳에서 작은 별이 반짝임
                half lum = dot(tex, half3(0.299h, 0.587h, 0.114h));
                half voidMask = 1.0h - smoothstep(0.06h, 0.14h, lum);
                float2 cell = floor(xz * 3.0);
                float star = step(0.94, Hash21(cell)) * (0.5 + 0.5 * sin(t * 3.0 + Hash21(cell + 7.0) * 30.0));
                float2 f = frac(xz * 3.0) - 0.5;
                star *= 1.0 - smoothstep(0.03, 0.09, length(f));
                color += star * voidMask * _StarTwinkle;

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
