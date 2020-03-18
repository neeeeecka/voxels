
void blockSampler_float(Texture2DArray tarr, float index, float2 uv, float3 normal, out float4 rgba)
{
    rgba = float4(uv.x, uv.y, 0, 0);
}