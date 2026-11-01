Shader "LightCrystal/Ground Palette"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Tint", Color) = (0.82,0.86,0.8,1)
        _Saturation("Saturation", Range(0,1)) = 0.65
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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color : COLOR; float2 groundPosition : TEXCOORD7; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Saturation;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color;
                output.groundPosition = TransformObjectToWorld(input.positionOS).xy;
                return output;
            }
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            float GroundNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 pixel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half brightness = dot(pixel.rgb, half3(0.299, 0.587, 0.114));
                half3 color = lerp(brightness.xxx, pixel.rgb, _Saturation) * input.color.rgb;
                color *= lerp(0.88, 1.06, GroundNoise(input.groundPosition * 0.13));
                half alpha = pixel.a * input.color.a;
                SurfaceData2D surface;
                InputData2D data;
                InitializeSurfaceData(color, alpha, half4(1,1,1,1), half3(0,0,1), surface);
                InitializeInputData(input.uv, input.lightingUV, data);
                return CombinedShapeLightShared(surface, data);
            }
            ENDHLSL
        }
    }
}
