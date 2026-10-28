using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bonsai.Harp;

namespace Harp.Generators;

internal sealed class PythonModule
{
    public string Description = "";
    public string Device = "";
    public int WhoAmI;
    public SortedSet<string> ProtocolImports = [];
    public SortedSet<string> CoreImports = [];
    public SortedSet<string> ExtensionImports = [];
    public bool UsesNDArray;
    public bool IsApplicationDevice;
    public List<PythonEnum> Enums = [];
    public List<PythonPayload> Payloads = [];
    public List<PythonRegister> Registers = [];
}

internal sealed class PythonEnum
{
    public string Name = "";
    public bool IsFlag;
    public string Description = "";
    public List<PythonEnumMember> Members = [];
}

internal sealed class PythonEnumMember
{
    public string Name = "";
    public string Value = "";
    public string Description = "";
}

internal sealed class PythonPayload
{
    public string Name = "";
    public string Description = "";
    public string ElementType = "";
    public int? Length;
    public bool IsAnonymous;
    public string UnwrapType = "";
    public List<PythonField> Fields = [];
}

internal sealed class PythonField
{
    public string Name = "";
    public string Annotation = "";
    public string Descriptor = "";
    public string Description = "";
}

internal enum PythonRegisterKind
{
    Scalar,
    Array,
    Struct
}

internal sealed class PythonRegister
{
    public string Name = "";
    public int Address;
    public PythonRegisterKind Kind;
    public string BaseClass = "";
    public int Length;
    public string PayloadType = "";
    public string PayloadClass = "";
    public string Description = "";
}

internal static partial class TemplateHelper
{
    static readonly HashSet<string> StandardConverterInterfaceTypes = ["HarpVersion"];

    static void AddCoreImport(PythonModule module, DeviceMetadata deviceMetadata, string typeName)
    {
        if (!deviceMetadata.GroupMasks.ContainsKey(typeName) && !deviceMetadata.BitMasks.ContainsKey(typeName))
            module.CoreImports.Add(typeName);
    }

    static void AddConverterImports(PythonModule module, string domainType, string domainConverter)
    {
        if (StandardConverterInterfaceTypes.Contains(domainType))
        {
            module.ProtocolImports.Add(domainType);
            module.ProtocolImports.Add(domainConverter);
        }
        else
        {
            module.ExtensionImports.Add(domainType);
            module.ExtensionImports.Add(domainConverter);
        }
    }

    public static string GetPythonScalarType(PayloadType payloadType)
    {
        return payloadType switch
        {
            PayloadType.U8 => "U8",
            PayloadType.S8 => "S8",
            PayloadType.U16 => "U16",
            PayloadType.S16 => "S16",
            PayloadType.U32 => "U32",
            PayloadType.S32 => "S32",
            PayloadType.U64 => "U64",
            PayloadType.S64 => "S64",
            PayloadType.Float => "Float",
            _ => throw new ArgumentOutOfRangeException(nameof(payloadType)),
        };
    }

    public static string GetPythonNumpyType(PayloadType payloadType)
    {
        return payloadType switch
        {
            PayloadType.U8 => "np.uint8",
            PayloadType.S8 => "np.int8",
            PayloadType.U16 => "np.uint16",
            PayloadType.S16 => "np.int16",
            PayloadType.U32 => "np.uint32",
            PayloadType.S32 => "np.int32",
            PayloadType.U64 => "np.uint64",
            PayloadType.S64 => "np.int64",
            PayloadType.Float => "np.float32",
            _ => throw new ArgumentOutOfRangeException(nameof(payloadType)),
        };
    }

    static bool IsPrimitiveInterfaceType(string interfaceType)
    {
        return GetInterfaceTypeSize(interfaceType, out _, out _);
    }

    public static string GetPythonFieldName(string name)
    {
        return FirmwareNamingConvention.Instance.Apply(name).ToLowerInvariant();
    }

    public static PythonModule BuildPythonModule(DeviceMetadata deviceMetadata)
    {
        var module = new PythonModule
        {
            Description = deviceMetadata.Description,
            Device = deviceMetadata.Device,
            WhoAmI = deviceMetadata.WhoAmI
        };

        foreach (var bitMask in deviceMetadata.BitMasks)
            module.Enums.Add(BuildPythonEnum(bitMask.Key, bitMask.Value.Description, bitMask.Value.Bits, isFlag: true));
        foreach (var groupMask in deviceMetadata.GroupMasks)
            module.Enums.Add(BuildPythonEnum(groupMask.Key, groupMask.Value.Description, groupMask.Value.Values, isFlag: false));

        foreach (var registerMetadata in deviceMetadata.Registers)
            BuildPythonRegister(module, registerMetadata.Key, registerMetadata.Value, deviceMetadata);

        module.ProtocolImports.Add("RegisterBase");
        module.IsApplicationDevice = deviceMetadata.IsApplicationDevice;
        return module;
    }

    static PythonEnum BuildPythonEnum(string name, string description, Dictionary<string, MaskValue> values, bool isFlag)
    {
        var pythonEnum = new PythonEnum { Name = name, IsFlag = isFlag, Description = description };
        foreach (var value in values)
        {
            if (isFlag && value.Value.Value == 0) continue;
            pythonEnum.Members.Add(new PythonEnumMember
            {
                Name = FirmwareNamingConvention.Instance.Apply(value.Key),
                Value = isFlag
                    ? $"0x{value.Value.Value:X}"
                    : value.Value.Value.ToString(CultureInfo.InvariantCulture),
                Description = value.Value.Description
            });
        }
        return pythonEnum;
    }

    static void BuildPythonRegister(PythonModule module, string name, RegisterInfo register, DeviceMetadata deviceMetadata)
    {
        var hasMask = !string.IsNullOrEmpty(register.MaskType);
        var isString = register.InterfaceType == "string";
        var isBoolean = HasBoolConversion(register);
        var isCustom = register.HasConverter
            || (!string.IsNullOrEmpty(register.InterfaceType) && !IsPrimitiveInterfaceType(register.InterfaceType) && !isString && !isBoolean);
        var hasNumericDefault = string.IsNullOrEmpty(register.InterfaceType)
            && register.Length == 0
            && register.DefaultValue.HasValue;
        var className = register.Visibility == RegisterVisibility.Private ? $"_{name}" : name;

        if (register.PayloadSpec == null && !hasMask && !isString && !isBoolean && !isCustom && !hasNumericDefault)
        {
            var isArray = register.Length > 0;
            module.ProtocolImports.Add(isArray ? $"Register{GetPythonScalarType(register.Type)}Array" : $"Register{GetPythonScalarType(register.Type)}");
            module.Registers.Add(new PythonRegister
            {
                Name = className,
                Address = register.Address,
                Kind = isArray ? PythonRegisterKind.Array : PythonRegisterKind.Scalar,
                BaseClass = isArray ? $"Register{GetPythonScalarType(register.Type)}Array" : $"Register{GetPythonScalarType(register.Type)}",
                Length = register.Length,
                Description = register.Description
            });
            return;
        }

        module.ProtocolImports.Add("RegisterBase");
        module.ProtocolImports.Add("PayloadType");

        var payloadName = register.PayloadSpec != null
            ? GetInterfaceType(name, register)
            : $"{name}Payload";
        var payload = module.Payloads.Find(existing => existing.Name == payloadName);
        if (payload == null)
        {
            payload = BuildPythonPayload(module, payloadName, register, deviceMetadata);
            module.Payloads.Add(payload);
        }

        var typeParameter = !string.IsNullOrEmpty(payload.UnwrapType) ? payload.UnwrapType : payload.Name;
        module.Registers.Add(new PythonRegister
        {
            Name = className,
            Address = register.Address,
            Kind = PythonRegisterKind.Struct,
            BaseClass = $"RegisterBase[{typeParameter}]",
            PayloadType = $"PayloadType.{GetPythonScalarType(register.Type)}",
            PayloadClass = payload.Name,
            Description = register.Description
        });
    }

    static PythonPayload BuildPythonPayload(PythonModule module, string payloadName, RegisterInfo register, DeviceMetadata deviceMetadata)
    {
        var elementType = GetPythonNumpyType(register.Type);
        var elementSize = GetPayloadTypeSize(register.Type);
        var payload = new PythonPayload
        {
            Name = payloadName,
            Description = $"Represents the payload of the {RemoveSuffix(payloadName, "Payload")} register.",
            ElementType = elementType,
            Length = register.Length > 0 ? register.Length : null
        };

        if (register.PayloadSpec != null)
        {
            module.ProtocolImports.Add("StructPayload");
            foreach (var member in register.PayloadSpec)
            {
                var field = BuildPythonField(module, member.Key, member.Value, register, deviceMetadata, elementType, elementSize);
                field.Description = member.Value.Description;
                payload.Fields.Add(field);
            }
            return payload;
        }

        module.ProtocolImports.Add("AnonymousPayload");
        payload.IsAnonymous = true;
        payload.Length = null;

        var maskDefaultArgument = GetPythonMaskDefaultArgument(deviceMetadata, register.MaskType, register.DefaultValue);
        if (FindGroupMask(deviceMetadata, register.MaskType) != null)
        {
            module.ProtocolImports.Add("GroupMask");
            AddCoreImport(module, deviceMetadata, register.MaskType);
            var elementMask = (1L << (elementSize * 8)) - 1;
            payload.UnwrapType = register.MaskType;
            payload.Fields.Add(new PythonField
            {
                Name = "__value__",
                Annotation = register.MaskType,
                Descriptor = $"GroupMask(enum={register.MaskType}, mask=0x{elementMask:X}{maskDefaultArgument})"
            });
        }
        else if (IsBitMask(deviceMetadata, register.MaskType))
        {
            module.ProtocolImports.Add("BitMask");
            AddCoreImport(module, deviceMetadata, register.MaskType);
            payload.UnwrapType = register.MaskType;
            payload.Fields.Add(new PythonField
            {
                Name = "__value__",
                Annotation = register.MaskType,
                Descriptor = $"BitMask(enum={register.MaskType}{maskDefaultArgument})"
            });
        }
        else if (register.InterfaceType == "string")
        {
            module.ProtocolImports.Add("Field");
            module.ProtocolImports.Add("StringConverter");
            payload.UnwrapType = "str";
            payload.Fields.Add(new PythonField
            {
                Name = "__value__",
                Annotation = "str",
                Descriptor = $"Field(StringConverter({Math.Max(1, register.Length) * elementSize}))"
            });
        }
        else if (HasBoolConversion(register))
        {
            module.ProtocolImports.Add("Field");
            module.ProtocolImports.Add("BoolConverter");
            var defaultArgument = GetPythonDefaultArgument(
                register, string.Empty, register.Type, register.Length, deviceMetadata);
            payload.UnwrapType = "bool";
            payload.Fields.Add(new PythonField
            {
                Name = "__value__",
                Annotation = "bool",
                Descriptor = $"Field(BoolConverter(){defaultArgument})"
            });
        }
        else if (string.IsNullOrEmpty(register.InterfaceType)
            && string.IsNullOrEmpty(register.MaskType)
            && !register.HasConverter)
        {
            module.ProtocolImports.Add("Field");
            module.ProtocolImports.Add("IdentityConverter");
            var defaultArgument = GetPythonDefaultArgument(
                register, string.Empty, register.Type, register.Length, deviceMetadata);
            payload.UnwrapType = elementType;
            payload.Fields.Add(new PythonField
            {
                Name = "__value__",
                Annotation = elementType,
                Descriptor = $"Field(IdentityConverter({elementType}){defaultArgument})"
            });
        }
        else
        {
            module.ProtocolImports.Add("Field");
            var domainType = register.InterfaceType;
            var domainConverter = $"{domainType}Converter";
            AddConverterImports(module, domainType, domainConverter);
            payload.UnwrapType = domainType;
            payload.Fields.Add(new PythonField
            {
                Name = "__value__",
                Annotation = domainType,
                Descriptor = $"Field({domainConverter}({elementType}))"
            });
        }

        return payload;
    }

    static PythonField BuildPythonField(
        PythonModule module,
        string name,
        PayloadMemberInfo member,
        RegisterInfo register,
        DeviceMetadata deviceMetadata,
        string elementType,
        int elementSize)
    {
        var offset = member.Offset.GetValueOrDefault(0);
        var offsetArgument = offset > 0 ? $", offset={offset}" : string.Empty;
        var typeName = GetDeclaredType(member);
        var defaultArgument = GetPythonDefaultArgument(member, typeName, register.Type, member.Length, deviceMetadata);
        var converterBaseName = name;
        name = GetPythonFieldName(name);
        var annotation = GetPythonMemberAnnotation(module, member, typeName, register, deviceMetadata, elementType);
        var span = Math.Max(1, member.Length) * elementSize;
        var usesMemberConverter = member.HasConverter;
        var isGroupMask = !usesMemberConverter && FindGroupMask(deviceMetadata, typeName) != null;
        var mask = member.Mask ?? (isGroupMask ? (int)((1L << (elementSize * 8)) - 1) : null);
        var maskArgument = mask.HasValue ? $", mask=0x{mask.GetValueOrDefault():X}" : string.Empty;

        string descriptor;
        if (isGroupMask)
        {
            module.ProtocolImports.Add("GroupMask");
            descriptor = $"GroupMask(enum={typeName}";
        }
        else if (!usesMemberConverter && IsBitMask(deviceMetadata, typeName))
        {
            module.ProtocolImports.Add("BitMask");
            descriptor = $"BitMask(enum={typeName}";
        }
        else
        {
            module.ProtocolImports.Add("Field");
            var converter = usesMemberConverter
                ? GetPythonMemberConverter(module, converterBaseName)
                : GetPythonBuiltInConverter(module, member, typeName, annotation, span, elementType);
            descriptor = $"Field({converter}";
        }

        return new PythonField
        {
            Name = name,
            Annotation = annotation,
            Descriptor = $"{descriptor}{maskArgument}{offsetArgument}{defaultArgument})"
        };
    }

    static string GetPythonMemberConverter(PythonModule module, string memberName)
    {
        var memberConverter = $"{memberName}Converter";
        module.ExtensionImports.Add(memberConverter);
        return $"{memberConverter}()";
    }

    static string GetPythonBuiltInConverter(
        PythonModule module,
        PayloadMemberInfo member,
        string typeName,
        string annotation,
        int span,
        string elementType)
    {
        if (member.InterfaceType == "bool")
        {
            module.ProtocolImports.Add("BoolConverter");
            return "BoolConverter()";
        }

        if (member.Mask.HasValue || IsPrimitiveInterfaceType(typeName) ||
            (string.IsNullOrEmpty(typeName) && !IsArray(member, member.Length)))
        {
            module.ProtocolImports.Add("IdentityConverter");
            return $"IdentityConverter({annotation})";
        }

        if (member.InterfaceType == "string")
        {
            module.ProtocolImports.Add("StringConverter");
            return $"StringConverter({span})";
        }

        if (IsArray(member, member.Length))
        {
            module.ProtocolImports.Add("ArrayConverter");
            return $"ArrayConverter({elementType}, {member.Length})";
        }

        var domainConverter = $"{member.InterfaceType}Converter";
        AddConverterImports(module, member.InterfaceType, domainConverter);
        return $"{domainConverter}({elementType})";
    }

    static string GetPythonMemberAnnotation(
        PythonModule module,
        PayloadMemberInfo member,
        string typeName,
        RegisterInfo register,
        DeviceMetadata deviceMetadata,
        string elementType)
    {
        if (member.InterfaceType == "bool")
            return "bool";
        if (member.InterfaceType == "string")
            return "str";
        if (IsArray(member, member.Length))
        {
            module.UsesNDArray = true;
            return $"NDArray[{elementType}]";
        }
        if (string.IsNullOrEmpty(typeName))
            return elementType;
        if (IsPrimitiveInterfaceType(typeName))
            return GetValueNumpyType(member, register.Type);

        if (IsMaskType(deviceMetadata, typeName))
            AddCoreImport(module, deviceMetadata, typeName);
        else if (StandardConverterInterfaceTypes.Contains(typeName))
            module.ProtocolImports.Add(typeName);
        else
            module.ExtensionImports.Add(typeName);
        return typeName;
    }

    static string GetValueNumpyType(ValueInfo info, PayloadType payloadType)
    {
        if (GetInterfaceTypeSize(info.InterfaceType, out var primitiveType, out _))
            return GetPythonNumpyType(primitiveType);
        return GetPythonNumpyType(payloadType);
    }

    static string GetPythonDefaultArgument(
        ValueInfo info,
        string typeName,
        PayloadType payloadType,
        int length,
        DeviceMetadata deviceMetadata)
    {
        var defaultValue = info.DefaultValue;
        if (!defaultValue.HasValue || IsArray(info, length))
            return string.Empty;

        var value = defaultValue.GetValueOrDefault();
        if (FindGroupMask(deviceMetadata, typeName) != null || IsBitMask(deviceMetadata, typeName))
            return GetPythonMaskDefaultArgument(deviceMetadata, typeName, value);

        if (info.InterfaceType == "bool")
            return $", default={(value != 0 ? "True" : "False")}";

        if (info.InterfaceType == "string" || info.HasConverter ||
            (!string.IsNullOrEmpty(info.InterfaceType) && !IsPrimitiveInterfaceType(info.InterfaceType)))
            return string.Empty;

        return $", default={GetValueNumpyType(info, payloadType)}({FormatNumericLiteral(value)})";
    }

    static string GetPythonMaskDefaultArgument(DeviceMetadata deviceMetadata, string maskType, float? defaultValue)
    {
        if (!defaultValue.HasValue)
            return string.Empty;

        var value = (long)defaultValue.GetValueOrDefault();
        var names = value == 0 && IsBitMask(deviceMetadata, maskType) ? null : GetMaskValueNames(deviceMetadata, maskType, value);
        if (names != null)
            return $", default={string.Join(" | ", names.Select(name => $"{maskType}.{FirmwareNamingConvention.Instance.Apply(name)}"))}";
        return FindGroupMask(deviceMetadata, maskType) != null ? $", default={value}" : $", default={maskType}({value})";
    }

    static string FormatNumericLiteral(float value)
    {
        return value == Math.Floor(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }
}
