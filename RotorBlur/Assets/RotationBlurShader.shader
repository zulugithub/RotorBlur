Shader "Unlit/RotationBlurShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
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
            float _sigma_mod = 22.9368; // []
            float _spreading = 40; // [deg]

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
 
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 oPos : TEXCOORD0;     // Object (cylinder) position in object space
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex); 
                o.oPos = v.vertex; // v.vertex is the local vertex coordinte           
                return o;
            }

            inline float4 RotateAroundYInRad (float4 vertex, float theta)
            {
                float sint, cost;
                sincos(theta, sint, cost);
                float2x2 m = float2x2(cost, -sint, sint, cost);
                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }

            sampler2D _MainTex;

            // ©bgolus: "The fragment shader is taking the interpolated 
            //           local vertex position, rotating it a bit, and calculating
            //           the resulting render texture screen space position. Then 
            //           sampling the low res render texture at that position. So 
            //           the sample matrix must be an object space to render texture 
            //           projection space transform they're calculating and passing in." 
            fixed4 frag (v2f i) : SV_Target 
            {
                #define NUMBER_OF_STEPS 64
                //#define ROTATION_RANGE 40
                
                float4 o0 = {0.0, 0.0, 0.0, 0.0};                               // fragment color 
                float linear_curve = 0.0;                                       // linearly growing variable (from -0.5 ... +0.5 in 64 steps)
                float weight = 0.0;                                             // color mixing weight of sample 
                float normalize_weight = 0.0;                                   // accumulated color mixing weight for normalizing result
                float theta = 0.0;                                              // [rad] rotation of cylinder

                for ( int s=0; s<NUMBER_OF_STEPS; s++ ) 
                {
                    linear_curve = (s + 0.5f) / NUMBER_OF_STEPS - 0.5f;         // linearly growing variable (from -0.5 ... +0.5 in 64 steps)   
                    //theta = linear_curve * ROTATION_RANGE * UNITY_PI / 180.0f;  // [rad] rotate the vertex (-20 deg .. 20 deg)
                    theta = linear_curve * _spreading * UNITY_PI / 180.0f;      // [rad] rotate the vertex (-20 deg .. 20 deg)
                    //weight = pow(2, ( -pow(linear_curve, 2) / (_sigma * _sigma * 0.5f) )  * 1.4427f);        // creates a sinusoidal curve y = (~0...1), weighting the center of frame at most
                    weight = pow(2, ( -pow(linear_curve, 2) * _sigma_mod ));    // simplified above linecreates a sinusoidal curve y = (~0...1), weighting the center of frame at most

                    float4 oVertex = RotateAroundYInRad(i.oPos, theta);         // rotate cylinder's vertex around local y axis

                    float4 renderTextureProjectorSpace = 
                      mul(_ProjectionMatrix_times_WorldToCameraMatrix_times_ObjectToWorld, oVertex);// clipped position in projector space (principally same as UNITY_MATRIX_MVP but for capture_camera)

                    float4 o = renderTextureProjectorSpace * 0.5f;              // https://forum.unity.com/threads/projecting-texture-from-a-camera-on-objects.628189/
                    o.xy = float2(o.x, o.y) + o.w; 
                    o.w = renderTextureProjectorSpace.w;

                    fixed4 col = tex2D(_MainTex, o.xy/o.w);                     // sampling the low res render texture          
                    o0 += col * weight;                                         // mix color    
                    normalize_weight += weight;                                 // summ up weight
                }

                return o0 / normalize_weight;                                   // normalize color and alpha 
            }
            ENDCG
        }
    }
}