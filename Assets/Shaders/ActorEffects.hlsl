#ifndef ACTOR_EFFECTS_INCLUDED
#define ACTOR_EFFECTS_INCLUDED

CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    half4 _EffectColor;
    half4 _HitColor;
    half _EffectAmount;
    half _HitAmount;
CBUFFER_END

half4 ApplyActorEffects(half4 color, float2 uv)
{
    half brightness = dot(color.rgb, half3(0.299h, 0.587h, 0.114h));
    half3 tinted = _EffectColor.rgb * (0.45h + brightness * 0.55h);
    half shimmer = step(0.9h, frac((uv.x + uv.y) * 12.0h + _Time.y * 0.2h));
    tinted += shimmer * 0.08h;
    color.rgb = lerp(color.rgb, tinted, saturate(_EffectAmount));
    color.rgb = lerp(color.rgb, _HitColor.rgb, saturate(_HitAmount) * 0.9h);
    return color;
}

#endif
