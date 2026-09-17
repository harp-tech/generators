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

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonGenerator"/> class with the
    /// specified device metadata.
    /// </summary>
    /// <param name="deviceMetadata">The device metadata object.</param>
    public PythonGenerator(DeviceMetadata deviceMetadata)
    {
        var session = new Dictionary<string, object>
        {
            { "DeviceMetadata", deviceMetadata }
        };
        _deviceTemplate.Initialize(PythonImplementation.DeviceFileName, errors, session);
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
        new(Device: _deviceTemplate.TransformText());
}

/// <summary>
/// Represents the generated Python device interface implementation.
/// </summary>
/// <param name="Device">The generated source code implementing the device register interface.</param>
public record struct PythonImplementation(string Device)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the file storing the device register interface source code.
    /// </summary>
    /// <remarks>
    /// The interface is generated as a package initializer, so the output directory alone
    /// determines the import path of the generated module.
    /// </remarks>
    public const string DeviceFileName = "__init__.py";

    /// <summary>
    /// Returns an enumerator that iterates through all the source code files in the
    /// generated implementation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated implementation files.
    /// </returns>
    public readonly IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        yield return new(DeviceFileName, Device);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
