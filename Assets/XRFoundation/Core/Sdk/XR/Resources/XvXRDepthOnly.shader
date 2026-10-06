Shader "Hidden/XvXR/LinearEyeDepthOnly"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Back ZWrite On ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float eyeDepthMetres : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                float4 clipPosition = UnityObjectToClipPos(input.vertex);
                output.position = clipPosition;
                // For a perspective projection clip.w is -viewSpace.z, which
                // gives linear eye-space depth in metres for Unity world units.
                output.eyeDepthMetres = max(0.0, clipPosition.w);
                return output;
            }

            float4 frag(v2f input) : SV_Target
            {
                // The RFloat target keeps the red component in metres.
                return float4(input.eyeDepthMetres, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
