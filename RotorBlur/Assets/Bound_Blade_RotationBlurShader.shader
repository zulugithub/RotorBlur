
Shader "Custom/Bound_Blade_RotationBlurShader_URP"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Color", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "False"
            "DisableBatching" = "True"
        }

        ZWrite Off
        BlendOp Add
        Blend One OneMinusSrcAlpha, Zero One

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4x4 _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld;

                float _spreading;
                float _sigma;
                float _texScale;

            CBUFFER_END

            //static const uint instanceCount = 100;

            #define MAX_BLUR_ANGLE 40.0
            #define PI 3.14159265358979323846
            #define MAX_BLUR_ANGLE_RAD ((MAX_BLUR_ANGLE / 180.0) * PI)

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 pos : TEXCOORD0;
                float4 wpos : TEXCOORD1;
                float4 projPos : TEXCOORD2;
                float4 sv_pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = v.vertex.xyz;

                o.sv_pos = TransformObjectToHClip(v.vertex.xyz);
                o.projPos = o.sv_pos;

                return o;
            }

            float4x4 calcRotateMatrix(float angle)
            {
                float s, c;
                sincos(angle, s, c);

                float4x4 rm = {
                    c, 0, -s, 0,
                    0, 1,  0, 0,
                    s, 0,  c, 0,
                    0, 0,  0, 1
                };

                return rm;
            }

            float gaussian(float x, float s)
            {
                return exp(-(x * x) / (2 * s * s));
            }

            float rand(float value)
            {
                return frac(sin(value) * 143758.5453);
            }

            float rand2(float2 value)
            {
                float seed = dot(frac(value), float2(12.9898, 37.719));
                return rand(seed);
            }

            float4x4 calcRotateMatrixPS(uint j, uint steps, float2 randSeed)
            {
                float f = (j + 0.5) / steps - 0.5;

                float a = f * _spreading * PI / 180.0;

                float rnd = rand2(randSeed) - 0.5;

                return calcRotateMatrix(a - rnd * _sigma * 0.2);
            }

            half4 frag(v2f i) : SV_Target
            {
                float4 pos = float4(i.pos, 1.0);

                const uint steps = 128;

                float4 acc = 0;

                [loop]
                for (uint j = 0; j < steps; ++j)
                {
                    float4x4 mr = calcRotateMatrixPS(j, steps, i.projPos.xy);

                    float4 p = mul(pos, mr);

                    float4 uvp = mul(
                        p,
                        _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld
                    );

                    float2 uv = uvp.xy / uvp.w * _texScale;

                    half4 col = SAMPLE_TEXTURE2D_LOD(
                        _MainTex,
                        sampler_MainTex,
                        uv,
                        0
                    );

                    acc += col;
                }

                acc /= steps;

                acc.a += 1e-6;

                acc *= saturate(acc.a) / acc.a;

                return acc;
            }

            ENDHLSL
        }
    }
}
