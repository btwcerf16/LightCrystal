#ifndef ACTOR_EFFECTS_INCLUDED
#define ACTOR_EFFECTS_INCLUDED

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _EffectColor;
    half4 _HitColor;
    half4 _OutlineColor;
    float4 _MainTex_TexelSize;
    half _OutlineWidth;
    half _EffectAmount;
    half _HitAmount;
CBUFFER_END

half OutlineAlpha(float2 uv)
{
    half inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside;
}

half4 ApplyActorEffects(half4 color, float2 uv)
{
    half brightness = dot(color.rgb, half3(0.299h, 0.587h, 0.114h));
    half3 tinted = _EffectColor.rgb * (0.45h + brightness * 0.55h);
    half shimmer = step(0.9h, frac((uv.x + uv.y) * 12.0h + _Time.y * 0.2h));
    tinted += shimmer * 0.08h;
    color.rgb = lerp(color.rgb, tinted, saturate(_EffectAmount));
    color.rgb = lerp(color.rgb, _HitColor.rgb, saturate(_HitAmount) * 0.9h);
    if (_OutlineWidth > 0.0h)
    {
        float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
        half neighbors = min(min(OutlineAlpha(uv + float2(offset.x, 0)), OutlineAlpha(uv - float2(offset.x, 0))),
            min(OutlineAlpha(uv + float2(0, offset.y)), OutlineAlpha(uv - float2(0, offset.y))));
        half edge = 1.0h - step(0.1h, neighbors);
        color.rgb = lerp(color.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
    }
    return color;
}

#endif
