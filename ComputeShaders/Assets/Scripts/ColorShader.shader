Shader "Custom/BoidsGradient"
{
    Properties
    {
        _MinBounds ("Min Bounds", Float) = -50.0
        _MaxBounds ("Max Bounds", Float) = 50.0
        _FromColor ("From Color", Color) = (0, 0, 0, 1)
        _ToColor ("To Color", Color) = (1, 1, 1, 1)
        _Scale ("Scale", Float) = 1.0
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
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Boid
            {
                float3 position;
                float memory_alignment_spacer_1;
                float4 rotation;
                float3 velocity;
                float memory_alignment_spacer_2;
            };
            
            StructuredBuffer<Boid> particles;


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
                float _Scale;
            CBUFFER_END
            
            

            float4x4 trs(float3 pos, float4 q, float s)
            {
                float x2 = q.x * 2.0; float y2 = q.y * 2.0; float z2 = q.z * 2.0;
                float xx = q.x * x2;  float yy = q.y * y2;  float zz = q.z * z2;
                float xy = q.x * y2;  float xz = q.x * z2;  float yz = q.y * z2;
                float wx = q.w * x2;  float wy = q.w * y2;  float wz = q.w * z2;
                float4x4 m;
                m[0] = float4((1.0-(yy+zz))*s, (xy-wz)*s,      (xz+wy)*s,      pos.x);
                m[1] = float4((xy+wz)*s,       (1.0-(xx+zz))*s, (yz-wx)*s,      pos.y);
                m[2] = float4((xz-wy)*s,       (yz+wx)*s,      (1.0-(xx+yy))*s, pos.z);
                m[3] = float4(0,               0,               0,               1);
                return m;
            }

            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    Boid b = particles[unity_InstanceID];
                    unity_ObjectToWorld = trs(b.position, b.rotation, _Scale);
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