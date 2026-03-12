Shader "Custom/BoidsGradient"
{
    Properties
    {
        _MinBounds ("Min Bounds", Float) = -10.0
        _MaxBounds ("Max Bounds", Float) = 10.0 //assumed that these are the boundaries of a cube
        _FromColor ("From Color", Color) = (0, 0, 0, 1)
        _ToColor ("To Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float _MinBounds;
                float _MaxBounds;
                float4 _FromColor;
                float4 _ToColor;
            CBUFFER_END

            #ifdef DOTS_INSTANCING_ON
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float,  _MinBounds)
                    UNITY_DOTS_INSTANCED_PROP(float,  _MaxBounds)
                    UNITY_DOTS_INSTANCED_PROP(float4, _FromColor)
                    UNITY_DOTS_INSTANCED_PROP(float4, _ToColor)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _MinBounds UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _MinBounds)
                #define _MaxBounds UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _MaxBounds)
                #define _FromColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _FromColor)
                #define _ToColor   UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ToColor)
            #endif

            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float range = _MaxBounds - _MinBounds;
                float3 t = saturate((input.positionWS - _MinBounds) / range);

                half4 color;
                color.xyz = lerp(_FromColor.xyz, _ToColor.xyz, t);
                color.w = 1.0;
                
                return color;
            }
            ENDHLSL
        }
    }
}