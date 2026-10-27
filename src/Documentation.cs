using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Harp.Generators;

internal static partial class TemplateHelper
{
    const string CoreNamespace = "Bonsai.Harp";
    const string DocumentationFileExtension = ".md";

    public static string GetDocumentationFileName(string uid)
    {
        return uid.Replace('.', '_') + DocumentationFileExtension;
    }

    public static string GetDocumentationValue(float? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? "";
    }

    public static string GetDocumentationRange(float? minValue, float? maxValue)
    {
        var min = GetDocumentationValue(minValue);
        var max = GetDocumentationValue(maxValue);
        if (min.Length > 0 && max.Length > 0) return $"{min} to {max}";
        if (min.Length > 0) return $"at least {min}";
        if (max.Length > 0) return $"at most {max}";
        return "";
    }

    public static string GetDocumentationTableRange(float? minValue, float? maxValue, float? defaultValue)
    {
        var range = minValue.HasValue || maxValue.HasValue
            ? $"[{GetDocumentationValue(minValue)}:{GetDocumentationValue(maxValue)}]"
            : "";
        var value = GetDocumentationValue(defaultValue);
        if (value.Length == 0)
            return range;

        return range.Length > 0 ? $"{value} {range}" : value;
    }

    public static IEnumerable<string> GetDocumentationTable(string[] columns, IReadOnlyList<string[]> rows)
    {
        var usedColumns = Enumerable.Range(0, columns.Length)
            .Where(column => rows.Any(row => row[column].Length > 0))
            .ToArray();
        if (usedColumns.Length == 0)
            yield break;

        yield return GetDocumentationTableRow(usedColumns.Select(column => columns[column]));
        yield return "|" + string.Join("|", usedColumns.Select(_ => "-")) + "|";
        foreach (var row in rows)
            yield return GetDocumentationTableRow(usedColumns.Select(column => row[column]));
    }

    static string GetDocumentationTableRow(IEnumerable<string> cells)
    {
        return $"| {string.Join(" | ", cells)} |";
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
