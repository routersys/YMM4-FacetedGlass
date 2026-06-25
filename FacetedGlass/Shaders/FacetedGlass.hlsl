Texture2D SourceTexture : register(t0);
SamplerState SourceSampler : register(s0);

cbuffer Constants : register(b0)
{
    float4 inputBounds : packoffset(c0);
    float amount : packoffset(c1.x);
    float cellSize : packoffset(c1.y);
    float relief : packoffset(c1.z);
    float rotation : packoffset(c1.w);
    float refraction : packoffset(c2.x);
    float refractiveIndex : packoffset(c2.y);
    float dispersion : packoffset(c2.z);
    float reflection : packoffset(c2.w);
    float borderWidth : packoffset(c3.x);
    float lightAngle : packoffset(c3.y);
    float lightElevation : packoffset(c3.z);
    int seed : packoffset(c3.w);
};

static const float Sqrt3 = 1.7320508075688772;
static const float WavelengthF = 486.1;
static const float WavelengthD = 587.6;
static const float WavelengthC = 656.3;

uint Hash32(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float VertexHeight(int2 vertex)
{
    uint value = asuint(vertex.x) * 0x9e3779b9u;
    value ^= asuint(vertex.y) * 0x85ebca6bu;
    value ^= asuint(seed) * 0xc2b2ae35u;
    return (Hash32(value) / 4294967295.0) * 2.0 - 1.0;
}

float2 Rotate(float2 coordinate, float angle)
{
    float cosine = cos(angle);
    float sine = sin(angle);
    return float2(coordinate.x * cosine - coordinate.y * sine, coordinate.x * sine + coordinate.y * cosine);
}

float2 LatticePoint(int2 vertex)
{
    return float2(vertex.x + 0.5 * vertex.y, 0.5 * Sqrt3 * vertex.y) * cellSize;
}

float EdgeDistance(float2 coordinate, float2 a, float2 b)
{
    float2 edge = b - a;
    return abs(edge.x * (coordinate.y - a.y) - edge.y * (coordinate.x - a.x)) / max(length(edge), 1e-6);
}

float CauchyIndex(float wavelength)
{
    float deltaFC = 0.04 * dispersion;
    float denominator = 1.0 / (WavelengthF * WavelengthF) - 1.0 / (WavelengthC * WavelengthC);
    float b = deltaFC / denominator;
    float a = refractiveIndex - b / (WavelengthD * WavelengthD);
    return a + b / (wavelength * wavelength);
}

float3 RefractedDirection(float3 normal, float index)
{
    return refract(float3(0.0, 0.0, -1.0), normal, 1.0 / max(index, 1.0001));
}

float2 RefractionOffset(float3 normal, float index)
{
    float3 direction = RefractedDirection(normal, index);
    return direction.xy / max(abs(direction.z), 1e-4) * refraction;
}

float4 SampleAt(float2 uv, float2 pixelStep, float2 scenePosition, float2 offset)
{
    float2 minimum = inputBounds.xy + 0.5;
    float2 maximum = inputBounds.zw - 0.5;
    float2 target = clamp(scenePosition + offset, minimum, maximum);
    return SourceTexture.SampleLevel(SourceSampler, uv + (target - scenePosition) * pixelStep, 0);
}

float3 StraightColor(float4 sample, float3 fallback)
{
    if (sample.a <= 1e-5)
        return fallback;
    return sample.rgb / sample.a;
}

float Schlick(float index, float cosine)
{
    float f0 = (index - 1.0) / (index + 1.0);
    f0 *= f0;
    return f0 + (1.0 - f0) * pow(1.0 - saturate(cosine), 5.0);
}

float4 main(
    float4 position : SV_POSITION,
    float4 scenePosition : SCENE_POSITION,
    float4 uv0 : TEXCOORD0
) : SV_TARGET
{
    float4 source = SourceTexture.SampleLevel(SourceSampler, uv0.xy, 0);
    if (amount <= 0.0 || source.a <= 0.0)
        return source;

    float2 center = 0.5 * (inputBounds.xy + inputBounds.zw);
    float rotationRadians = radians(rotation);
    float2 local = Rotate(scenePosition.xy - center, -rotationRadians);
    float latticeV = 2.0 * local.y / (Sqrt3 * cellSize);
    float latticeU = local.x / cellSize - 0.5 * latticeV;
    int2 baseVertex = int2(floor(latticeU), floor(latticeV));
    float2 fraction = frac(float2(latticeU, latticeV));

    int2 vertex0;
    int2 vertex1;
    int2 vertex2;

    if (fraction.x + fraction.y <= 1.0)
    {
        vertex0 = baseVertex;
        vertex1 = baseVertex + int2(1, 0);
        vertex2 = baseVertex + int2(0, 1);
    }
    else
    {
        vertex0 = baseVertex + int2(1, 1);
        vertex1 = baseVertex + int2(0, 1);
        vertex2 = baseVertex + int2(1, 0);
    }

    float2 point0 = LatticePoint(vertex0);
    float2 point1 = LatticePoint(vertex1);
    float2 point2 = LatticePoint(vertex2);
    float heightScale = cellSize * relief * 0.35;
    float3 surface0 = float3(point0, VertexHeight(vertex0) * heightScale);
    float3 surface1 = float3(point1, VertexHeight(vertex1) * heightScale);
    float3 surface2 = float3(point2, VertexHeight(vertex2) * heightScale);
    float3 normal = normalize(cross(surface1 - surface0, surface2 - surface0));
    if (normal.z < 0.0)
        normal = -normal;
    normal.xy = Rotate(normal.xy, rotationRadians);

    float indexR = CauchyIndex(610.0);
    float indexG = CauchyIndex(550.0);
    float indexB = CauchyIndex(460.0);
    float2 offsetR = RefractionOffset(normal, indexR);
    float2 offsetG = RefractionOffset(normal, indexG);
    float2 offsetB = RefractionOffset(normal, indexB);
    float3 sourceStraight = source.rgb / max(source.a, 1e-5);
    float4 sampleR = SampleAt(uv0.xy, uv0.zw, scenePosition.xy, offsetR);
    float4 sampleG = SampleAt(uv0.xy, uv0.zw, scenePosition.xy, offsetG);
    float4 sampleB = SampleAt(uv0.xy, uv0.zw, scenePosition.xy, offsetB);
    float3 refracted = float3(
        StraightColor(sampleR, sourceStraight).r,
        StraightColor(sampleG, sourceStraight).g,
        StraightColor(sampleB, sourceStraight).b);

    float angleRadians = radians(lightAngle);
    float elevationRadians = radians(lightElevation);
    float cosineElevation = cos(elevationRadians);
    float3 light = normalize(float3(cos(angleRadians) * cosineElevation, sin(angleRadians) * cosineElevation, sin(elevationRadians)));
    float3 view = float3(0.0, 0.0, 1.0);
    float diffuse = 0.78 + 0.22 * saturate(dot(normal, light));
    float fresnel = Schlick(indexG, dot(normal, view));
    float specular = pow(saturate(dot(reflect(-light, normal), view)), 48.0);

    float distance0 = EdgeDistance(local, point0, point1);
    float distance1 = EdgeDistance(local, point1, point2);
    float distance2 = EdgeDistance(local, point2, point0);
    float edgeDistance = min(distance0, min(distance1, distance2));
    float antialias = max(fwidth(edgeDistance), 0.5);
    float border = borderWidth <= 0.0 ? 0.0 : 1.0 - smoothstep(borderWidth, borderWidth + antialias, edgeDistance);

    float reflectance = reflection * (fresnel + specular * 0.75 + border * 0.35);
    float3 glass = refracted * diffuse * (1.0 - saturate(fresnel * reflection * 0.35));
    glass += reflectance.xxx;
    glass = saturate(glass);

    float4 faceted = float4(glass * source.a, source.a);
    return lerp(source, faceted, amount);
}
