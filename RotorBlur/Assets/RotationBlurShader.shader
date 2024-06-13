Shader "Unlit/RotationBlurShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Color", 2D) = "white" {}
        [NoScaleOffset] _DepthTex ("Depth", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="False" "RenderType"="Transparent" "DisableBatching" = "True"}
        ZWrite On Lighting Off Fog { Mode Off } Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Projection matrix of the camera acting as a projector * world to camera (acting as a projector) matrix  *  ObjectToWorld
            float4x4 _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld; 
			float4x4 _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse; 
            float _spreading = 40; // [deg]
			float _sigma = 0.35 ; // []
			float _texScale = 1.0f;

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            struct appdata
			{
                float4 vertex : POSITION;
                //float2 uv : TEXCOORD1;
            };

            struct v2f
            {
                float3 pos : TEXCOORD0;     // Object (cylinder) position in object space
                //float4 wpos : TEXCOORD1;    
                //float4 projpos : TEXCOORD2;
				//float2 uv : TEXCOORD1;
                float4 sv_pos : SV_POSITION;
            };

			v2f vert (appdata v)
            {
                v2f o;
                o.pos = v.vertex; // v.vertex is the local vertex coordinte           
                o.sv_pos = UnityObjectToClipPos(v.vertex);  
				//o.uv = v.uv;
                return o;
            }

			#define USE_DEPTH_REPROJECT 1
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

            sampler2D _MainTex;
			sampler2D _DepthTex;

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
				const uint steps = 64;

				float aw;
				float4 rpos = pos;

		   
			#if USE_DEPTH_REPROJECT
				aw = 0.1;
				[loop]
				for (uint j = 0; j < steps; ++j) { 
			
					float f = (j + 0.5) / steps - 0.5;
					float a = MAX_BLUR_ANGLE_RAD * f;
					float w = gaussian(f, _sigma * 0.25);
					float4x4 mr = calcRotateMatrix(a);
			
					float4 p = mul(pos, mr);
					float4 uvp = mul(p, _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld);
			
					float2 uv = uvp.xy / uvp.w * _texScale;
			
					float depth =  tex2D(_DepthTex, uv).r ; 
					w *= step(1e-6, depth);
			
					float4 rp = mul( float4(uvp.xy / uvp.w, depth, 1), _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld_inverse);
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
				for (uint j = 0; j < steps; ++j) {

					float f = (j + 0.5) / steps - 0.5;
					float a = f * _spreading * UNITY_PI / 180.0f;
					float w = gaussian(f, _sigma * 0.5); 
					float4x4 mr = calcRotateMatrix(a);

					float4 p = mul(rpos, mr);
                    float4 uvp = mul(p, _ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld); // clipped position in projector space (principally same as UNITY_MATRIX_MVP but for capture_camera)

					float2 uv = uvp.xy/uvp.w * _texScale;
					fixed4 col = tex2D(_MainTex, uv); 
					//float depth = tex2D(_DepthTex, uv).r;
					//col.r = depth; col.g = depth; col.b = depth; col.w=1;
					acc += col * w;
					aw += w;	
				 }
				 acc /= aw;

				 return acc; 
				
            }
            ENDCG
        }
    }

    FallBack "Legacy Shaders/VertexLit"
}
