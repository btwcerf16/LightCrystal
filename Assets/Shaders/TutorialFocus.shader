Shader "LightCrystal/TutorialFocus"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Tutorial Focus"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _TutorialFocusTarget;
            float _TutorialFocusAmount;

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 color = FragBlit(input, sampler_PointClamp);
                float4 targetClip = TransformWorldToHClip(_TutorialFocusTarget.xyz);
                float inverseW = rcp(max(targetClip.w, 0.00001));
                float2 center = targetClip.xy * inverseW * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                center.y = 1.0 - center.y;
                #endif

                float2 distanceFromCenter = input.positionCS.xy / _ScaledScreenParams.xy - center;
                distanceFromCenter.x *= _ScaledScreenParams.x / _ScaledScreenParams.y;
                float radius = _TutorialFocusTarget.w * abs(UNITY_MATRIX_P._m11) * inverseW * 0.5;
                float distanceFromTarget = length(distanceFromCenter) / max(radius, 0.00001);
                half focus = radius > 0.0 ? 1.0 - smoothstep(0.75, 1.0, distanceFromTarget) : 0.0;
                half luminance = dot(color.rgb, half3(0.2126, 0.7152, 0.0722));
                color.rgb = lerp(color.rgb, luminance.xxx, _TutorialFocusAmount * (1.0 - focus));
                return color;
            }
            ENDHLSL
        }
    }
}
