//This is an unlit shader. So no lighting will be calculated

Shader "Custom/HelloWorldShader"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            //Program input is this struct. It's passed into the vertex program "vert"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            //The vertex program outputs this struct. It's passed into the fragment program "frag"
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Vertex program. Used for adjusting vertex data
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Fragment program. Used for final pixel output
            fixed4 frag (v2f i) : SV_Target
            {
                const float PI = 3.14159;
                // Using the UV calculate some color values based on UV coordinates
                float redValue = (sin(i.uv.x * PI) + PI / 2) / PI;
                float greenValue = (cos(i.uv.y * PI) + PI / 2) / PI;
                float blueValue = (sin(i.uv.y * PI) + PI / 2) / PI;
                // Create a fixed4 and populate color values (RGBA)
                fixed4 col = fixed4(redValue, greenValue, blueValue, 1.0);
                // Return output color
                return col;
            }
            ENDCG
        }
    }
}
