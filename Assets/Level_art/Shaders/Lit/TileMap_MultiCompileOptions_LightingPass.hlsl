
// NOTE: These are the shader options that we need for the tile map shaders
#pragma only_renderers d3d11 glcore gles3 metal vulkan
#pragma target 2.0

// -------------------------------------
// Universal Pipeline keywords
#pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _SHADOWS_SOFT
#pragma multi_compile_fragment _LIGHT_COOKIES

// -------------------------------------
// Unity defined keywords
#pragma multi_compile _ LIGHTMAP_ON
