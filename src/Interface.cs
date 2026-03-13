using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bonsai.Harp;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Generators;

/// <summary>
/// Represents information about Harp device functionality and operation registers.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Specifies the name of the device.
    /// </summary>
    public string Device;

    /// <summary>
    /// Specifies the unique identifier for this device type.
    /// </summary>
    public int WhoAmI;

    /// <summary>
    /// Specifies the version of the device firmware.
    /// </summary>
    public HarpVersion FirmwareVersion;

    /// <summary>
    /// Specifies the version of the device hardware.
    /// </summary>
    public HarpVersion HardwareTargets;

    /// <summary>
    /// Specifies the collection of registers implementing the device function.
    /// </summary>
    public Dictionary<string, RegisterInfo> Registers = [];

    /// <summary>
    /// Specifies the collection of masks available to be used with the different registers.
    /// </summary>
    public Dictionary<string, BitMaskInfo> BitMasks = [];

    /// <summary>
    /// Specifies the collection of group masks available to be used with the different registers.
    /// </summary>
    public Dictionary<string, GroupMaskInfo> GroupMasks = [];
}

/// <summary>
/// Specifies the operations which can be used to access register data.
/// </summary>
[Flags]
public enum RegisterAccess
{
    /// <summary>
    /// Specifies that the register will accept a request to read the payload value. 
    /// </summary>
    Read = 0x1,

    /// <summary>
    /// Specifies that the register will accept a request to write the payload value. 
    /// </summary>
    Write = 0x2,

    /// <summary>
    /// Specifies that the device may send messages to the controller reporting the
    /// contents of the register.
    /// </summary>
    Event = 0x4
}

/// <summary>
/// Specifies whether a register is exposed in the high-level interface.
/// </summary>
public enum RegisterVisibility
{
    /// <summary>
    /// Specifies whether the register is exposed to the high-level interface.
    /// </summary>
    Public,

    /// <summary>
    /// Specifies whether the register is hidden to the high-level interface.
    /// </summary>
    Private
}

/// <summary>
/// Specifies a custom converter which will be used to parse or format the payload or
/// payload member value.
/// </summary>
public enum MemberConverter
{
    /// <summary>
    /// Specifies that no custom conversion is required.
    /// </summary>
    None,

    /// <summary>
    /// Specifies that the custom converter will operate on the specified payload type.
    /// </summary>
    Payload,

    /// <summary>
    /// Specifies that the custom converter should operate directly in raw payload bytes.
    /// </summary>
    RawPayload
}

/// <summary>
/// Represents information about the functionality and operation of a specific register.
/// </summary>
public class RegisterInfo
{
    /// <summary>
    /// Specifies the unique 8-bit address of the register.
    /// </summary>
    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public int Address;

    /// <summary>
    /// Specifies the type of the register payload.
    /// </summary>
    public PayloadType Type;

    /// <summary>
    /// Specifies the length of the register payload.
    /// </summary>
    public int Length;

    /// <summary>
    /// Specifies the expected use of the register.
    /// </summary>
    public RegisterAccess Access = RegisterAccess.Read;

    /// <summary>
    /// Specifies whether the register function is exposed in the high-level interface.
    /// </summary>
    public RegisterVisibility Visibility;

    /// <summary>
    /// Specifies whether register values can be saved in non-volatile memory.
    /// </summary>
    public bool Volatile;

    /// <summary>
    /// Specifies the name of the bit mask or group mask used to represent the payload value.
    /// </summary>
    public string MaskType;

    /// <summary>
    /// Specifies the minimum allowable value for the payload.
    /// </summary>
    public float? MinValue;

    /// <summary>
    /// Specifies the maximum allowable value for the payload.
    /// </summary>
    public float? MaxValue;

    /// <summary>
    /// Specifies the default value for the payload.
    /// </summary>
    public float? DefaultValue;

    /// <summary>
    /// Specifies the name of the type used to represent the payload value in the high-level interface.
    /// </summary>
    public string InterfaceType;

    /// <summary>
    /// Specifies a custom converter which will be used to parse or format the payload value.
    /// </summary>
    public MemberConverter Converter;

    /// <summary>
    /// Specifies a summary description of the register function.
    /// </summary>
    public string Description = "";

    /// <summary>
    /// Specifies a collection of payload members describing the contents
    /// of the raw payload value.
    /// </summary>
    public Dictionary<string, PayloadMemberInfo> PayloadSpec;

    /// <summary>
    /// Gets a value indicating whether a custom converter will be used to parse or
    /// format the payload.
    /// </summary>
    [YamlIgnore]
    public bool HasConverter => Converter > MemberConverter.None;

    /// <summary>
    /// Gets the name of the type used to represent the payload for interface conversions.
    /// </summary>
    [YamlIgnore]
    public string PayloadInterfaceType => Converter == MemberConverter.RawPayload
        ? "ArraySegment<byte>"
        : TemplateHelper.GetInterfaceType(Type, Length);
}

/// <summary>
/// Represents information about a specific payload member.
/// </summary>
public class PayloadMemberInfo
{
    /// <summary>
    /// Specifies the zero-based index at which encoding of this payload member starts.
    /// </summary>
    public int? Offset;

    /// <summary>
    /// Specifies the number of elements used to encode this payload member.
    /// </summary>
    public int? Length;

    /// <summary>
    /// Specifies the mask used to read and write this payload member.
    /// </summary>
    [YamlConverter(typeof(HexValueTypeConverter))]
    public int? Mask;

    /// <summary>
    /// Specifies the name of the bit mask or group mask used to represent this payload member.
    /// </summary>
    public string MaskType;

    /// <summary>
    /// Specifies the minimum allowable value for this payload member.
    /// </summary>
    public float? MinValue;

    /// <summary>
    /// Specifies the maximum allowable value for this payload member.
    /// </summary>
    public float? MaxValue;

    /// <summary>
    /// Specifies the default value for this payload member.
    /// </summary>
    public float? DefaultValue;

    /// <summary>
    /// Specifies the name of the type used to represent this payload member in the high-level interface.
    /// </summary>
    public string InterfaceType;

    /// <summary>
    /// Specifies a custom converter which will be used to parse or format this payload member.
    /// </summary>
    public MemberConverter Converter;

    /// <summary>
    /// Specifies a summary description of this payload member.
    /// </summary>
    public string Description = "";

    /// <summary>
    /// Gets a value indicating whether a custom converter will be used to parse or
    /// format this payload member.
    /// </summary>
    [YamlIgnore]
    public bool HasConverter => Converter > MemberConverter.None;

    /// <summary>
    /// Gets the name of the type used to represent this payload member in the high-level interface.
    /// </summary>
    /// <param name="payloadType">
    /// The raw payload type of the register where this payload member is located.
    /// </param>
    /// <returns>
    /// The high-level interface type.
    /// </returns>
    public string GetConverterInterfaceType(PayloadType payloadType)
    {
        return Converter switch
        {
            MemberConverter.RawPayload => "ArraySegment<byte>",
            MemberConverter.Payload => Length.GetValueOrDefault() > 0
                ? $"ArraySegment<{TemplateHelper.GetInterfaceType(payloadType, 0)}>"
                : TemplateHelper.GetInterfaceType(payloadType, 0),
            _ => TemplateHelper.GetInterfaceType(payloadType, Length.GetValueOrDefault())
        };
    }
}

/// <summary>
/// Represents a bit mask used for reading or writing specific registers.
/// </summary>
public class BitMaskInfo
{
    /// <summary>
    /// Specifies a summary description of the bit mask function.
    /// </summary>
    public string Description = "";

    /// <summary>
    /// Specifies the collection of bit mask values.
    /// </summary>
    public Dictionary<string, MaskValue> Bits = [];

    /// <summary>
    /// Gets the name of the underlying primitive type used to represent a bit mask value
    /// in the high-level interface.
    /// </summary>
    [YamlIgnore]
    public string InterfaceType => TemplateHelper.GetInterfaceType(Bits);
}

/// <summary>
/// Represents a group mask used for reading or writing specific registers.
/// </summary>
public class GroupMaskInfo
{
    /// <summary>
    /// Specifies a summary description of the group mask function.
    /// </summary>
    public string Description = "";

    /// <summary>
    /// Specifies the collection of group mask values.
    /// </summary>
    public Dictionary<string, MaskValue> Values = [];

    /// <summary>
    /// Gets the name of the underlying primitive type used to represent a group mask value
    /// in the high-level interface.
    /// </summary>
    [YamlIgnore]
    public string InterfaceType => TemplateHelper.GetInterfaceType(Values);
}

/// <summary>
/// Represents a bit mask or group mask value.
/// </summary>
public class MaskValue
{
    /// <summary>
    /// Specifies the numerical mask value.
    /// </summary>
    [YamlConverter(typeof(HexValueTypeConverter))]
    public int Value;

    /// <summary>
    /// Specifies a summary description of the mask value function.
    /// </summary>
    public string Description;
}

internal static partial class TemplateHelper
{
    public static DeviceInfo ReadDeviceMetadata(string path)
    {
        using var reader = new StreamReader(path);
        var parser = new MergingParser(new Parser(reader));
        return MetadataDeserializer.Instance.Deserialize<DeviceInfo>(parser);
    }

    public static string GetInterfaceType(string name, RegisterInfo register)
    {
        if (!string.IsNullOrEmpty(register.InterfaceType)) return register.InterfaceType;
        else if (register.PayloadSpec != null) return $"{name}Payload";
        else if (!string.IsNullOrEmpty(register.MaskType)) return register.MaskType;
        else return GetInterfaceType(register.Type, register.Length);
    }

    public static string GetInterfaceType(PayloadMemberInfo member, PayloadType payloadType)
    {
        if (!string.IsNullOrEmpty(member.InterfaceType)) return member.InterfaceType;
        else if (!string.IsNullOrEmpty(member.MaskType)) return member.MaskType;
        else return GetInterfaceType(payloadType, member.Length.GetValueOrDefault());
    }

    public static string GetInterfaceType(PayloadType payloadType)
    {
        return payloadType switch
        {
            PayloadType.U8 => "byte",
            PayloadType.S8 => "sbyte",
            PayloadType.U16 => "ushort",
            PayloadType.S16 => "short",
            PayloadType.U32 => "uint",
            PayloadType.S32 => "int",
            PayloadType.U64 => "ulong",
            PayloadType.S64 => "long",
            PayloadType.Float => "float",
            _ => throw new ArgumentOutOfRangeException(nameof(payloadType)),
        };
    }

    public static string GetInterfaceType(PayloadType payloadType, int payloadLength)
    {
        var baseType = GetInterfaceType(payloadType);
        if (payloadLength > 0) return $"{baseType}[]";
        else return baseType;
    }

    public static string GetInterfaceType(Dictionary<string, MaskValue> maskValues)
    {
        var max = maskValues.Values.Max(field => field.Value);
        if (max <= byte.MaxValue) return "byte";
        if (max <= ushort.MaxValue) return "ushort";
        else return "uint";
    }

    public static bool GetInterfaceTypeSize(string interfaceType, out PayloadType payloadType, out int size)
    {
        payloadType = interfaceType switch
        {
            "byte" => PayloadType.U8,
            "sbyte" => PayloadType.S8,
            "ushort" => PayloadType.U16,
            "short" => PayloadType.S16,
            "uint" => PayloadType.U32,
            "int" => PayloadType.S32,
            "ulong" => PayloadType.U64,
            "long" => PayloadType.S64,
            "float" => PayloadType.Float,
            _ => 0
        };

        size = GetPayloadTypeSize(payloadType);
        return payloadType > 0;
    }

    public static string GetPayloadTypeSuffix(PayloadType payloadType, int payloadLength = 0)
    {
        if (payloadLength > 0)
        {
            var baseType = GetInterfaceType(payloadType);
            return $"Array<{baseType}>";
        }

        return payloadType switch
        {
            PayloadType.U8 => "Byte",
            PayloadType.S8 => "SByte",
            PayloadType.U16 => "UInt16",
            PayloadType.S16 => "Int16",
            PayloadType.U32 => "UInt32",
            PayloadType.S32 => "Int32",
            PayloadType.U64 => "UInt64",
            PayloadType.S64 => "Int64",
            PayloadType.Float => "Single",
            _ => throw new ArgumentOutOfRangeException(nameof(payloadType)),
        };
    }

    public static int GetPayloadTypeSize(PayloadType payloadType)
    {
        return (int)payloadType & 0xF;
    }

    public static string GetRangeAttributeDeclaration(float? minValue, float? maxValue)
    {
        var minValueDeclaration = minValue.HasValue ? minValue.Value.ToString() : "long.MinValue";
        var maxValueDeclaration = maxValue.HasValue ? maxValue.Value.ToString() : "long.MaxValue";
        return $"[Range(min: {minValueDeclaration}, max: {maxValueDeclaration})]";
    }

    public static string GetDefaultValueAssignment(float? defaultValue, float? minValue, PayloadType payloadType)
    {
        defaultValue ??= minValue;
        var suffix = payloadType == PayloadType.Float ? "F" : string.Empty;
        return defaultValue.HasValue ? $" = {defaultValue}{suffix};" : string.Empty;
    }

    public static string GetParseConversion(RegisterInfo register, string expression)
    {
        if (register.PayloadSpec != null || register.HasConverter)
            return $"ParsePayload({expression})";
        else if (register.InterfaceType == "string")
            return $"PayloadMarshal.ReadUtf8String({expression})";
        else
            return GetConversionToInterfaceType(register.InterfaceType ?? register.MaskType, expression);
    }

    public static string GetFormatConversion(RegisterInfo register, string expression)
    {
        if (register.PayloadSpec != null || register.InterfaceType == "string" || register.HasConverter)
            return $"FormatPayload({expression})";
        else
            return GetConversionFromInterfaceType(register.InterfaceType ?? register.MaskType, register.PayloadInterfaceType, expression);
    }

    public static string GetConversionToInterfaceType(string interfaceType, string expression)
    {
        if (string.IsNullOrEmpty(interfaceType)) return expression;
        return interfaceType switch
        {
            "bool" => $"{expression} != 0",
            _ => $"({interfaceType}){expression}",
        };
    }

    public static string GetConversionFromInterfaceType(
        string interfaceType,
        string payloadInterfaceType,
        string expression)
    {
        if (!string.IsNullOrEmpty(interfaceType))
        {
            if (interfaceType == "bool") expression = $"({expression} ? 1 : 0)";
            expression = $"({payloadInterfaceType}){expression}";
        }
        return expression;
    }

    static int GetMaskShift(int mask)
    {
        var lsb = mask & (~mask + 1);
        return (int)Math.Log(lsb, 2);
    }

    public static int GetMaskSelect(GroupMaskInfo groupMask)
    {
        var maxValue = groupMask.Values.Max(member => member.Value.Value);
        if (maxValue == 0)
            return 1;

        var msb = 1 << (int)Math.Log(maxValue, 2);
        return (msb << 1) - 1;
    }

    static int GetMemberSize(
        PayloadMemberInfo member,
        RegisterInfo register,
        DeviceInfo deviceMetadata,
        out string interfaceType,
        out PayloadType payloadType)
    {
        interfaceType = GetInterfaceType(member, register.Type);
        if (deviceMetadata.GroupMasks.TryGetValue(interfaceType, out GroupMaskInfo groupMask))
            interfaceType = groupMask.InterfaceType;
        else if (deviceMetadata.BitMasks.TryGetValue(interfaceType, out BitMaskInfo bitMask))
            interfaceType = bitMask.InterfaceType;

        if (GetInterfaceTypeSize(interfaceType, out payloadType, out int size))
            return size;
        else
            return GetPayloadTypeSize(register.Type);
    }

    public static string GetPayloadMemberParser(
        string name,
        PayloadMemberInfo member,
        string expression,
        RegisterInfo register,
        DeviceInfo deviceMetadata)
    {
        var payloadType = register.Type;
        var memberLength = member.Length.GetValueOrDefault();
        var memberOffset = member.Offset.GetValueOrDefault();
        var payloadInterfaceType = GetInterfaceType(payloadType);
        if (memberLength > 0 && member.InterfaceType != "bool")
        {
            if (member.Converter == MemberConverter.RawPayload)
                throw new NotSupportedException("Raw payload converters inside payload spec is not currently supported.");

            GetMemberSize(member, register, deviceMetadata, out var memberInterfaceType, out var memberPayloadType);
            if (memberInterfaceType == "string")
                return $"PayloadMarshal.ReadUtf8String(new ArraySegment<{payloadInterfaceType}>({expression}, {memberOffset}, {memberLength}))";
            else if (member.Converter == MemberConverter.Payload || memberInterfaceType != GetInterfaceType(payloadType, register.Length))
            {
                expression = $"new ArraySegment<{payloadInterfaceType}>({expression}, {memberOffset}, {memberLength})";
                if (member.Converter == MemberConverter.Payload)
                    return $"ParsePayload{name}({expression})";
                else if (memberPayloadType > 0)
                {
                    expression = $"PayloadMarshal.Read{GetPayloadTypeSuffix(memberPayloadType)}({expression})";
                    return GetConversionToInterfaceType(member.MaskType, expression);
                }
                else
                {
                    return $"PayloadMarshal.Read{memberInterfaceType}({expression})";
                }
            }
            else
                expression = $"PayloadMarshal.GetSubArray({expression}, {memberOffset}, {memberLength})";
        }
        else if (register.Length > 0 && member.Offset.HasValue)
        {
            expression = $"{expression}[{memberOffset}]";
        }
        if (member.Mask.HasValue)
        {
            var mask = member.Mask.Value;
            var shift = GetMaskShift(mask);
            expression = $"({expression} & 0x{mask:X})";
            if (member.InterfaceType != "bool" || member.HasConverter)
            {
                if (shift > 0)
                {
                    expression = $"({expression} >> {shift})";
                }

                expression = $"({payloadInterfaceType}){expression}";
            }
        }

        if (member.HasConverter)
        {
            return $"ParsePayload{name}({expression})";
        }
        return GetConversionToInterfaceType(member.InterfaceType ?? member.MaskType, expression);
    }

    public static string GetPayloadMemberAssignmentFormatter(
        string name,
        PayloadMemberInfo member,
        string expression,
        RegisterInfo register,
        bool assigned)
    {
        var payloadType = register.Type;
        var memberLength = member.Length.GetValueOrDefault();
        var memberOffset = member.Offset.GetValueOrDefault();
        var payloadInterfaceType = GetInterfaceType(payloadType);

        var memberConversion = GetPayloadMemberValueFormatter(
            name,
            member,
            $"value.{name}",
            payloadType);
        if (memberLength > 0 && member.InterfaceType != "bool")
        {
            if (member.Converter == MemberConverter.RawPayload)
                throw new NotSupportedException("Raw payload converters inside payload spec is not currently supported.");
            return $"PayloadMarshal.Write(new ArraySegment<{payloadInterfaceType}>({expression}, {memberOffset}, {memberLength}), {memberConversion})";
        }
        else
        {
            if (member.Mask.HasValue && assigned)
            {
                memberConversion = $" |= {memberConversion}";
            }
            else
                memberConversion = $" = {memberConversion}";
            var memberIndexer = register.Length > 0 && member.Offset.HasValue ? $"[{memberOffset}]" : string.Empty;
            return $"{expression}{memberIndexer}{memberConversion}";
        }
    }

    public static string GetPayloadMemberValueFormatter(
        string name,
        PayloadMemberInfo member,
        string expression,
        PayloadType payloadType)
    {
        if (member.HasConverter)
            expression = $"FormatPayload{name}({expression})";

        if (member.Length > 0)
            return expression;

        var isBoolean = member.InterfaceType == "bool" && !member.HasConverter;
        var payloadInterfaceType = GetInterfaceType(payloadType);
        if (!string.IsNullOrEmpty(member.InterfaceType ?? member.MaskType))
        {
            if (isBoolean)
            {
                var bitValue = member.Mask.HasValue ? $"0x{member.Mask.Value:X}" : "1";
                expression = $"({expression} ? {bitValue} : 0)";
            }
            expression = $"({payloadInterfaceType}){expression}";
        }
        if (member.Mask.HasValue && !isBoolean)
        {
            var mask = member.Mask.Value;
            var shift = GetMaskShift(mask);
            if (shift > 0)
            {
                expression = $"({expression} << {shift})";
            }

            expression = $"({payloadInterfaceType})({expression} & 0x{mask:X})";
        }

        return expression;
    }

    public static string RemoveSuffix(string value, string suffix)
    {
        return value.EndsWith(suffix)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }
}

class HarpVersionTypeConverter : IYamlTypeConverter
{
    public static readonly HarpVersionTypeConverter Instance = new();

    public bool Accepts(Type type)
    {
        return type == typeof(HarpVersion);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return HarpVersion.Parse(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        var scalar = new Scalar(
            AnchorName.Empty,
            TagName.Empty,
            ((HarpVersion)value).ToString(),
            ScalarStyle.DoubleQuoted,
            isPlainImplicit: false,
            isQuotedImplicit: true);
        emitter.Emit(scalar);
    }
}

class HexValueTypeConverter : IYamlTypeConverter
{
    public static readonly HexValueTypeConverter Instance = new();

    public bool Accepts(Type type) => false;

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        return rootDeserializer(type);
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(string.Format($"0x{value:X}")));
    }
}

class MaskValueTypeConverter : IYamlTypeConverter
{
    public static readonly MaskValueTypeConverter Instance = new();
    static readonly IDeserializer ValueDeserializer = new DeserializerBuilder()
        .WithTypeConverter(HexValueTypeConverter.Instance)
        .Build();
    static readonly IValueSerializer DefaultSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithTypeConverter(HexValueTypeConverter.Instance)
        .BuildValueSerializer();

    public bool Accepts(Type type)
    {
        return type == typeof(MaskValue);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume(out MappingStart _))
        {
            var maskValue = new MaskValue();
            while (!parser.TryConsume(out MappingEnd _))
            {
                var key = parser.Consume<Scalar>();
                var value = parser.Consume<Scalar>();
                if (string.IsNullOrEmpty(value.Value))
                {
                    maskValue.Value = ValueDeserializer.Deserialize<int>(key.Value);
                }
                else if (key.Value.Equals(nameof(maskValue.Value), StringComparison.OrdinalIgnoreCase))
                {
                    maskValue.Value = ValueDeserializer.Deserialize<int>(value.Value);
                }
                else if (key.Value.Equals(nameof(maskValue.Description), StringComparison.OrdinalIgnoreCase))
                {
                    maskValue.Description = value.Value;
                }
            }
            return maskValue;
        }
        else
        {
            var scalar = parser.Consume<Scalar>();
            return new MaskValue { Value = ValueDeserializer.Deserialize<int>(scalar.Value) };
        }
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        var maskValue = (MaskValue)value;
        if (string.IsNullOrEmpty(maskValue.Description))
            HexValueTypeConverter.Instance.WriteYaml(emitter, maskValue.Value, typeof(int), serializer);
        else
            DefaultSerializer.SerializeValue(emitter, value, type);
    }
}

class RegisterAccessTypeConverter : IYamlTypeConverter
{
    public static readonly RegisterAccessTypeConverter Instance = new();

    public bool Accepts(Type type)
    {
        return type == typeof(RegisterAccess);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume(out SequenceStart _))
        {
            RegisterAccess value = 0;
            while (parser.TryConsume(out Scalar scalar))
            {
                value |= (RegisterAccess)Enum.Parse(typeof(RegisterAccess), scalar.Value);
            }
            parser.Consume<SequenceEnd>();
            return value;
        }
        else
        {
            var scalar = parser.Consume<Scalar>();
            return (RegisterAccess)Enum.Parse(typeof(RegisterAccess), scalar.Value);
        }
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        var access = (RegisterAccess)value;
        switch (access)
        {
            case RegisterAccess.Read:
            case RegisterAccess.Write:
            case RegisterAccess.Event:
                emitter.Emit(new Scalar(access.ToString()));
                return;
        }

        emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, isImplicit: true, SequenceStyle.Flow));
        if ((access | RegisterAccess.Read) != 0)
            emitter.Emit(new Scalar(nameof(RegisterAccess.Read)));
        if ((access | RegisterAccess.Write) != 0)
            emitter.Emit(new Scalar(nameof(RegisterAccess.Write)));
        if ((access | RegisterAccess.Event) != 0)
            emitter.Emit(new Scalar(nameof(RegisterAccess.Event)));
        emitter.Emit(new SequenceEnd());
    }
}
