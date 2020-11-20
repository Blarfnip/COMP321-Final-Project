Shader "Unlit/TransparencyShader"
{
    Properties
    {
        _Tex("default texture",2D) = "default" {}
        _Color("default color",Color) = (1.0,1.0,1.0,1.0)
        _Transparency("Transparency", Range(0.0,1.0)) = 0.8
        
    }

    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            sampler2D _Tex;
            float4 _Tex_ST;
            float _Transparency;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Tex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                
                fixed4 col = tex2D(_Tex, i.uv) + _Color;
                col.a = _Transparency-_Time.y/10;
                if (col.a<0.01) clip(col.r),clip(col.g),clip(col.b);
                return col;
            }
            ENDCG
        }
    }
}
