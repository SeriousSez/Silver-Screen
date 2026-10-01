#ifndef SILVERSCREEN_CUTAWAY_LIT_INPUT_INCLUDED
#define SILVERSCREEN_CUTAWAY_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

// URP's Lit passes already provide consistent screen-space dither clipping for LOD
// crossfades. Feed those passes a material-local coverage value so cutaways retain
// their source material's opaque PBR, depth, reflection, and shadow behavior.
half _CutawayOpacity;
#define unity_LODFade float4(_CutawayOpacity, 0.0, 0.0, 0.0)
#define LOD_FADE_CROSSFADE 1

#endif