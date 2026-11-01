Shader "LightCrystal/Shop Summon Zone"
{
    Properties
    {
        _Color("Color", Color) = (0.86,0.61,0.28,1)
        _Progress("Progress", Range(0,1)) = 0
        _Phase("Wave Phase", Range(0,1)) = 0
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
                float _Progress;
                float _Phase;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }
            float Line(float distance, float thickness)
            {
                return 1.0 - smoothstep(thickness, thickness + 0.012, abs(distance));
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (floor(input.uv * 160.0) + 0.5) / 160.0 * 2.0 - 1.0;
                float radius = length(p);
                float angle = (atan2(p.y, p.x) + PI) / TWO_PI;
                float segments = step(0.14, frac(angle * 16.0));
                float outer = Line(radius - 0.94, 0.005) * segments;
                float arc = Line(radius - 0.94, 0.005) * step(angle, _Progress) * step(0.001, _Progress);
                float inside = 1.0 - smoothstep(0.94, 1.0, radius);
                float wave = Line(radius - _Phase * 0.94, 0.004) * inside;
                float alpha = max(outer * 0.48, arc * 0.9);
                alpha = max(alpha, wave * lerp(0.025, 0.19, _Progress));
                return half4(_Color.rgb, saturate(alpha) * _Color.a);
            }
            ENDHLSL
        }
    }
}
