Shader "LightCrystal/Spell Range"
{
    Properties
    {
        _Color("Color", Color) = (0.35, 0.85, 0.9, 0.75)
        _LineWidth("Line width, pixels", Range(1, 6)) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _LineWidth;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                float aa = max(fwidth(radius), 0.0001);
                float width = aa * _LineWidth;
                float edge = smoothstep(1.0 - width - aa, 1.0 - width, radius)
                    * (1.0 - smoothstep(1.0 - aa, 1.0, radius));
                return half4(_Color.rgb, _Color.a * edge);
            }
            ENDHLSL
        }
    }
}
