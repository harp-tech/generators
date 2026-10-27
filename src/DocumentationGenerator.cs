using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Harp.Generators;

/// <summary>
/// Provides automatic generation of language-neutral device interface reference documentation.
/// </summary>
/// <remarks>
/// The reference is generated as DocFX UniversalReference pages describing each public register,
/// payload type and mask, together with the C# and Python names of every type and member. Each
/// page claims the UID of the corresponding type emitted by the interface generator.
/// </remarks>
public sealed class DocumentationGenerator
{
    readonly DeviceMetadata deviceMetadata;
    readonly string targetNamespace;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentationGenerator"/> class with the
    /// specified device metadata and target namespace for generated code.
    /// </summary>
    /// <param name="deviceMetadata">The device metadata object.</param>
    /// <param name="ns">
    /// The target namespace used for all generated code. Page UIDs are resolved against this
    /// namespace, so it must match the namespace passed to the interface generator.
    /// </param>
    public DocumentationGenerator(DeviceMetadata deviceMetadata, string ns)
    {
        this.deviceMetadata = deviceMetadata;
        targetNamespace = ns;
    }

    /// <summary>
    /// Generates the device interface reference documentation complying with the specified metadata file.
    /// </summary>
    /// <returns>The generated device interface reference documentation.</returns>
    public DocumentationImplementation GenerateImplementation()
    {
        var registers = TemplateHelper.GetPublicRegisters(deviceMetadata)
            .Select(register => TemplateHelper.BuildRegisterReference(deviceMetadata, targetNamespace, register.Key, register.Value))
            .ToList();
        var payloads = TemplateHelper.GetPayloadTypeNames(deviceMetadata)
            .Select(payloadName => TemplateHelper.BuildPayloadReference(deviceMetadata, targetNamespace, payloadName))
            .ToList();
        var masks = deviceMetadata.BitMasks
            .Select(mask => TemplateHelper.BuildMaskReference(
                targetNamespace, mask.Key, mask.Value.Description, mask.Value.Bits, mask.Value.InterfaceType, isFlag: true))
            .Concat(deviceMetadata.GroupMasks.Select(mask => TemplateHelper.BuildMaskReference(
                targetNamespace, mask.Key, mask.Value.Description, mask.Value.Values, mask.Value.InterfaceType, isFlag: false)))
            .ToList();

        var toc = new List<ReferenceTocEntry>();
        AddTocSection(toc, "Registers", registers);
        AddTocSection(toc, "Payloads", payloads);
        AddTocSection(toc, "Masks", masks);

        return new(
            Toc: TemplateHelper.SerializeReferenceToc(toc),
            Pages: registers.Concat(payloads).Concat(masks)
                .Select(page => new KeyValuePair<string, string>(
                    DocumentationImplementation.GetPageFileName(page.Items[0].Uid),
                    TemplateHelper.SerializeReferencePage(page)))
                .ToList());
    }

    static void AddTocSection(List<ReferenceTocEntry> toc, string name, List<ReferencePage> pages)
    {
        if (pages.Count == 0)
            return;

        toc.Add(new ReferenceTocEntry
        {
            Name = name,
            Items = pages.Select(page => new ReferenceTocEntry
            {
                Uid = page.Items[0].Uid,
                Name = page.Items[0].Name
            }).ToList()
        });
    }
}

/// <summary>
/// Represents the generated device interface reference documentation.
/// </summary>
/// <param name="Toc">
/// The generated table of contents, grouping the reference pages into registers, payloads and masks.
/// </param>
/// <param name="Pages">
/// The generated UniversalReference pages, keyed by file name.
/// </param>
public readonly record struct DocumentationImplementation(
    string Toc,
    IReadOnlyList<KeyValuePair<string, string>> Pages)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the file storing the table of contents.
    /// </summary>
    public const string TocFileName = "toc.yml";

    /// <summary>
    /// Returns the name of the file storing the reference page with the specified UID.
    /// </summary>
    /// <param name="uid">The UID of the type described by the reference page.</param>
    /// <returns>The name of the reference page file.</returns>
    public static string GetPageFileName(string uid) => $"{uid}.yml";

    /// <summary>
    /// Returns an enumerator that iterates through all the files in the generated documentation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated documentation files.
    /// </returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        yield return new(TocFileName, Toc);
        foreach (var page in Pages ?? [])
        {
            yield return page;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
