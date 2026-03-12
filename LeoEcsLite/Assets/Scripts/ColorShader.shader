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
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct ParticleData
            {
                float4x4 mat;
            };

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<ParticleData> _ParticleBuffer;
            #endif

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

            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    ParticleData data = _ParticleBuffer[unity_InstanceID];
                    unity_ObjectToWorld = data.mat;
                    unity_WorldToObject = unity_ObjectToWorld;
                #endif
            }

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