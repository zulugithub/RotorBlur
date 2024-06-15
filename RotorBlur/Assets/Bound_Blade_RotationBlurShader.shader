Shader "Custom/Bound_Blade_RotationBlurShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Color", 2D) = "white" {}

		//[Enum(UnityEngine.Rendering.BlendMode)] // https://www.youtube.com/watch?v=vr1u8HbWTbo
		//_SrcFactor("Src Factor",float) = 5
		//[Enum(UnityEngine.Rendering.BlendMode)]
		//_DstFactor("Dst Factor",float) = 10
		//[Enum(UnityEngine.Rendering.BlendOp)]
		//_Opp("Src Factor",float) = 0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Fade" "IgnoreProjector"="False" "DisableBatching" = "True"}
        ZWrite Off //Lighting Off Fog { Mode Off } 
		//BlendOp [_Opp]
		//Blend [_SrcFactor] [_DstFactor]
		// finalValue = sourceFactor * sourceValue operation destinationFactor * destinationValue
        // Blend <source factor RGB> <destination factor RGB>, <source factor alpha> <destination factor alpha>
        BlendOp Add
        Blend One OneMinusSrcAlpha, Zero One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Projection matrix of the camera acting as a projector * world to camera (acting as a projector) matrix  *  ObjectToWorld
            float4x4 _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld; 
            float _spreading = 40; // [deg]
			float _sigma = 0.35 ; // []
			static float _texScale = 1.0;
			static uint instanceCount = 100; 

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            struct appdata
			{
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 pos : TEXCOORD0;     // Object (cylinder) position in object space
                float4 wpos : TEXCOORD1;    
                float4 projPos : TEXCOORD2; 
                float4 sv_pos : SV_POSITION;
            };

			v2f vert (appdata v)
            {
                v2f o;
                o.pos = v.vertex; // v.vertex is the local vertex coordinte           
                o.sv_pos = o.projPos = UnityObjectToClipPos(v.vertex);  
				//o.uv = v.uv;
                return o;
            }

			#define MAX_BLUR_ANGLE 40.0

			#define PI 3.1415926535897932384626433832795
			#define MAX_BLUR_ANGLE_RAD ((MAX_BLUR_ANGLE / 180.0) * PI)

			float4x4 calcRotateMatrix(float angle) 
			{
				float s, c;
				sincos(angle, s, c);
				float4x4 rm = {   c, 0,-s, 0,
								  0, 1, 0, 0,
								  s, 0, c, 0,
								  0, 0, 0, 1 };
				return rm;
			}

			float gaussian(float x, float s) 
			{
				return exp(-(x * x) / (2 * s * s));
			}

			float rand(float value) {
				return frac(sin(value) * 143758.5453);
			}

			float rand2(float2 value) {
				float seed = dot(frac(value), float2(12.9898, 37.719));
				return rand(seed);
			}

			float4x4 calcRotateMatrixPS(uint j, uint steps, float2 randSeed) {
				float f = (j + 0.5) / steps - 0.5;
				float a = MAX_BLUR_ANGLE_RAD * f / instanceCount * 2;
				float rnd = rand2(randSeed) - 0.5;
				return calcRotateMatrix(a - rnd * _sigma * 0.2);
			}

            sampler2D _MainTex;

			// ©bgolus: "The fragment shader is taking the interpolated 
			//           local vertex position, rotating it a bit, and calculating
			//           the resulting render texture screen space position. Then 
			//           sampling the low res render texture at that position. So 
			//           the sample matrix must be an object space to render texture 
			//           projection space transform they're calculating and passing in." 
			//
			// See DCS's '"...\Eagle Dynamics\Bazar\shaders\enlight\helicopterRotor.fx"
			//
			// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/UnityCG.cginc
			// https://forum.unity.com/threads/projecting-texture-from-a-camera-on-objects.628189/						ComputeNonStereoScreenPos(
			// https://forum.unity.com/threads/decodedepthnormal-linear01depth-lineareyedepth-explanations.608452/		_ZBufferParams.z
			// https://forum.unity.com/threads/custom-shader-not-writing-to-depth-buffer.1048934/
			// https://forum.unity.com/threads/mul-function-and-matrices.445266/										mul()
            fixed4 frag (v2f i) : SV_Target 
            {
                float4 pos = float4(i.pos, 1.0);
				const uint steps = 128;
				float4 acc = 0;

				[loop]
				for (uint j = 0; j < steps; ++j) 
				{
					float4x4 mr = calcRotateMatrixPS(j, steps, i.projPos.xy);

					//float4 p = mul(pos, bladePos); // TODO
					float4 p = mul(pos, mr);
					float4 uvp = mul(p, _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld); // clipped position in projector space (principally same as UNITY_MATRIX_MVP but for capture_camera)

					float2 uv = uvp.xy/uvp.w * _texScale;
					fixed4 col = tex2D(_MainTex, uv); 

					acc += col;
				 }
				 acc /= steps;

				 acc.a += 1e-6;
				 acc *= saturate(acc.a) / acc.a;		// normalize by alpha

				 return acc;	
            }
            ENDCG
        }
    }

    FallBack "Legacy Shaders/VertexLit"
}
