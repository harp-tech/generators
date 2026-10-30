using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Bonsai.Harp;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Generators;

/// <summary>
/// Represents metadata about Harp device functionality and operation registers.
/// </summary>
public class DeviceMetadata
{
    /// <summary>
    /// Specifies a summary description of the register interface.
    /// </summary>
    public string Description = "";

    /// <summary>
    /// Specifies the name of the device.
    /// </summary>
    public string Device = "";

    /// <summary>
    /// Specifies the unique identifier for this device type.
    /// </summary>
    public int WhoAmI;

    /// <summary>
    /// Specifies the version of the device firmware.
    /// </summary>
    public HarpVersion? FirmwareVersion;

    /// <summary>
    /// Specifies the version of the device hardware.
    /// </summary>
    public HarpVersion? HardwareTargets;

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

    /// <summary>
    /// Gets a value indicating whether the metadata describes an application device.
    /// </summary>
    /// <remarks>
    /// Metadata declaring no device name describes the core register set. In this case, the
    /// generated interface carries the register classes and the address map only.
    /// </remarks>
    [YamlIgnore]
    public bool IsApplicationDevice => !string.IsNullOrEmpty(Device);

    /// <summary>
    /// Reads device interface metadata from the specified file.
    /// </summary>
    /// <param name="path">The path of the device metadata file.</param>
    /// <returns>
    /// A <see cref="DeviceMetadata"/> object describing the device interface.
    /// </returns>
    public static DeviceMetadata Load(string path)
    {
        using var reader = new StreamReader(path);
        return Load(reader);
    }

    /// <summary>
    /// Reads device interface metadata from the specified text reader.
    /// </summary>
    /// <param name="reader">
    /// A <see cref="TextReader"/> positioned at the start of the device metadata.
    /// </param>
    /// <returns>
    /// A <see cref="DeviceMetadata"/> object describing the device interface.
    /// </returns>
    /// <remarks>
    /// Device metadata should always be read with this method or with <see cref="Parse(string)"/>,
    /// since both resolve the YAML merge keys that make schema reuse possible.
    /// </remarks>
    public static DeviceMetadata Load(TextReader reader)
    {
        var parser = new MergingParser(new Parser(reader));
        var deviceMetadata = MetadataDeserializer.Instance.Deserialize<DeviceMetadata?>(parser)
            ?? throw new YamlException("The device metadata document is empty.");
        deviceMetadata.Validate();
        return deviceMetadata;
    }

    /// <summary>
    /// Reads device interface metadata from the specified string.
    /// </summary>
    /// <param name="text">A string containing the device metadata document.</param>
    /// <returns>
    /// A <see cref="DeviceMetadata"/> object describing the device interface.
    /// </returns>
    /// <remarks>
    /// Equivalent to calling <see cref="Load(TextReader)"/> over the contents of
    /// <paramref name="text"/>. Use <see cref="Load(string)"/> to read from a file.
    /// </remarks>
    public static DeviceMetadata Parse(string text)
    {
        using var reader = new StringReader(text);
        return Load(reader);
    }

    void Validate()
    {
        var errors = new List<string>();
        ValidateDescription(errors, Device, Description);
        foreach (var bitMask in BitMasks)
        {
            ValidateBitMask(errors, bitMask.Key, bitMask.Value);
            ValidateDescription(errors, bitMask.Key, bitMask.Value.Description);
            foreach (var bit in bitMask.Value.Bits)
                ValidateDescription(errors, $"{bitMask.Key}.{bit.Key}", bit.Value.Description);
        }

        foreach (var groupMask in GroupMasks)
        {
            ValidateDescription(errors, groupMask.Key, groupMask.Value.Description);
            foreach (var value in groupMask.Value.Values)
                ValidateDescription(errors, $"{groupMask.Key}.{value.Key}", value.Value.Description);
        }

        var payloadTypes = new Dictionary<string, KeyValuePair<string, RegisterInfo>>();
        foreach (var register in Registers)
        {
            ValidateDescription(errors, register.Key, register.Value.Description);
            ValidateValue(errors, register.Key, register.Value, register.Value.Type);
            ValidatePrimitiveType(errors, register.Key, register.Value, register.Value.Type, register.Value.Length, allowSpan: false);
            ValidateMaskType(errors, register.Key, register.Value, register.Value.Type, memberMask: null);
            ValidatePayloadType(errors, payloadTypes, register);
            if (register.Value.PayloadSpec != null)
            {
                foreach (var member in register.Value.PayloadSpec)
                {
                    var name = $"{register.Key}.{member.Key}";
                    ValidateDescription(errors, name, member.Value.Description);
                    ValidateValue(errors, name, member.Value, register.Value.Type);
                    if (!member.Value.Mask.HasValue)
                        ValidatePrimitiveType(errors, name, member.Value, register.Value.Type, member.Value.Length, allowSpan: true);
                    ValidateMaskType(errors, name, member.Value, register.Value.Type, member.Value.Mask);
                    if (TemplateHelper.HasBoolConversion(member.Value) && member.Value.Length > 1)
                        errors.Add($"({name}) Interface type bool must span exactly one element.");
                }
            }
        }

        if (errors.Count > 0)
            throw new YamlException($"Invalid device metadata:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    static void ValidateDescription(List<string> errors, string name, string description)
    {
        if (description.IndexOfAny(['\r', '\n']) >= 0)
            errors.Add($"({name}) Description must be a single line.");
    }

    static void ValidateValue(List<string> errors, string name, ValueInfo info, PayloadType payloadType)
    {
        var value = info.DefaultValue.GetValueOrDefault();
        if (value < info.MinValue)
            errors.Add($"({name}) Default value {value} is below the minimum value {info.MinValue}.");
        else if (value > info.MaxValue)
            errors.Add($"({name}) Default value {value} is above the maximum value {info.MaxValue}.");

        if (info.InterfaceType == "string" && payloadType != PayloadType.U8)
            errors.Add($"({name}) Interface type string requires payload type U8.");
    }

    static void ValidatePrimitiveType(
        List<string> errors,
        string name,
        ValueInfo info,
        PayloadType payloadType,
        int length,
        bool allowSpan)
    {
        if (info.HasConverter || !TemplateHelper.GetInterfaceTypeSize(info.InterfaceType, out var primitiveType, out var size))
            return;

        if (allowSpan && length > 1)
        {
            var span = length * TemplateHelper.GetPayloadTypeSize(payloadType);
            if (size != span)
                errors.Add($"({name}) Interface type {info.InterfaceType} is {size} bytes, but {length} elements of payload type {payloadType} span {span} bytes.");
        }
        else if (!allowSpan && length > 0)
            errors.Add($"({name}) Interface type {info.InterfaceType} cannot be declared on a register with a length.");
        else if (primitiveType != payloadType)
            errors.Add($"({name}) Interface type {info.InterfaceType} on a single element requires payload type {primitiveType}, but the payload type is {payloadType}.");
    }

    void ValidateMaskType(List<string> errors, string name, ValueInfo info, PayloadType payloadType, uint? memberMask)
    {
        if (string.IsNullOrEmpty(info.MaskType))
            return;

        if (!TemplateHelper.IsMaskType(this, info.MaskType))
        {
            errors.Add($"({name}) Mask type {info.MaskType} does not refer to a bit mask or group mask.");
            return;
        }

        var bitMask = TemplateHelper.FindBitMask(this, info.MaskType);
        if (bitMask is null)
            return;

        var usedMask = bitMask.Bits.Values.Aggregate(0u, (mask, bit) => mask | bit.Value);
        var allowedMask = memberMask ?? TemplateHelper.GetElementMask(payloadType);
        var shift = memberMask.HasValue ? TemplateHelper.GetMaskShift(memberMask.GetValueOrDefault()) : 0;
        if ((((ulong)usedMask << shift) & ~allowedMask) == 0)
            return;

        var excessBits = bitMask.Bits
            .Where(bit => (((ulong)bit.Value.Value << shift) & ~allowedMask) != 0)
            .Select(bit => bit.Key);
        var target = memberMask.HasValue ? $"the member mask 0x{memberMask.GetValueOrDefault():X}" : $"payload type {payloadType}";
        errors.Add($"({name}) Bit mask {info.MaskType} has bits outside {target}: {string.Join(", ", excessBits)}.");
    }

    static void ValidateBitMask(List<string> errors, string maskName, BitMaskInfo mask)
    {
        var declaredBits = new Dictionary<uint, string>();
        foreach (var bit in mask.Bits)
        {
            var name = $"{maskName}.{bit.Key}";
            var value = bit.Value.Value;
            if (value == 0)
            {
                if (bit.Key != "None")
                    errors.Add($"({name}) A bit mask member with value zero must be named None.");
            }
            else if ((value & (value - 1)) != 0)
                errors.Add($"({name}) Bit mask value 0x{value:X} is not a single bit.");
            else if (declaredBits.TryGetValue(value, out var declaredName))
                errors.Add($"({name}) Bit mask value 0x{value:X} is already declared by {maskName}.{declaredName}.");
            else
                declaredBits.Add(value, bit.Key);
        }
    }

    static void ValidatePayloadType(
        List<string> errors,
        Dictionary<string, KeyValuePair<string, RegisterInfo>> payloadTypes,
        KeyValuePair<string, RegisterInfo> register)
    {
        var info = register.Value;
        if (info.PayloadSpec == null || string.IsNullOrEmpty(info.InterfaceType))
            return;

        if (!payloadTypes.TryGetValue(info.InterfaceType, out var declared))
        {
            payloadTypes.Add(info.InterfaceType, register);
            return;
        }

        if (declared.Value.Type != info.Type ||
            declared.Value.Length != info.Length ||
            MetadataSerializer.Instance.Serialize(declared.Value.PayloadSpec) != MetadataSerializer.Instance.Serialize(info.PayloadSpec))
            errors.Add($"({register.Key}) Payload type {info.InterfaceType} is declared by register {declared.Key} with a different payload type, length or payload spec.");
    }
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
/// Represents information about how a value is represented and constrained.
/// </summary>
public abstract class ValueInfo
{
    /// <summary>
    /// Specifies the name of the bit mask or group mask used to represent the value.
    /// </summary>
    public string MaskType = "";

    /// <summary>
    /// Specifies the minimum allowable value.
    /// </summary>
    public double? MinValue;

    /// <summary>
    /// Specifies the maximum allowable value.
    /// </summary>
    public double? MaxValue;

    /// <summary>
    /// Specifies the default value.
    /// </summary>
    public double? DefaultValue;

    /// <summary>
    /// Specifies the name of the type used to represent the value in the high-level interface.
    /// </summary>
    public string InterfaceType = "";

    /// <summary>
    /// Specifies a custom converter which will be used to parse or format the value.
    /// </summary>
    public MemberConverter Converter;

    /// <summary>
    /// Gets a value indicating whether a custom converter will be used to parse or
    /// format the value.
    /// </summary>
    [YamlIgnore]
    public bool HasConverter => Converter > MemberConverter.None;
}

/// <summary>
/// Represents information about the functionality and operation of a specific register.
/// </summary>
public class RegisterInfo : ValueInfo
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
    /// Specifies a summary description of the register function.
    /// </summary>
    [YamlMember(Order = 1)]
    public string Description = "";

    /// <summary>
    /// Specifies a collection of payload members describing the contents
    /// of the raw payload value.
    /// </summary>
    [YamlMember(Order = 2)]
    public Dictionary<string, PayloadMemberInfo>? PayloadSpec;
}

/// <summary>
/// Represents information about a specific payload member.
/// </summary>
public class PayloadMemberInfo : ValueInfo
{
    /// <summary>
    /// Specifies the zero-based index at which encoding of this payload member starts.
    /// </summary>
    public int? Offset;

    /// <summary>
    /// Specifies the number of elements used to encode this payload member.
    /// </summary>
    public int Length;

    /// <summary>
    /// Specifies the mask used to read and write this payload member.
    /// </summary>
    [YamlConverter(typeof(HexValueTypeConverter))]
    public uint? Mask;

    /// <summary>
    /// Specifies a summary description of this payload member.
    /// </summary>
    [YamlMember(Order = 1)]
    public string Description = "";
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
    public uint Value;

    /// <summary>
    /// Specifies a summary description of the mask value function.
    /// </summary>
    public string Description = "";
}

internal static partial class TemplateHelper
{
    const string CoreMetadataResourceName = "Harp.Generators.core.yml";

    static readonly Regex WordPattern = new(@"(?:<[^>]*>|[^\s<])+");

    static readonly Lazy<DeviceMetadata> CoreMetadata = new(() =>
    {
        using var stream = typeof(TemplateHelper).Assembly.GetManifestResourceStream(CoreMetadataResourceName)
            ?? throw new InvalidOperationException($"Embedded metadata '{CoreMetadataResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return DeviceMetadata.Load(reader);
    });

    static GroupMaskInfo? FindGroupMask(DeviceMetadata deviceMetadata, string typeName)
    {
        if (deviceMetadata.GroupMasks.TryGetValue(typeName, out var groupMask)) return groupMask;
        if (deviceMetadata.BitMasks.ContainsKey(typeName)) return null;
        return CoreMetadata.Value.GroupMasks.TryGetValue(typeName, out groupMask) ? groupMask : null;
    }

    public static BitMaskInfo? FindBitMask(DeviceMetadata deviceMetadata, string typeName)
    {
        if (deviceMetadata.BitMasks.TryGetValue(typeName, out var bitMask)) return bitMask;
        if (deviceMetadata.GroupMasks.ContainsKey(typeName)) return null;
        return CoreMetadata.Value.BitMasks.TryGetValue(typeName, out bitMask) ? bitMask : null;
    }

    static bool IsBitMask(DeviceMetadata deviceMetadata, string typeName)
    {
        return FindBitMask(deviceMetadata, typeName) != null;
    }

    public static bool IsMaskType(DeviceMetadata deviceMetadata, string typeName)
    {
        return FindGroupMask(deviceMetadata, typeName) != null || IsBitMask(deviceMetadata, typeName);
    }

    static List<string>? GetMaskValueNames(DeviceMetadata deviceMetadata, string maskType, uint value)
    {
        var groupMask = FindGroupMask(deviceMetadata, maskType);
        var entries = groupMask?.Values ?? FindBitMask(deviceMetadata, maskType)?.Bits;
        if (entries is null)
            return null;

        foreach (var entry in entries)
        {
            if (entry.Value.Value == value)
                return [entry.Key];
        }

        if (groupMask != null || value == 0)
            return null;

        var remaining = value;
        var names = new List<string>();
        foreach (var entry in entries)
        {
            var bits = entry.Value.Value;
            if (bits != 0 && (remaining & bits) == bits)
            {
                names.Add(entry.Key);
                remaining &= ~bits;
            }
        }
        return remaining == 0 ? names : null;
    }

    public static string GetInterfaceType(string name, RegisterInfo register)
    {
        if (register.PayloadSpec != null && string.IsNullOrEmpty(register.InterfaceType)) return $"{name}Payload";
        else return GetInterfaceType(register, register.Type, register.Length);
    }

    public static string GetInterfaceType(PayloadMemberInfo member, PayloadType payloadType)
    {
        return GetInterfaceType(member, payloadType, member.Length);
    }

    static string GetInterfaceType(ValueInfo info, PayloadType payloadType, int length)
    {
        var declaredType = GetDeclaredType(info);
        return string.IsNullOrEmpty(declaredType) ? GetInterfaceType(payloadType, length) : declaredType;
    }

    static string GetDeclaredType(ValueInfo info)
    {
        return string.IsNullOrEmpty(info.InterfaceType) ? info.MaskType : info.InterfaceType;
    }

    static bool IsArray(ValueInfo info, int length)
    {
        return length > 0 && string.IsNullOrEmpty(GetDeclaredType(info));
    }

    public static bool HasBoolConversion(ValueInfo info)
    {
        return info.InterfaceType == "bool" && !info.HasConverter;
    }

    static bool UsesPayloadSpan(PayloadMemberInfo member)
    {
        return member.Length > 0 && !HasBoolConversion(member);
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

    public static ulong GetElementMask(PayloadType payloadType)
    {
        var size = GetPayloadTypeSize(payloadType);
        return size >= 8 ? ulong.MaxValue : (1UL << (size * 8)) - 1;
    }

    public static string GetDescriptionText(string description)
    {
        var text = string.Join(" ", description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length == 0 || text.EndsWith('.') ? text : $"{text}.";
    }

    public static IEnumerable<string> GetDocumentationComment(string indent, string xmlText, int maxLineLength)
    {
        var words = WordPattern.Matches(xmlText).Select(match => match.Value);
        foreach (var line in WrapWords(words, maxLineLength - "/// ".Length))
        {
            yield return $"{indent}/// {line}";
        }
    }

    static IEnumerable<string> WrapWords(IEnumerable<string> words, int width)
    {
        var line = new StringBuilder();
        foreach (var word in words)
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString();
    }

    public static string GetXmlText(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public static string GetRangeAttributeDeclaration(double? minValue, double? maxValue)
    {
        var minValueDeclaration = minValue.HasValue ? minValue.Value.ToString() : "long.MinValue";
        var maxValueDeclaration = maxValue.HasValue ? maxValue.Value.ToString() : "long.MaxValue";
        return $"[Range(min: {minValueDeclaration}, max: {maxValueDeclaration})]";
    }

    public static string GetDefaultValueAssignment(
        DeviceMetadata deviceMetadata,
        ValueInfo info,
        PayloadType payloadType,
        string interfaceType,
        int length)
    {
        var maskType = info.MaskType;
        var defaultValue = info.DefaultValue;
        if (defaultValue.HasValue)
        {
            if (interfaceType == "bool")
                return $" = {(defaultValue.GetValueOrDefault() != 0 ? "true" : "false")};";
            if (interfaceType == maskType)
            {
                var value = (uint)defaultValue.GetValueOrDefault();
                var names = GetMaskValueNames(deviceMetadata, maskType, value);
                return names is null
                    ? $" = ({maskType}){value};"
                    : $" = {string.Join(" | ", names.Select(name => $"{maskType}.{name}"))};";
            }

            var suffix = payloadType == PayloadType.Float ? "F" : string.Empty;
            return $" = {defaultValue}{suffix};";
        }

        if (interfaceType == "string")
            return " = string.Empty;";
        else if (interfaceType.EndsWith("[]"))
            return $" = new {interfaceType.Substring(0, interfaceType.Length - 2)}[{length}];";
        else if (interfaceType == maskType || interfaceType == "bool")
            return string.Empty;
        else if (GetInterfaceTypeSize(interfaceType, out _, out _))
            return string.Empty;
        else return " = new();";
    }

    public static string GetPayloadInterfaceType(RegisterInfo register)
    {
        return register.Converter == MemberConverter.RawPayload
            ? "byte[]"
            : GetInterfaceType(register.Type, register.Length);
    }

    public static string GetConverterParameterDeclaration(
        PayloadMemberInfo member,
        PayloadType payloadType,
        string parameterName)
    {
        return member.Converter switch
        {
            MemberConverter.RawPayload => $"ArraySegment<byte> {parameterName}",
            MemberConverter.Payload => UsesPayloadSpan(member)
                ? $"{GetInterfaceType(payloadType, 0)}[] {parameterName}, int offset, int count"
                : $"{GetInterfaceType(payloadType, 0)} {parameterName}",
            _ => $"{GetInterfaceType(payloadType, member.Length)} {parameterName}"
        };
    }

    public static string GetConverterReturnType(PayloadMemberInfo member, PayloadType payloadType)
    {
        return UsesPayloadSpan(member)
            ? $"{GetInterfaceType(payloadType, 0)}[]"
            : GetInterfaceType(payloadType, 0);
    }

    public static string GetParseConversion(RegisterInfo register, string expression)
    {
        if (register.Converter == MemberConverter.RawPayload)
            return $"ParsePayload({expression}.Array!, {expression}.Offset, {expression}.Count)";
        else if (register.PayloadSpec != null || register.HasConverter)
            return $"ParsePayload({expression})";
        else if (register.InterfaceType == "string")
            return $"PayloadMarshal.ReadUtf8String({expression}.Array!, {expression}.Offset, {expression}.Count)";
        else
            return GetConversionToInterfaceType(GetDeclaredType(register), expression);
    }

    public static string GetFormatConversion(RegisterInfo register, string expression)
    {
        if (register.PayloadSpec != null || register.InterfaceType == "string" || register.HasConverter)
            return $"FormatPayload({expression})";
        else
            return GetConversionFromInterfaceType(
                GetDeclaredType(register),
                GetPayloadInterfaceType(register),
                expression);
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

    public static int GetMaskShift(uint mask)
    {
        return BitOperations.TrailingZeroCount(mask);
    }

    public static uint GetMaskSelect(GroupMaskInfo groupMask)
    {
        var maxValue = groupMask.Values.Max(member => member.Value.Value);
        if (maxValue == 0)
            return 1;

        var msb = BitOperations.Log2(maxValue);
        return msb == 31 ? uint.MaxValue : (2u << msb) - 1;
    }

    static int GetMemberSize(
        PayloadMemberInfo member,
        RegisterInfo register,
        DeviceMetadata deviceMetadata,
        out string interfaceType,
        out PayloadType payloadType)
    {
        interfaceType = GetInterfaceType(member, register.Type);
        if (deviceMetadata.GroupMasks.TryGetValue(interfaceType, out var groupMask))
            interfaceType = groupMask.InterfaceType;
        else if (deviceMetadata.BitMasks.TryGetValue(interfaceType, out var bitMask))
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
        DeviceMetadata deviceMetadata)
    {
        var payloadType = register.Type;
        var memberLength = member.Length;
        var memberOffset = member.Offset.GetValueOrDefault();
        var payloadInterfaceType = GetInterfaceType(payloadType);
        if (UsesPayloadSpan(member))
        {
            if (member.Converter == MemberConverter.RawPayload)
                throw new NotSupportedException("Raw payload converters inside payload spec is not currently supported.");

            GetMemberSize(member, register, deviceMetadata, out var memberInterfaceType, out var memberPayloadType);
            if (memberInterfaceType == "string")
                return $"PayloadMarshal.ReadUtf8String({expression}, {memberOffset}, {memberLength})";
            else if (member.Converter == MemberConverter.Payload || memberInterfaceType != GetInterfaceType(payloadType, register.Length))
            {
                var extentArguments = $"{expression}, {memberOffset}, {memberLength}";
                expression = $"{expression}, {memberOffset}";
                if (member.Converter == MemberConverter.Payload)
                    return $"ParsePayload{name}({extentArguments})";
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
            if (!HasBoolConversion(member))
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
        return GetConversionToInterfaceType(GetDeclaredType(member), expression);
    }

    public static string GetPayloadMemberAssignmentFormatter(
        string name,
        PayloadMemberInfo member,
        string expression,
        RegisterInfo register,
        bool assigned)
    {
        var payloadType = register.Type;
        var memberLength = member.Length;
        var memberOffset = member.Offset.GetValueOrDefault();

        var memberConversion = GetPayloadMemberValueFormatter(
            name,
            member,
            $"value.{name}",
            payloadType);
        if (UsesPayloadSpan(member))
        {
            if (member.Converter == MemberConverter.RawPayload)
                throw new NotSupportedException("Raw payload converters inside payload spec is not currently supported.");

            var memberInterfaceType = GetInterfaceType(member, payloadType);
            if (!member.HasConverter && GetInterfaceTypeSize(memberInterfaceType, out _, out _))
                return $"PayloadMarshal.Write({expression}, {memberOffset}, {memberConversion})";

            return $"PayloadMarshal.Write({expression}, {memberOffset}, {memberLength}, {memberConversion})";
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

        if (UsesPayloadSpan(member))
            return expression;

        var isBoolean = HasBoolConversion(member);
        var payloadInterfaceType = GetInterfaceType(payloadType);
        var memberInterfaceType = GetDeclaredType(member);
        if (!string.IsNullOrEmpty(memberInterfaceType))
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

    public bool Accepts(Type type) => type == typeof(HarpVersion);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return HarpVersion.Parse(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not HarpVersion version)
            return;

        var scalar = new Scalar(
            AnchorName.Empty,
            TagName.Empty,
            version.ToString(),
            ScalarStyle.DoubleQuoted,
            isPlainImplicit: false,
            isQuotedImplicit: true);
        emitter.Emit(scalar);
    }
}

class HexValueTypeConverter : IYamlTypeConverter
{
    public static readonly HexValueTypeConverter Instance = new();

    static readonly IDeserializer ScalarDeserializer = new DeserializerBuilder().Build();

    public bool Accepts(Type type) => false;

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        return ParseMask(parser.Consume<Scalar>());
    }

    public static uint ParseMask(Scalar scalar)
    {
        var value = ScalarDeserializer.Deserialize<ulong>(scalar.Value);
        if (value > uint.MaxValue)
            throw new YamlException(scalar.Start, scalar.End, $"Mask value {scalar.Value} does not fit in 32 bits.");
        return (uint)value;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(string.Format($"0x{value:X}")));
    }
}

class MaskValueTypeConverter : IYamlTypeConverter
{
    public static readonly MaskValueTypeConverter Instance = new();
    static readonly IValueSerializer DefaultSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithTypeConverter(HexValueTypeConverter.Instance)
        .BuildValueSerializer();

    public bool Accepts(Type type) => type == typeof(MaskValue);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume<MappingStart>(out var _))
        {
            var maskValue = new MaskValue();
            while (!parser.TryConsume<MappingEnd>(out var _))
            {
                var key = parser.Consume<Scalar>();
                var value = parser.Consume<Scalar>();
                if (string.IsNullOrEmpty(value.Value))
                {
                    maskValue.Value = HexValueTypeConverter.ParseMask(key);
                }
                else if (key.Value.Equals(nameof(maskValue.Value), StringComparison.OrdinalIgnoreCase))
                {
                    maskValue.Value = HexValueTypeConverter.ParseMask(value);
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
            return new MaskValue { Value = HexValueTypeConverter.ParseMask(scalar) };
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not MaskValue maskValue)
            return;

        if (string.IsNullOrEmpty(maskValue.Description))
            HexValueTypeConverter.Instance.WriteYaml(emitter, maskValue.Value, typeof(uint), serializer);
        else
            DefaultSerializer.SerializeValue(emitter, value, type);
    }
}

class RegisterAccessTypeConverter : IYamlTypeConverter
{
    public static readonly RegisterAccessTypeConverter Instance = new();

    public bool Accepts(Type type) => type == typeof(RegisterAccess);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.TryConsume<SequenceStart>(out var _))
        {
            RegisterAccess value = 0;
            while (parser.TryConsume<Scalar>(out var scalar))
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

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not RegisterAccess access)
            return;

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
