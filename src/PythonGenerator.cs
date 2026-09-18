using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Harp.Generators;

/// <summary>
/// Provides automatic generation of Python device interface implementations.
/// </summary>
public sealed class PythonGenerator
{
    readonly PyDevice _deviceTemplate = new();
    readonly CompilerErrorCollection errors = [];
    readonly bool isPackage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonGenerator"/> class with the
    /// specified device metadata.
    /// </summary>
    /// <param name="deviceMetadata">The device metadata object.</param>
    /// <param name="package">
    /// <see langword="true"/> to generate the interface as a package initializer, so the output
    /// directory alone determines the import path; otherwise the interface is generated as a
    /// single module file.
    /// </param>
    public PythonGenerator(DeviceMetadata deviceMetadata, bool package = false)
    {
        var session = new Dictionary<string, object>
        {
            { "DeviceMetadata", deviceMetadata }
        };
        var fileName = package
            ? PythonImplementation.PackageFileName
            : PythonImplementation.DeviceFileName;
        _deviceTemplate.Initialize(fileName, errors, session);
        isPackage = package;
    }

    /// <summary>
    /// Gets the collection of errors emitted during the code generation process.
    /// </summary>
    public CompilerErrorCollection Errors => errors;

    /// <summary>
    /// Generates a Python device interface implementation complying with the specified metadata file.
    /// </summary>
    /// <returns>The generated device interface implementation.</returns>
    public PythonImplementation GenerateImplementation() =>
        new(Device: _deviceTemplate.TransformText(), Package: isPackage);
}

/// <summary>
/// Represents the generated Python device interface implementation.
/// </summary>
/// <param name="Device">The generated source code implementing the device register interface.</param>
/// <param name="Package">
/// Indicates whether the device register interface is generated as a package initializer.
/// </param>
public record struct PythonImplementation(string Device, bool Package = false)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the file storing the device register interface source code.
    /// </summary>
    public const string DeviceFileName = "device.py";

    /// <summary>
    /// Represents the name for the file storing the device register interface source code when the
    /// interface is generated as a package.
    /// </summary>
    /// <remarks>
    /// Generating the interface under this name makes the output directory alone determine the
    /// import path, so no re-export module is needed to recover it.
    /// </remarks>
    public const string PackageFileName = "__init__.py";

    /// <summary>
    /// Represents the name for the marker file declaring that the generated package is typed.
    /// </summary>
    public const string TypedMarkerFileName = "py.typed";

    /// <summary>
    /// Returns an enumerator that iterates through all the source code files in the
    /// generated implementation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated implementation files.
    /// </returns>
    public readonly IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        yield return new(Package ? PackageFileName : DeviceFileName, Device);
        if (Package)
            yield return new(TypedMarkerFileName, string.Empty);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
