namespace FacetedGlass;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/FacetedGlass;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
