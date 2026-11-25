// 적 탄 (2026-11-25 가시성): 밝은 심 + 어두운 잉크 테두리 + 적과 같은 빨간 바깥 테두리
// - 몸(EmissiveBody): 장별 탄 색 그대로, 정면(가운데)일수록 흰빛 심을 더함 (_CoreWhite, 숫자 탄은 MPB로 0)
// - 잉크 테두리(Outline, SRPDefaultUnlit): 탄 색을 아주 어둡게 한 얇은 띠 → 밝은 바닥(3장 거울빛 · 구름)에서도 윤곽이 섬
// - 빨간 바깥 테두리(OuterOutline, CatTangOuterOutline): 적 툰 셰이더와 같은 렌더러 기능이 불투명 뒤에 그림 (스텐실 바깥에만)
//   → 어두운 바닥(4장 톱니 · 5장 보라)에서도 적 탄임을 빨강으로 알림
// - 테두리 껍질은 면 법선 대신 "탄 길이 축(로컬 z) 선분에서 꼭짓점까지" 방향으로 화면에서 부풀림:
//   각진(면 단위 법선) 저폴리 탄이라 법선으로 밀면 모서리마다 틈이 생김. 위치만으로 정한 방향이라 틈이 없음.
//   _ShellAxis.x = 축 선분 가운데 z, .y = 선분 반길이 (긴 바늘 탄은 옆면 테두리도 고르게). 스타일별 값은 EnemyProjectile이 MPB로 넣음
// - 스텐실 6: 적(4) · 폭발물(5)과 따로. 탄끼리 겹치면 테두리가 하나로 합쳐짐
Shader "CatTang/EnemyProjectile"
{
    Properties
    {
        [HDR] _Tint ("Projectile tint", Color) = (1, 0.18, 0.7, 1)
        _Phase ("Emission phase", Float) = 0
        _CoreWhite ("Core white (가운데 흰빛)", Range(0, 1)) = 0.32
        _InkWidth ("Ink rim width (px)", Range(0, 6)) = 1.6
        _InkDark ("Ink = tint × this (linear)", Range(0, 0.5)) = 0.035
        _OuterOutlineColor ("Outer Outline Color", Color) = (0.78, 0.07, 0.12, 1)
        _OuterOutlineWidth ("Outer Outline Width (px, 0 = 끔)", Range(0, 12)) = 2.6
        _OuterStencilRef ("Outer Outline Stencil Ref", Int) = 6
        _ShellAxis ("Shell axis (center z, half length)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            half _Phase;
            half _CoreWhite;
            half _InkWidth;
            half _InkDark;
            half4 _OuterOutlineColor;
            half _OuterOutlineWidth;
            float4 _ShellAxis;
        CBUFFER_END

        // 3장 거울 바닥 반사 카메라가 그리는 중 (MirrorReflection): 테두리 껍질은 건너뜀 (툰 셰이더와 같음)
        float _CatTangMirrorPass;

        // 화면 픽셀 기준으로 widthPx만큼 바깥으로 부풀린 클립 위치
        float4 ShellPosition(float3 positionOS, float widthPx)
        {
            float3 axisPoint = float3(0, 0, clamp(positionOS.z, _ShellAxis.x - _ShellAxis.y, _ShellAxis.x + _ShellAxis.y));
            float4 posCS = TransformObjectToHClip(positionOS);
            float4 axisCS = TransformObjectToHClip(axisPoint);
            float2 d = (posCS.xy / posCS.w - axisCS.xy / axisCS.w) * _ScreenParams.xy;
            float len = length(d);
            float2 dir = len > 1e-4 ? d / len : float2(0, 0);
            posCS.xy += dir * widthPx * 2.0 * posCS.w / _ScreenParams.xy;
            return posCS;
        }
        ENDHLSL

        Pass
        {
            Name "EmissiveBody"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            ZWrite On
            Stencil { Ref [_OuterStencilRef] Comp Always Pass Replace }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 mask : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
                half3 mask : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertex = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertex.positionCS;
                output.positionWS = vertex.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(vertex.positionCS.z);
                output.mask = input.mask.rgb;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half facing = abs(dot(normal, GetWorldSpaceNormalizeViewDir(input.positionWS)));
                half facet = abs(dot(normal, normalize(half3(-0.43h, 0.78h, -0.45h))));
                half pulse = 0.96h + 0.06h * sin(_Time.y * 23.0h + _Phase);
                half rim = pow(1.0h - saturate(facing), 2.25h);
                half3 colored = _Tint.rgb * (0.09h + input.mask.r * (0.52h + facet * 0.88h) + rim * 0.28h);
                half hot = input.mask.g * (0.28h + facet * 0.42h);
                half3 color = (colored + lerp(_Tint.rgb, half3(1.0h, 0.94h, 0.86h), 0.64h) * hot) * pulse;
                // 밝은 심: 카메라를 마주한 면일수록 탄 색이 섞인 흰빛으로 (테두리 쪽은 원래 탄 색 그대로 → 색 정체성 유지)
                half core = _CoreWhite * pow(saturate(facing), 3.0h) * (0.55h + 0.45h * input.mask.r);
                color = lerp(color, lerp(_Tint.rgb, half3(1.0h, 0.97h, 0.92h), 0.72h) * 1.08h, core);
                return half4(MixFog(color, input.fog * 0.30h), 1);
            }
            ENDHLSL
        }

        // ---------- 어두운 잉크 테두리 (불투명과 함께 그림, 몸이 찍은 스텐실 바깥에만) ----------
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            Stencil { Ref [_OuterStencilRef] Comp NotEqual Pass Replace }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; half fog : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                if (_InkWidth <= 0.01 || _CatTangMirrorPass > 0.5) { output.positionCS = float4(0, 0, -2, 1); return output; }   // 끔: 화면 밖
                output.positionCS = ShellPosition(input.positionOS.xyz, _InkWidth);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 ink = _Tint.rgb * _InkDark + half3(0.004h, 0.003h, 0.004h);   // 선형 공간: 0.035배 ≈ sRGB 0.2 안팎의 탄 색 먹물
                return half4(MixFog(ink, input.fog * 0.30h), 1);
            }
            ENDHLSL
        }

        // ---------- 빨간 바깥 테두리 (적과 같은 렌더러 기능 CatTangOuterOutline이 불투명 뒤에 그림) ----------
        Pass
        {
            Name "OuterOutline"
            Tags { "LightMode"="CatTangOuterOutline" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Stencil { Ref [_OuterStencilRef] Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; half fog : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                if (_OuterOutlineWidth <= 0.01 || _CatTangMirrorPass > 0.5) { output.positionCS = float4(0, 0, -2, 1); return output; }
                output.positionCS = ShellPosition(input.positionOS.xyz, _InkWidth + _OuterOutlineWidth);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(MixFog(_OuterOutlineColor.rgb, input.fog * 0.30h), 1);
            }
            ENDHLSL
        }
    }
}
