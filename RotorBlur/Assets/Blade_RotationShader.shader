Shader "Custom/Blade_RotationShader"
{   
    // https://docs.unity3d.com/Manual/SL-VertexFragmentShaderExamples.html
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Fade" "IgnoreProjector"="True" "DisableBatching" = "True"} // Fade
        
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            ZWrite Off 
            Fog { Mode Off } 
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            static uint instanceCount = 100;
            static float sigma = 0.35f;
             

            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma instancing_options procedural:setup 
            #pragma multi_compile_instancing nolightmap nodirlightmap nodynlightmap novertexlight
            //#pragma instancing_options procedural:ConfigureProcedural
            // shadow helper functions and macros
            #include "AutoLight.cginc"

            void setup() 
            {
               #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    uint temp = unity_InstanceID;
                #endif  
            } 

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

            float instanceAlpha(uint id, uint instanceCount, float sigma) {
	            float f = (float)id / (instanceCount - 1) - 0.5;
	            return gaussian(f * 2, sigma);
            }

            float normalizedInstanceAlpha(uint id, uint instanceCount, float sigma) {
	            float acc = 0;
	            for (uint i = 0; i < instanceCount; ++i)
		            acc += instanceAlpha(i, instanceCount, sigma);
	            return instanceAlpha(id, instanceCount, sigma) / acc;
            }


            struct appdata
            {
                float4 vertex : POSITION;
                float4 texcoord : TEXCOORD0;
                float4 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1) // put shadows data into TEXCOORD1
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
   	            nointerpolation float  bladeAlpha: TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID // use this to access instanced properties in the fragment shader.
            };
            v2f vert (appdata v, uint instanceID : SV_InstanceID )   // instanceID -->  https://forum.unity.com/threads/unity_instanceid-does-not-work-and-documentation-is-lacking.1491262/#post-9299753
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);  // https://forum.unity.com/threads/instance-id-in-shader.501821/#post-3266676
                //UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.
                      
                float4x4 mr = calcRotateMatrix(instanceID * 0.01f);
                float4 p = mul(v.vertex, mr);

                o.pos = UnityObjectToClipPos(p);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(worldNormal,1));

	            o.bladeAlpha = normalizedInstanceAlpha(instanceID, instanceCount, sigma);
                // compute shadows data
                TRANSFER_SHADOW(o)
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target 
            {
                //UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.
                
                fixed4 col = tex2D(_MainTex, i.uv);
                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                fixed shadow = SHADOW_ATTENUATION(i);
                // darken light's illumination with shadow, keep ambient intact
                fixed3 lighting = i.diff * shadow + i.ambient;
                col.rgb *= lighting;
                col.a = i.bladeAlpha * 1.0f;
                return col;
            }
            ENDCG
        }

        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}