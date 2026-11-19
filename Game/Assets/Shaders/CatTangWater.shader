// 3장 분수 샘 물 폭발용 3D 물 (2026-11-18 사용자: 그림 말고 블렌더 3D로, 확실히 물처럼 보이게)
// 블렌더 회전체 메시(fx_water_*, UV 없음)에 씀. 물처럼 보이게 하는 것:
//  - 몸통: 반투명 청록, 빛 받는 쪽은 밝은 하늘빛 · 그늘 쪽은 짙은 청록 (계단 2단, 게임 툰 규칙)
//  - 프레넬: 비스듬한 가장자리일수록 불투명하고 흰빛 (물 표면 반사) — 가운데는 비쳐 보임
//  - 흐르는 줄무늬: 물체 좌표 높이 + 둘레 각으로 흰 물결 줄이 위로 흘러감 (_Flow, 물방울은 0)
//  - 하이라이트: 날카로운 흰 반짝 (빛 반사), 꼭대기 쪽 거품(물체 y가 _FoamY 위면 하얗게)
//  - 얇은 짙은 청록 외곽선 (화면 픽셀 두께, 게임 캐릭터 외곽선과 같은 느낌)
// _Fade 0~1로 전체가 옅어짐 (MaterialPropertyBlock)
Shader "CatTang/Water"
{
    Properties
    {
        _Shallow ("Lit water", Color) = (0.36, 0.86, 1, 1)
        _Deep ("Shade water", Color) = (0.05, 0.47, 0.8, 1)
        _Rim ("Rim / foam", Color) = (0.95, 1, 1, 1)
        _Tint ("Sky tint", Color) = (1, 0.75, 0.92, 1)
        _OutlineColor ("Outline", Color) = (0.05, 0.33, 0.45, 1)
        _OutlinePx ("Outline px", Float) = 2.2
        _Alpha ("Body alpha", Range(0, 1)) = 0.78
        _Flow ("Flow speed", Float) = 2.4
        _FoamY ("Foam above (object y)", Float) = 99
        _Fade ("Fade", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Shallow, _Deep, _Rim, _Tint, _OutlineColor;
            float _OutlinePx, _Alpha, _Flow, _FoamY, _Fade;
        CBUFFER_END
        ENDHLSL

        // 1) 외곽선: 뒷면을 화면 픽셀 두께만큼 밀어냄
        Pass
        {
            Name "WaterOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct Vo { float4 pos : SV_POSITION; };
            Vo vert(A i)
            {
                Vo o;
                float4 c = TransformObjectToHClip(i.pos.xyz);
                float3 nW = TransformObjectToWorldNormal(i.n);
                float2 nC = mul((float3x3)UNITY_MATRIX_VP, nW).xy;
                float len = max(length(nC), 1e-4);
                c.xy += nC / len * _OutlinePx * c.w * 2.0 / _ScreenParams.xy;
                o.pos = c;
                return o;
            }
            half4 frag(Vo i) : SV_Target { return half4(_OutlineColor.rgb, 0.85 * _Fade); }
            ENDHLSL
        }

        // 2) 물 몸통
        Pass
        {
            Name "WaterBody"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct Vo { float4 pos : SV_POSITION; float3 nW : TEXCOORD0; float3 pW : TEXCOORD1; float3 pO : TEXCOORD2; };
            Vo vert(A i)
            {
                Vo o;
                o.pW = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(o.pW);
                o.nW = TransformObjectToWorldNormal(i.n);
                o.pO = i.pos.xyz;
                return o;
            }
            half4 frag(Vo i) : SV_Target
            {
                float3 N = normalize(i.nW);
                float3 V = normalize(GetWorldSpaceViewDir(i.pW));
                Light L = GetMainLight();
                float ndl = dot(N, L.direction);
                // 몸통 (툰 물): 정면 = 밝은 물, 비스듬한 쪽 = 짙은 물, 그늘 쪽은 한 번 더 짙게 → 맨 가장자리에 가는 흰 테
                float fr = 1 - saturate(dot(N, V));
                half3 col = fr < 0.42 ? _Shallow.rgb : _Deep.rgb;
                if (ndl < -0.1) col *= 0.82;
                // 하늘빛(분홍) 반사가 정면 밝은 물에 아주 살짝
                col = lerp(col, _Tint.rgb, (fr < 0.42 ? 0.08 : 0.0));
                half rim = smoothstep(0.86, 0.94, fr);
                // 흐르는 물결 줄: 높이를 따라 위로, 둘레 각으로 비틀림
                float ang = atan2(i.pO.z, i.pO.x);
                float s = frac(i.pO.y * 3.2 - _Time.y * _Flow + sin(ang * 3 + i.pO.y * 5) * 0.18);
                half streak = smoothstep(0.86, 0.9, s) * (1 - smoothstep(0.95, 0.99, s)) * step(0.001, _Flow);
                // 반짝 하이라이트
                float3 H = normalize(L.direction + V);
                half spec = step(0.955, dot(N, H));
                // 꼭대기 거품
                half foam = step(_FoamY, i.pO.y);
                col = lerp(col, _Rim.rgb, saturate(rim * 0.9 + streak * 0.85 + spec + foam * 0.85));
                half a = saturate(_Alpha + (fr < 0.42 ? -0.12 : 0.08) + rim * 0.2 + streak * 0.25 + spec + foam * 0.25) * _Fade;   // 가운데가 더 비쳐 보임
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
