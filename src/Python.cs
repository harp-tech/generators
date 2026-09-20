using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    const string CoreMetadataResourceName = "Harp.Generators.core.yml";

    static readonly Dictionary<string, string> PrimitiveNumpyTypes = new()
    {
        { "byte", "np.uint8" },
        { "sbyte", "np.int8" },
        { "short", "np.int16" },
        { "ushort", "np.uint16" },
        { "int", "np.int32" },
        { "uint", "np.uint32" },
        { "long", "np.int64" },
        { "ulong", "np.uint64" },
        { "float", "np.float32" },
    };

    static readonly Dictionary<string, int> PrimitiveSizes = new()
    {
        { "byte", 1 }, { "sbyte", 1 },
        { "short", 2 }, { "ushort", 2 },
        { "int", 4 }, { "uint", 4 },
        { "long", 8 }, { "ulong", 8 },
        { "float", 4 },
    };

    static readonly HashSet<string> StandardConverterInterfaceTypes = ["HarpVersion"];

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

    static bool IsBitMask(DeviceMetadata deviceMetadata, string typeName)
    {
        if (deviceMetadata.BitMasks.ContainsKey(typeName)) return true;
        if (deviceMetadata.GroupMasks.ContainsKey(typeName)) return false;
        return CoreMetadata.Value.BitMasks.ContainsKey(typeName);
    }

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
        return PrimitiveNumpyTypes.ContainsKey(interfaceType);
    }

    static string GetMemberTypeName(PayloadMemberInfo member)
    {
        if (!string.IsNullOrEmpty(member.InterfaceType)) return member.InterfaceType;
        else if (!string.IsNullOrEmpty(member.MaskType)) return member.MaskType;
        else return string.Empty;
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
        var isCustom = register.HasConverter
            || (!string.IsNullOrEmpty(register.InterfaceType) && !IsPrimitiveInterfaceType(register.InterfaceType) && !isString);
        var className = register.Visibility == RegisterVisibility.Private ? $"_{name}" : name;

        if (register.PayloadSpec == null && !hasMask && !isString && !isCustom)
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

        var payloadName = register.PayloadSpec != null && !string.IsNullOrEmpty(register.InterfaceType)
            ? register.InterfaceType
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
                Descriptor = $"GroupMask(enum={register.MaskType}, mask=0x{elementMask:X})"
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
                Descriptor = $"BitMask(enum={register.MaskType})"
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
        var typeName = GetMemberTypeName(member);
        var defaultArgument = GetPythonDefaultArgument(member, typeName, register, deviceMetadata);
        var converterBaseName = name;
        name = GetPythonFieldName(name);

        if (FindGroupMask(deviceMetadata, typeName) != null)
        {
            module.ProtocolImports.Add("GroupMask");
            AddCoreImport(module, deviceMetadata, typeName);
            var elementMask = (1L << (elementSize * 8)) - 1;
            var enumMask = member.Mask.GetValueOrDefault((int)elementMask);
            return new PythonField
            {
                Name = name,
                Annotation = typeName,
                Descriptor = $"GroupMask(enum={typeName}, mask=0x{enumMask:X}{offsetArgument}{defaultArgument})"
            };
        }

        if (member.Mask.HasValue)
        {
            var mask = member.Mask.GetValueOrDefault();
            if (member.InterfaceType == "bool")
            {
                module.ProtocolImports.Add("Field");
                module.ProtocolImports.Add("BoolConverter");
                return new PythonField
                {
                    Name = name,
                    Annotation = "bool",
                    Descriptor = $"Field(BoolConverter(), mask=0x{mask:X}{offsetArgument}{defaultArgument})"
                };
            }

            module.ProtocolImports.Add("Field");
            module.ProtocolImports.Add("IdentityConverter");
            var numpyType = GetMemberNumpyType(member, register);
            return new PythonField
            {
                Name = name,
                Annotation = numpyType,
                Descriptor = $"Field(IdentityConverter({numpyType}), mask=0x{mask:X}{offsetArgument}{defaultArgument})"
            };
        }

        module.ProtocolImports.Add("Field");
        var memberLength = member.Length;
        var span = Math.Max(1, memberLength) * elementSize;

        if (member.InterfaceType == "string")
        {
            module.ProtocolImports.Add("StringConverter");
            return new PythonField
            {
                Name = name,
                Annotation = "str",
                Descriptor = $"Field(StringConverter({span}){offsetArgument}{defaultArgument})"
            };
        }

        if (member.InterfaceType == "bool")
        {
            module.ProtocolImports.Add("BoolConverter");
            return new PythonField
            {
                Name = name,
                Annotation = "bool",
                Descriptor = $"Field(BoolConverter(){offsetArgument}{defaultArgument})"
            };
        }

        if (string.IsNullOrEmpty(member.InterfaceType))
        {
            if (memberLength > 0)
            {
                module.ProtocolImports.Add("ArrayConverter");
                module.UsesNDArray = true;
                return new PythonField
                {
                    Name = name,
                    Annotation = $"NDArray[{elementType}]",
                    Descriptor = $"Field(ArrayConverter({elementType}, {memberLength}){offsetArgument}{defaultArgument})"
                };
            }

            module.ProtocolImports.Add("IdentityConverter");
            return new PythonField
            {
                Name = name,
                Annotation = elementType,
                Descriptor = $"Field(IdentityConverter({elementType}){offsetArgument}{defaultArgument})"
            };
        }

        if (IsPrimitiveInterfaceType(member.InterfaceType) && PrimitiveSizes[member.InterfaceType] == span)
        {
            module.ProtocolImports.Add("IdentityConverter");
            var numpyType = PrimitiveNumpyTypes[member.InterfaceType];
            return new PythonField
            {
                Name = name,
                Annotation = numpyType,
                Descriptor = $"Field(IdentityConverter({numpyType}){offsetArgument}{defaultArgument})"
            };
        }

        if (!IsPrimitiveInterfaceType(member.InterfaceType))
        {
            var domainType = member.InterfaceType;
            var domainConverter = $"{domainType}Converter";
            AddConverterImports(module, domainType, domainConverter);
            return new PythonField
            {
                Name = name,
                Annotation = domainType,
                Descriptor = $"Field({domainConverter}({elementType}){offsetArgument}{defaultArgument})"
            };
        }

        var converterName = $"{converterBaseName}Converter";
        module.ExtensionImports.Add(converterName);
        return new PythonField
        {
            Name = name,
            Annotation = PrimitiveNumpyTypes[member.InterfaceType],
            Descriptor = $"Field({converterName}(){offsetArgument}{defaultArgument})"
        };
    }

    static string GetMemberNumpyType(PayloadMemberInfo member, RegisterInfo register)
    {
        if (!string.IsNullOrEmpty(member.InterfaceType) && PrimitiveNumpyTypes.TryGetValue(member.InterfaceType, out var numpyType))
            return numpyType;
        return GetPythonNumpyType(register.Type);
    }

    static string GetPythonDefaultArgument(PayloadMemberInfo member, string typeName, RegisterInfo register, DeviceMetadata deviceMetadata)
    {
        var defaultValue = member.DefaultValue ?? member.MinValue;
        if (!defaultValue.HasValue || member.Length > 0)
            return string.Empty;

        var value = defaultValue.GetValueOrDefault();
        var groupMask = FindGroupMask(deviceMetadata, typeName);
        if (groupMask != null)
        {
            foreach (var entry in groupMask.Values)
            {
                if (entry.Value.Value == (int)value)
                    return $", default={typeName}.{FirmwareNamingConvention.Instance.Apply(entry.Key)}";
            }
            return $", default={(long)value}";
        }

        if (member.InterfaceType == "bool")
            return $", default={(value != 0 ? "True" : "False")}";

        if (member.InterfaceType == "string" || member.HasConverter ||
            (!string.IsNullOrEmpty(member.InterfaceType) && !IsPrimitiveInterfaceType(member.InterfaceType)))
            return string.Empty;

        return $", default={GetMemberNumpyType(member, register)}({FormatNumericLiteral(value)})";
    }

    static string FormatNumericLiteral(float value)
    {
        return value == Math.Floor(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }
}
