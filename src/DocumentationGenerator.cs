using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Harp.Generators;

/// <summary>
/// Provides automatic generation of device interface reference documentation.
/// </summary>
/// <remarks>
/// Each generated register page is a documentation overwrite file targeting the UID of the
/// corresponding type emitted by the interface generator.
/// </remarks>
public sealed class DocumentationGenerator
{
    readonly DeviceMetadata deviceMetadata;
    readonly CompilerErrorCollection errors = [];
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
    /// Gets the collection of errors emitted during the code generation process.
    /// </summary>
    public CompilerErrorCollection Errors => errors;

    /// <summary>
    /// Generates the device interface reference documentation complying with the specified metadata file.
    /// </summary>
    /// <returns>The generated device interface documentation.</returns>
    public DocumentationImplementation GenerateImplementation()
    {
        var registers = new List<KeyValuePair<string, string>>();
        foreach (var register in deviceMetadata.Registers)
        {
            if (register.Value.Visibility != RegisterVisibility.Public)
                continue;

            var fileName = TemplateHelper.GetDocumentationFileName($"{targetNamespace}.{register.Key}");
            var page = TransformTemplate(new DocRegister(), fileName, register.Key);
            registers.Add(new(fileName, page));
        }

        return new(
            Device: deviceMetadata.IsApplicationDevice
                ? TransformTemplate(new DocDevice(), DocumentationImplementation.DeviceFileName)
                : string.Empty,
            Index: TransformTemplate(new DocIndex(), DocumentationImplementation.IndexFileName),
            Registers: registers);
    }

    string TransformTemplate(TemplateBase template, string fileName, string registerName = "")
    {
        var session = new Dictionary<string, object>
        {
            { "Namespace", targetNamespace },
            { "DeviceMetadata", deviceMetadata },
            { "RegisterName", registerName }
        };
        template.Initialize(fileName, errors, session);
        return template.TransformText();
    }
}

/// <summary>
/// Represents the generated device interface reference documentation.
/// </summary>
/// <param name="Device">
/// The generated page fragment listing the name, identity class and version of the device,
/// or an empty string if the metadata describes the core register set.
/// </param>
/// <param name="Index">
/// The generated page fragment indexing the device register interface.
/// </param>
/// <param name="Registers">
/// The generated register documentation overwrite pages, keyed by file name.
/// </param>
public readonly record struct DocumentationImplementation(
    string Device,
    string Index,
    IReadOnlyList<KeyValuePair<string, string>> Registers)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the file storing the device summary.
    /// </summary>
    public const string DeviceFileName = "device.md";

    /// <summary>
    /// Represents the default name for the file storing the register index.
    /// </summary>
    public const string IndexFileName = "registers.md";

    /// <summary>
    /// Returns an enumerator that iterates through all the pages in the generated documentation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated documentation pages.
    /// </returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        if (!string.IsNullOrEmpty(Device))
            yield return new(DeviceFileName, Device);
        yield return new(IndexFileName, Index);
        foreach (var register in Registers ?? [])
        {
            yield return register;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
