#pragma target 5
SamplerState g_samPoint
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Wrap;
    AddressV = Wrap;
};
void blockSampler_float(Texture2DArray tarr, float offset, float2 uv, float3 normal, out float4 rgba)
{
    uint status;
    tarr.Gather(g_samPoint, uv.x, offset, status);
}