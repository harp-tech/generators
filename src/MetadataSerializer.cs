using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Generators;

/// <summary>
/// Provides methods and objects used to serialize device metadata objects.
/// </summary>
public static class MetadataSerializer
{
    /// <summary>
    /// Gets an <see cref="ISerializer"/> instance that can be used to serialize
    /// device metadata objects.
    /// </summary>
    public static readonly ISerializer Instance = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithTypeConverter(RegisterAccessTypeConverter.Instance)
        .WithTypeConverter(MaskValueTypeConverter.Instance)
        .WithTypeConverter(HarpVersionTypeConverter.Instance)
        .WithTypeConverter(HexValueTypeConverter.Instance)
        .WithTypeConverter(LowerCaseEnumTypeConverter.Instance)
        .WithTypeConverter(CamelCaseEnumTypeConverter.Instance)
        .ConfigureDefaultValuesHandling(
            DefaultValuesHandling.OmitNull |
            DefaultValuesHandling.OmitDefaults |
            DefaultValuesHandling.OmitEmptyCollections)
        .Build();
}
