// 5장 중간 보스 '거울 속의 고양이' 셰이더 (URP, 2026-11-19 사용자: 주인공 고양이 재활용 + 셰이더 씌워서)
// CatTang/Toon과 같은 외곽선 · 바깥 테두리 · 그림자 패스를 쓰고, 본체만 거울 몸으로 바꿈:
// - 툰 1단 명암 (브롤풍과 같은 규칙) 위에 크롬 반사: 반사 벡터 위쪽 = 옅은 은 라벤더 · 수평선 = 흰 띠 · 아래 = 깊은 남보라 (5장 허공 색)
// - 프레넬 은빛 테두리 + 비스듬히 흐르는 반짝임 띠(_GlintSpeed) = 거울 표면
// - 금빛 균열 (_Crack 0~1): 물체 공간 3D 셀 경계에서 갈라짐. 셀마다 문턱값이 달라 _Crack이 오를수록 금이 늘어남 → 코드가 (1 - 체력 비율)을 넣음 (맞을수록 금이 감)
//   균열은 HDR 금빛(_CrackGlow > 1)이라 블룸에 빛남
Shader "CatTang/MirrorCat"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Mirror)]
        _Mirror ("Mirror Amount", Range(0, 1)) = 0.45
        _SkyColor ("Reflect Sky", Color) = (0.86, 0.88, 1, 1)
        _HorizonColor ("Reflect Horizon", Color) = (1, 1, 1, 1)
        _VoidColor ("Reflect Void", Color) = (0.2, 0.14, 0.38, 1)
        _RimColor ("Rim Color", Color) = (0.85, 0.92, 1, 1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.8
        _GlintSpeed ("Glint Speed", Float) = 0.35
        _GlintStrength ("Glint Strength", Range(0, 2)) = 0.6

        [Header(Cracks)]
        _Crack ("Crack Amount (0 = 멀쩡 ~ 1 = 산산조각)", Range(0, 1)) = 0.1
        _CrackScale ("Crack Cell Scale", Float) = 3.2
        _CrackWidth ("Crack Line Width", Range(0.005, 0.15)) = 0.045
        _CrackColor ("Crack Color", Color) = (1, 0.78, 0.32, 1)
        _CrackGlow ("Crack Glow (HDR x)", Range(1, 6)) = 2.6

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.08, 0.05, 0.08, 1)
        _OutlineWidth ("Outline Width (px)", Range(0, 10)) = 4
        _OuterOutlineColor ("Outer Outline Color", Color) = (0.78, 0.07, 0.12, 1)
        _OuterOutlineWidth ("Outer Outline Width (px, 0 = 끔)", Range(0, 12)) = 0
        _OuterStencilRef ("Outer Outline Stencil Ref", Int) = 0
        [HideInInspector] _BrawlActor ("Brawl Actor", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Mirror;
            half4 _SkyColor, _HorizonColor, _VoidColor, _RimColor;
            half _RimStrength, _GlintSpeed, _GlintStrength;
            half _Crack, _CrackScale, _CrackWidth, _CrackGlow;
            half4 _CrackColor;
            half4 _OutlineColor;
            half _OutlineWidth;
            half4 _OuterOutlineColor;
            half _OuterOutlineWidth;
            half _BrawlActor;
        CBUFFER_END

        float _CatTangBrawl;
        float _CatTangMirrorPass;

        float3 Hash33(float3 p)
        {
            p = float3(dot(p, float3(127.1, 311.7, 74.7)), dot(p, float3(269.5, 183.3, 246.1)), dot(p, float3(113.5, 271.9, 124.6)));
            return frac(sin(p) * 43758.5453);
        }

        // 3D 셀 경계까지 거리 (가장 가까운 두 점의 차) + 가장 가까운 셀 · 두 번째 셀의 무작위 값 (균열이 어느 셀 경계에서 먼저 생길지)
        void Cells(float3 x, out float edge, out float r1, out float r2)
        {
            float3 ip = floor(x), fp = frac(x);
            float d1 = 8, d2 = 8; r1 = 0; r2 = 0;
            for (int k = -1; k <= 1; k++)
            for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                float3 g = float3(i, j, k);
                float3 h = Hash33(ip + g);
                float3 d = g + h - fp;
                float dd = dot(d, d);
                if (dd < d1) { d2 = d1; r2 = r1; d1 = dd; r1 = h.x; }
                else if (dd < d2) { d2 = dd; r2 = h.x; }
            }
            edge = sqrt(d2) - sqrt(d1);
        }
        ENDHLSL

        // ---------- 1. 본체 (거울 몸) ----------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Stencil { Ref [_OuterStencilRef] Comp Always Pass Replace }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // 툰 1단 명암 (브롤풍과 같은 문턱)
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(N, mainLight.direction));
                half shadow = smoothstep(0.45h, 0.55h, mainLight.shadowAttenuation);
                half lit = smoothstep(0.27h, 0.31h, ndl * shadow);
                half3 color = lerp(albedo * 0.62h, albedo, lit) * mainLight.color + albedo * SampleSH(N) * 0.25h;

                // 크롬 반사: 반사 벡터 높이로 하늘 → 수평선 흰 띠 → 아래 허공. 단계를 딱 끊어 툰 금속처럼
                float3 R = reflect(-V, N);
                half ry = R.y;
                half3 env = ry > 0.08h ? _SkyColor.rgb : (ry > -0.12h ? _HorizonColor.rgb * 1.15h : _VoidColor.rgb);
                color = lerp(color, env * lerp(0.75h, 1.0h, lit), _Mirror);

                // 은빛 테두리
                half fresnel = pow(1.0h - saturate(dot(N, V)), 3.0h);
                color += _RimColor.rgb * step(0.35h, fresnel) * _RimStrength;

                // 흐르는 반짝임 띠 (월드 대각선, 몸을 쓸고 지나감)
                half g = frac((i.positionWS.x + i.positionWS.y * 1.3h) * 0.35h - _Time.y * _GlintSpeed);
                half glint = smoothstep(0.0h, 0.02h, g) * (1.0h - smoothstep(0.02h, 0.06h, g));
                color += glint * _GlintStrength * (0.4h + 0.6h * fresnel);

                // 금빛 균열: 셀 경계 중 (두 셀 무작위 값의 작은 쪽) < _Crack 인 곳만 갈라짐
                float edge, r1, r2;
                Cells(i.positionOS * _CrackScale, edge, r1, r2);
                half open = step(min(r1, r2), _Crack * 1.05h);
                half crackLine = (1.0h - smoothstep(_CrackWidth * 0.5h, _CrackWidth, edge)) * open;
                half3 crack = _CrackColor.rgb * _CrackGlow * (0.85h + 0.15h * sin(_Time.y * 6.0h + r1 * 30.0h));
                color = lerp(color, crack, crackLine);
                // 균열 둘레는 살짝 어두운 그을림 (선이 또렷하게)
                half halo = (1.0h - smoothstep(_CrackWidth, _CrackWidth * 2.2h, edge)) * open * (1.0h - crackLine);
                color *= 1.0h - halo * 0.35h;

                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ---------- 2. 외곽선 (CatTang/Toon과 같음) ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            Stencil { Ref [_OuterStencilRef] Comp Always Pass Replace }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float fogFactor : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                if (_CatTangMirrorPass > 0.5) { o.positionCS = float4(0, 0, -2, 1); return o; }
                float4 posCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = length(nCS);
                float2 dir = len > 1e-5 ? nCS / len : float2(0, 0);
                half width = _CatTangBrawl > 0.5 && _BrawlActor > 0.5 ? 3.5h : _OutlineWidth;
                posCS.xy += dir * width * 2.0 * posCS.w / _ScreenParams.xy;
                o.positionCS = posCS;
                o.fogFactor = ComputeFogFactor(posCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 oc = _CatTangBrawl > 0.5 && _BrawlActor > 0.5 ? half3(0.05h, 0.04h, 0.06h) : _OutlineColor.rgb;
                return half4(MixFog(oc, i.fogFactor), 1);
            }
            ENDHLSL
        }

        // ---------- 2-1. 바깥 테두리 ----------
        Pass
        {
            Name "OuterOutline"
            Tags { "LightMode" = "CatTangOuterOutline" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Stencil { Ref [_OuterStencilRef] Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float fogFactor : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                if (_OuterOutlineWidth <= 0.01 || _CatTangMirrorPass > 0.5) { o.positionCS = float4(0, 0, -2, 1); return o; }
                float4 posCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = length(nCS);
                float2 dir = len > 1e-5 ? nCS / len : float2(0, 0);
                half baseW = _CatTangBrawl > 0.5 && _BrawlActor > 0.5 ? 3.5h : _OutlineWidth;
                posCS.xy += dir * (baseW + _OuterOutlineWidth) * 2.0 * posCS.w / _ScreenParams.xy;
                o.positionCS = posCS;
                o.fogFactor = ComputeFogFactor(posCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return half4(MixFog(_OuterOutlineColor.rgb, i.fogFactor), 1); }
            ENDHLSL
        }

        // ---------- 3. 그림자 (그림자 있는 쪽만 진짜 — 3페이즈 가짜 조각은 렌더러 그림자를 끔) ----------
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

            float4 vert(Attributes v) : SV_POSITION
            {
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
                return posCS;
            }

            half4 frag() : SV_Target { return 0; }
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
            float4 vert(Attributes v) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(v); return TransformObjectToHClip(v.positionOS.xyz); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
