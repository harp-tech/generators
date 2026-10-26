using System.Globalization;

namespace Harp.Generators;

internal static partial class TemplateHelper
{
    const string CoreNamespace = "Bonsai.Harp";
    const string DocumentationFileExtension = ".md";

    public static string GetDocumentationFileName(string uid)
    {
        return uid.Replace('.', '_') + DocumentationFileExtension;
    }

    public static string GetDocumentationRange(float? minValue, float? maxValue, float? defaultValue)
    {
        var range = "";
        if (minValue.HasValue || maxValue.HasValue)
        {
            var min = minValue?.ToString(CultureInfo.InvariantCulture) ?? "";
            var max = maxValue?.ToString(CultureInfo.InvariantCulture) ?? "";
            range = $"[{min}:{max}]";
        }

        if (defaultValue.HasValue)
        {
            var value = defaultValue.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            range = range.Length > 0 ? $"{value} {range}" : value;
        }

        return range;
    }

    public static string GetDocumentationLink(DeviceMetadata deviceMetadata, string ns, string interfaceType)
    {
        var uid = GetDocumentationUid(deviceMetadata, ns, interfaceType);
        return string.IsNullOrEmpty(uid) ? interfaceType : $"[{interfaceType}](xref:{uid})";
    }

    public static string GetDocumentationInterfaceType(
        DeviceMetadata deviceMetadata,
        string ns,
        string name,
        RegisterInfo register)
    {
        if (string.IsNullOrEmpty(register.InterfaceType) &&
            string.IsNullOrEmpty(register.MaskType) &&
            register.PayloadSpec == null)
        {
            return "";
        }

        return GetDocumentationLink(deviceMetadata, ns, GetInterfaceType(name, register));
    }

    public static string GetDocumentationInterfaceType(
        DeviceMetadata deviceMetadata,
        string ns,
        PayloadMemberInfo member)
    {
        var interfaceType = string.IsNullOrEmpty(member.InterfaceType) ? member.MaskType : member.InterfaceType;
        return string.IsNullOrEmpty(interfaceType) ? "" : GetDocumentationLink(deviceMetadata, ns, interfaceType);
    }

    static string GetDocumentationUid(DeviceMetadata deviceMetadata, string ns, string interfaceType)
    {
        if (deviceMetadata.BitMasks.ContainsKey(interfaceType) ||
            deviceMetadata.GroupMasks.ContainsKey(interfaceType) ||
            IsGeneratedPayloadType(deviceMetadata, interfaceType))
        {
            return $"{ns}.{interfaceType}";
        }

        if (CoreMetadata.Value.BitMasks.ContainsKey(interfaceType) ||
            CoreMetadata.Value.GroupMasks.ContainsKey(interfaceType) ||
            StandardConverterInterfaceTypes.Contains(interfaceType))
        {
            return $"{CoreNamespace}.{interfaceType}";
        }

        return "";
    }

    static bool IsGeneratedPayloadType(DeviceMetadata deviceMetadata, string interfaceType)
    {
        foreach (var register in deviceMetadata.Registers)
        {
            if (register.Value.PayloadSpec != null &&
                interfaceType == GetInterfaceType(register.Key, register.Value))
                return true;
        }

        return false;
    }
}
