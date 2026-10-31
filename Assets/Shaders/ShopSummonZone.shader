Shader "LightCrystal/Shop Summon Zone"
{
    Properties
    {
        _Color("Color", Color) = (0.25,0.85,1,1)
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
            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                float aa = max(fwidth(radius), 0.003);
                float inside = 1.0 - smoothstep(0.995, 1.0, radius);
                float border = smoothstep(0.97 - aa, 0.98, radius) * inside;
                float wave = 1.0 - smoothstep(0.015, 0.05 + aa, abs(radius - _Phase));
                float alpha = max(border * 0.9, wave * lerp(0.08, 0.65, _Progress) * inside);
                alpha = max(alpha, inside * lerp(0.015, 0.05, _Progress));
                return half4(_Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
