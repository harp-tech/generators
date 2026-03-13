using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Generators;

/// <summary>
/// Provides methods and objects used to deserialize device metadata objects.
/// </summary>
public static class MetadataDeserializer
{
    /// <summary>
    /// Gets an <see cref="IDeserializer"/> instance that can be used to deserialize
    /// device metadata objects.
    /// </summary>
    public static readonly IDeserializer Instance = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithTypeConverter(RegisterAccessTypeConverter.Instance)
        .WithTypeConverter(MaskValueTypeConverter.Instance)
        .WithTypeConverter(HarpVersionTypeConverter.Instance)
        .WithTypeConverter(HexValueTypeConverter.Instance)
        .WithTypeConverter(LowerCaseEnumTypeConverter.Instance)
        .WithTypeConverter(CamelCaseEnumTypeConverter.Instance)
        .WithPortPinInfoTypeConverter()
        .Build();

    static DeserializerBuilder WithPortPinInfoTypeConverter(this DeserializerBuilder deserializerBuilder)
    {
        var portPinConverter = new PortPinInfoTypeConverter(deserializerBuilder.Build());
        return deserializerBuilder.WithTypeConverter(portPinConverter);
    }
}
