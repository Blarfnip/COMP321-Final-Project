Shader "Custom/GrayScaleTextureShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _JiggleAmplitude ("Jiggle Amplitude", float) = 0.1
        _JiggleFrequency ("Jiggle Frequency", float) = 200
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _JiggleAmplitude;
            float _JiggleFrequency;

            float4 jiggleVertex(float4 vertex, float4 sv_Vertex) {
                vertex.x += sin(_Time * _JiggleFrequency + sv_Vertex.y) * _JiggleAmplitude;
                vertex.y += cos(_Time * _JiggleFrequency + sv_Vertex.x) * _JiggleAmplitude;
                
                return vertex;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = jiggleVertex(o.vertex, v.vertex);

                return o;
            }

            fixed4 colorToGrayscale(fixed4 col) {
                float avg = (col.r + col.g + col.b) / 3;
                return fixed4(avg, avg, avg, 1); 
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return colorToGrayscale(col);
            }
            ENDCG
        }
    }
}
