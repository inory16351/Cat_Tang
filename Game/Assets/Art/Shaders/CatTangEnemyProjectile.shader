Shader "CatTang/EnemyProjectile"
{
    Properties
    {
        [HDR] _Tint ("Projectile tint", Color) = (1, 0.18, 0.7, 1)
        _Phase ("Emission phase", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "EmissiveBody"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Phase;
            CBUFFER_END

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
                return half4(MixFog(color, input.fog * 0.30h), 1);
            }
            ENDHLSL
        }
    }
}
