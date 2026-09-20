namespace Harp.Generators.Tests;

/// <summary>
/// Represents the version of a device component, shadowing the Bonsai.Harp type of the same
/// name for the duration of the test compilation.
/// </summary>
/// <remarks>
/// This type exists only so the test device metadata can declare an interface type of
/// HarpVersion while remaining compatible with XmlSerializer, which requires a public
/// parameterless constructor and settable properties. Remove it once Bonsai.Harp.HarpVersion
/// provides both, and the generated interfaces will bind to that type again.
/// </remarks>
public class HarpVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HarpVersion"/> class.
    /// </summary>
    public HarpVersion()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HarpVersion"/> class with the specified
    /// version components.
    /// </summary>
    /// <param name="major">The major version component.</param>
    /// <param name="minor">The minor version component.</param>
    public HarpVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    /// <summary>
    /// Gets or sets the major version component.
    /// </summary>
    public int Major { get; set; }

    /// <summary>
    /// Gets or sets the minor version component.
    /// </summary>
    public int Minor { get; set; }
}
