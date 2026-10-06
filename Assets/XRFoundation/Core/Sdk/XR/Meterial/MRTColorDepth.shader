Shader "Custom/MRTColorDepth"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float depth : TEXCOORD0;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Linear eye-space depth normalized by far plane.
                o.depth = o.vertex.w / _ProjectionParams.z;
                return o;
            }

            void frag(v2f i, out fixed4 color : SV_Target0, out fixed4 depth : SV_Target1)
            {
                color = _Color;
                float d = saturate(i.depth);
                depth = fixed4(d, d, d, 1);
            }
            ENDCG
        }
    }
}
