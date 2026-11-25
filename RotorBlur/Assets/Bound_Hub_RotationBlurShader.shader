
Shader "Custom/Bound_Hub_RotationBlurShader_URP"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Color", 2D) = "white" {}
        [NoScaleOffset] _DepthTex ("Depth", 2D) = "white" {}
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

        ZWrite On

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

            TEXTURE2D(_DepthTex);
            SAMPLER(sampler_DepthTex);

            CBUFFER_START(UnityPerMaterial)

                float4x4 _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld;

                float4x4 _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse;

                float _spreading;
                float _sigma;
                float _texScale;

            CBUFFER_END

            #define USE_DEPTH_REPROJECT 1
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
                float4 sv_pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.pos = v.vertex.xyz;

                o.sv_pos = TransformObjectToHClip(v.vertex.xyz);

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

            half4 frag(v2f i) : SV_Target
            {
                float4 pos = float4(i.pos, 1.0);

                const uint steps = 64;

                float aw;
                float4 rpos = pos;

                #if USE_DEPTH_REPROJECT

                    aw = 0.1;

                    [loop]
                    for (uint j = 0; j < steps; ++j)
                    {
                        float f = (j + 0.5) / steps - 0.5;

                        float a = f * _spreading * PI / 180.0;

                        float w = gaussian(f, _sigma * 0.25);

                        float4x4 mr = calcRotateMatrix(a);

                        float4 p = mul(pos, mr);

                        float4 uvp = mul(
                            p,
                            _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld
                        );

                        float2 uv = uvp.xy / uvp.w * _texScale;

                        float depth = SAMPLE_TEXTURE2D_LOD(
                            _DepthTex,
                            sampler_DepthTex,
                            uv,
                            0
                        ).r;

                        w *= step(1e-6, depth);

                        float4 rp = mul(
                            float4(uvp.xy / uvp.w, depth, 1),
                            _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse
                        );

                        rp = mul(mr, rp);

                        w *= max(dot(rp, pos), 0);

                        rpos += rp * w;

                        aw += w;
                    }

                    rpos /= aw;

                #endif

                float4 acc = 0;

                aw = 0;

                [loop]
                for (uint j = 0; j < steps; ++j)
                {
                    float f = (j + 0.5) / steps - 0.5;

                    float a = f * _spreading * PI / 180.0;

                    float w = gaussian(f, _sigma * 0.50);

                    float4x4 mr = calcRotateMatrix(a);

                    float4 p = mul(rpos, mr);

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

                    acc += col * w;

                    aw += w;
                }

                acc /= aw;

                return acc;
            }

            ENDHLSL
        }
    }
}
