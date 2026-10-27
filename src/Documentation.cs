using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Serialization;

namespace Harp.Generators;

internal sealed class ReferencePage
{
    [YamlMember(Alias = "items")]
    public List<ReferenceItem> Items = [];
}

internal sealed class ReferenceItem
{
    [YamlMember(Alias = "uid")]
    public string Uid = "";

    [YamlMember(Alias = "id")]
    public string Id = "";

    [YamlMember(Alias = "name")]
    public string Name = "";

    [YamlMember(Alias = "name.python")]
    public string? PythonName;

    [YamlMember(Alias = "fullName")]
    public string? FullName;

    [YamlMember(Alias = "parent")]
    public string? Parent;

    [YamlMember(Alias = "type")]
    public string Type = "";

    [YamlMember(Alias = "langs")]
    public List<string> Langs = [];

    [YamlMember(Alias = "summary")]
    public string? Summary;

    [YamlMember(Alias = "harp")]
    public ReferenceDetails Harp = new();

    [YamlMember(Alias = "children")]
    public List<string>? Children;
}

internal sealed class ReferenceDetails
{
    [YamlMember(Alias = "register")]
    public RegisterDetails? Register;

    [YamlMember(Alias = "payload")]
    public PayloadDetails? Payload;

    [YamlMember(Alias = "member")]
    public PayloadMemberDetails? Member;

    [YamlMember(Alias = "bitMask")]
    public MaskDetails? BitMask;

    [YamlMember(Alias = "groupMask")]
    public MaskDetails? GroupMask;

    [YamlMember(Alias = "maskValue")]
    public MaskValueDetails? MaskValue;
}

internal sealed class ReferenceType
{
    [YamlMember(Alias = "name")]
    public string Name = "";

    [YamlMember(Alias = "uid")]
    public string? Uid;
}

internal sealed class RegisterDetails
{
    [YamlMember(Alias = "address")]
    public int Address;

    [YamlMember(Alias = "payloadType")]
    public string PayloadType = "";

    [YamlMember(Alias = "length")]
    public int? Length;

    [YamlMember(Alias = "access")]
    public List<string> Access = [];

    [YamlMember(Alias = "defaultValue")]
    public float? DefaultValue;

    [YamlMember(Alias = "minValue")]
    public float? MinValue;

    [YamlMember(Alias = "maxValue")]
    public float? MaxValue;

    [YamlMember(Alias = "interfaceType")]
    public ReferenceType? InterfaceType;

    [YamlMember(Alias = "logFile")]
    public string? LogFile;
}

internal sealed class PayloadDetails
{
    [YamlMember(Alias = "registers")]
    public List<ReferenceType> Registers = [];
}

internal sealed class PayloadMemberDetails
{
    [YamlMember(Alias = "offset")]
    public int? Offset;

    [YamlMember(Alias = "length")]
    public int? Length;

    [YamlMember(Alias = "mask")]
    public string? Mask;

    [YamlMember(Alias = "defaultValue")]
    public float? DefaultValue;

    [YamlMember(Alias = "minValue")]
    public float? MinValue;

    [YamlMember(Alias = "maxValue")]
    public float? MaxValue;

    [YamlMember(Alias = "interfaceType")]
    public ReferenceType? InterfaceType;
}

internal sealed class MaskDetails
{
    [YamlMember(Alias = "type")]
    public string Type = "";
}

internal sealed class MaskValueDetails
{
    [YamlMember(Alias = "value")]
    public string Value = "";
}

internal sealed class ReferenceTocEntry
{
    [YamlMember(Alias = "uid")]
    public string? Uid;

    [YamlMember(Alias = "name")]
    public string Name = "";

    [YamlMember(Alias = "items")]
    public List<ReferenceTocEntry>? Items;
}

internal static partial class TemplateHelper
{
    const string CoreNamespace = "Bonsai.Harp";
    const string CSharpLanguage = "csharp";
    const string PythonLanguage = "python";

    static readonly ISerializer ReferenceSerializer = new SerializerBuilder()
        .WithQuotingNecessaryStrings()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .DisableAliases()
        .Build();

    public static string SerializeReferencePage(ReferencePage page)
    {
        return "### YamlMime:UniversalReference" + Environment.NewLine + ReferenceSerializer.Serialize(page);
    }

    public static string SerializeReferenceToc(List<ReferenceTocEntry> toc)
    {
        return "### YamlMime:TableOfContent" + Environment.NewLine + ReferenceSerializer.Serialize(toc);
    }

    public static IEnumerable<KeyValuePair<string, RegisterInfo>> GetPublicRegisters(DeviceMetadata deviceMetadata)
    {
        return deviceMetadata.Registers.Where(register => register.Value.Visibility == RegisterVisibility.Public);
    }

    public static ReferencePage BuildRegisterReference(DeviceMetadata deviceMetadata, string ns, string name, RegisterInfo register)
    {
        var hasInterfaceType =
            !string.IsNullOrEmpty(register.InterfaceType) ||
            !string.IsNullOrEmpty(register.MaskType) ||
            register.PayloadSpec != null;
        var details = new RegisterDetails
        {
            Address = register.Address,
            PayloadType = register.Type.ToString(),
            Length = register.Length > 0 ? register.Length : null,
            Access = GetAccessNames(register.Access),
            DefaultValue = register.DefaultValue,
            MinValue = register.MinValue,
            MaxValue = register.MaxValue,
            InterfaceType = hasInterfaceType
                ? GetReferenceType(deviceMetadata, ns, GetInterfaceType(name, register))
                : null,
            LogFile = deviceMetadata.IsApplicationDevice
                ? $"{deviceMetadata.Device}_{register.Address}.bin"
                : null
        };

        var item = CreateReferenceItem($"{ns}.{name}", name, name, "class", register.Description);
        item.Harp.Register = details;
        return new ReferencePage { Items = [item] };
    }

    public static ReferencePage BuildPayloadReference(DeviceMetadata deviceMetadata, string ns, string payloadName)
    {
        var registers = deviceMetadata.Registers
            .Where(register => register.Value.PayloadSpec != null && GetInterfaceType(register.Key, register.Value) == payloadName)
            .ToList();
        var payloadSpec = registers[0].Value.PayloadSpec!;
        var uid = $"{ns}.{payloadName}";
        var description = $"Represents the payload of the {RemoveSuffix(payloadName, "Payload")} register.";
        var item = CreateReferenceItem(uid, payloadName, payloadName, "struct", description);
        item.Harp.Payload = new PayloadDetails
        {
            Registers = registers
                .Where(register => register.Value.Visibility == RegisterVisibility.Public)
                .Select(register => new ReferenceType
                {
                    Name = register.Key,
                    Uid = $"{ns}.{register.Key}"
                })
                .ToList()
        };

        var page = new ReferencePage { Items = [item] };
        item.Children = [];
        foreach (var member in payloadSpec)
        {
            var memberUid = $"{uid}.{member.Key}";
            var memberItem = CreateReferenceItem(memberUid, member.Key, GetPythonFieldName(member.Key), "field", member.Value.Description);
            memberItem.Parent = uid;
            memberItem.Harp.Member = new PayloadMemberDetails
            {
                Offset = member.Value.Offset,
                Length = member.Value.Length > 0 ? member.Value.Length : null,
                Mask = member.Value.Mask.HasValue ? $"0x{member.Value.Mask.GetValueOrDefault():X}" : null,
                DefaultValue = member.Value.DefaultValue,
                MinValue = member.Value.MinValue,
                MaxValue = member.Value.MaxValue,
                InterfaceType = GetMemberReferenceType(deviceMetadata, ns, member.Value)
            };
            item.Children.Add(memberUid);
            page.Items.Add(memberItem);
        }

        return page;
    }

    public static ReferencePage BuildMaskReference(string ns, string name, string description, Dictionary<string, MaskValue> values, string interfaceType, bool isFlag)
    {
        var uid = $"{ns}.{name}";
        var item = CreateReferenceItem(uid, name, name, "enum", description);
        var details = new MaskDetails { Type = interfaceType };
        if (isFlag) item.Harp.BitMask = details;
        else item.Harp.GroupMask = details;

        var page = new ReferencePage { Items = [item] };
        item.Children = [];
        foreach (var value in values)
        {
            var valueUid = $"{uid}.{value.Key}";
            var pythonName = isFlag && value.Value.Value == 0
                ? null
                : FirmwareNamingConvention.Instance.Apply(value.Key);
            var valueItem = CreateReferenceItem(valueUid, value.Key, pythonName, "field", value.Value.Description);
            valueItem.Parent = uid;
            valueItem.Harp.MaskValue = new MaskValueDetails
            {
                Value = isFlag
                    ? $"0x{value.Value.Value:X}"
                    : value.Value.Value.ToString(CultureInfo.InvariantCulture)
            };
            item.Children.Add(valueUid);
            page.Items.Add(valueItem);
        }

        return page;
    }

    public static IEnumerable<string> GetPayloadTypeNames(DeviceMetadata deviceMetadata)
    {
        return deviceMetadata.Registers
            .Where(register => register.Value.PayloadSpec != null)
            .Select(register => GetInterfaceType(register.Key, register.Value))
            .Distinct();
    }

    static ReferenceItem CreateReferenceItem(string uid, string name, string? pythonName, string type, string description)
    {
        var item = new ReferenceItem
        {
            Uid = uid,
            Id = name,
            Name = name,
            FullName = uid,
            Type = type,
            Langs = [CSharpLanguage],
            Summary = string.IsNullOrEmpty(description) ? null : description
        };

        if (pythonName != null)
        {
            item.PythonName = pythonName;
            item.Langs.Add(PythonLanguage);
        }

        return item;
    }

    static List<string> GetAccessNames(RegisterAccess access)
    {
        return Enum.GetValues<RegisterAccess>()
            .Where(flag => access.HasFlag(flag))
            .Select(flag => flag.ToString())
            .ToList();
    }

    static ReferenceType? GetMemberReferenceType(DeviceMetadata deviceMetadata, string ns, PayloadMemberInfo member)
    {
        var interfaceType = GetMemberTypeName(member);
        return string.IsNullOrEmpty(interfaceType) ? null : GetReferenceType(deviceMetadata, ns, interfaceType);
    }

    static ReferenceType GetReferenceType(DeviceMetadata deviceMetadata, string ns, string interfaceType)
    {
        return new ReferenceType
        {
            Name = interfaceType,
            Uid = GetReferenceUid(deviceMetadata, ns, interfaceType)
        };
    }

    static string? GetReferenceUid(DeviceMetadata deviceMetadata, string ns, string interfaceType)
    {
        if (deviceMetadata.BitMasks.ContainsKey(interfaceType) ||
            deviceMetadata.GroupMasks.ContainsKey(interfaceType) ||
            GetPayloadTypeNames(deviceMetadata).Contains(interfaceType))
        {
            return $"{ns}.{interfaceType}";
        }

        if (CoreMetadata.Value.BitMasks.ContainsKey(interfaceType) ||
            CoreMetadata.Value.GroupMasks.ContainsKey(interfaceType) ||
            StandardConverterInterfaceTypes.Contains(interfaceType))
        {
            return $"{CoreNamespace}.{interfaceType}";
        }

        return null;
    }
}
