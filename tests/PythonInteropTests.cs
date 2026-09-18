using System.Globalization;
using System.Reflection;
using System.Text;
using Bonsai.Harp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Harp.Generators.Tests;

[TestClass]
public sealed class PythonInteropTests
{
    const string DeviceMetadataFileName = "device.yml";
    const int FrameCount = 3;

    static string ResolveOutputDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("HARP_INTEROP_OUTPUT");
        if (!string.IsNullOrEmpty(configured))
            return configured;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Harp.Generators.sln")))
            directory = directory.Parent;
        var root = directory?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(root, "artifacts", "python-interop");
    }

    [TestMethod]
    public void GenerateInteropFixtures()
    {
        var metadataPath = TestHelper.GetMetadataPath(DeviceMetadataFileName);
        var deviceMetadata = TestHelper.ReadDeviceMetadata(metadataPath);

        var interfaceImplementation = new InterfaceGenerator(deviceMetadata, typeof(PythonInteropTests).Namespace ?? "").GenerateImplementation();
        var pythonImplementation = new PythonGenerator(deviceMetadata, package: true).GenerateImplementation();
        var payloadExtensions = TestHelper.GetManifestResourceText("PayloadMarshal.cs");
        var customImplementation = TestHelper.GetManifestResourceText("EmbeddedSources.device.cs");
        var assembly = CompilerTestHelper.CompileAndLoadFromSource(
            interfaceImplementation.Device,
            interfaceImplementation.AsyncDevice,
            payloadExtensions,
            customImplementation);

        var outputDirectory = ResolveOutputDirectory();
        var packageDirectory = Path.Combine(outputDirectory, "harp_device");
        var dataDirectory = Path.Combine(outputDirectory, "data");
        Directory.CreateDirectory(packageDirectory);
        Directory.CreateDirectory(dataDirectory);

        foreach (var sourceFile in pythonImplementation)
        {
            File.WriteAllText(Path.Combine(packageDirectory, sourceFile.Key), sourceFile.Value);
        }

        File.WriteAllText(Path.Combine(packageDirectory, "converters.py"), TestHelper.GetManifestResourceText("Python.converters.py"));
        File.WriteAllText(Path.Combine(outputDirectory, DeviceMetadataFileName), File.ReadAllText(metadataPath));

        var manifest = new StringBuilder();
        manifest.Append("[\n");
        var first = true;
        foreach (var registerMetadata in deviceMetadata.Registers)
        {
            var register = registerMetadata.Value;
            if (register.Visibility != RegisterVisibility.Public)
                continue;

            if (register.PayloadSpec == null && register.HasConverter)
                continue;

            var registerType = assembly.GetType($"{typeof(PythonInteropTests).Namespace}.{registerMetadata.Key}")
                ?? throw new InvalidOperationException($"Generated register type not found: {registerMetadata.Key}");
            var fromPayload = FindFromPayloadMethod(registerType);
            var valueType = fromPayload.GetParameters()[1].ParameterType;

            var expected = new List<string>();
            using (var binaryStream = new FileStream(Path.Combine(dataDirectory, $"{registerMetadata.Key}.bin"), FileMode.Create))
            {
                for (int seed = 1; seed <= FrameCount; seed++)
                {
                    var value = InteropValue.Build(valueType, register, seed);
                    var message = (HarpMessage)fromPayload.Invoke(null, [MessageType.Write, value])!;
                    binaryStream.Write(message.MessageBytes, 0, message.MessageBytes.Length);
                    expected.Add(InteropValue.Canonicalize(value, register));
                }
            }

            if (!first) manifest.Append(",\n");
            first = false;
            manifest.Append("    {\n");
            manifest.Append($"        \"name\": \"{registerMetadata.Key}\",\n");
            manifest.Append($"        \"address\": {register.Address},\n");
            manifest.Append($"        \"frames\": {FrameCount},\n");
            manifest.Append("        \"expected\": [").Append(string.Join(", ", expected)).Append("]\n");
            manifest.Append("    }");
        }
        manifest.Append("\n]\n");
        File.WriteAllText(Path.Combine(outputDirectory, "manifest.json"), manifest.ToString());

        Assert.IsTrue(Directory.GetFiles(dataDirectory, "*.bin").Length > 0);
    }

    static MethodInfo FindFromPayloadMethod(Type registerType)
    {
        foreach (var method in registerType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "FromPayload") continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 2 && parameters[0].ParameterType == typeof(MessageType))
                return method;
        }
        throw new InvalidOperationException($"No FromPayload(MessageType, value) method on {registerType.Name}.");
    }
}

static class InteropValue
{
    public static object Build(Type valueType, RegisterInfo register, int seed)
    {
        if (register.PayloadSpec != null)
        {
            var instance = Activator.CreateInstance(valueType)!;
            foreach (var member in register.PayloadSpec)
            {
                var field = valueType.GetField(member.Key)
                    ?? throw new InvalidOperationException($"Field not found on payload struct: {member.Key}");
                field.SetValue(instance, BuildField(field.FieldType, member.Value, seed));
            }
            return instance;
        }

        return BuildScalar(valueType, Math.Max(1, register.Length), GetElementRange(register.Type), seed);
    }

    static object BuildField(Type fieldType, PayloadMemberInfo member, int seed)
    {
        if (fieldType.IsArray)
            return BuildArray(fieldType.GetElementType()!, Math.Max(1, member.Length), seed);
        if (fieldType.IsEnum)
            return SmallestFittingEnumValue(fieldType, GetFieldRange(member));
        return BuildScalar(fieldType, Math.Max(1, member.Length), GetFieldRange(member), seed);
    }

    static long GetElementRange(PayloadType payloadType)
    {
        var size = (int)payloadType & 0xF;
        return size >= 8 ? long.MaxValue : (1L << (size * 8)) - 1;
    }

    static long GetFieldRange(PayloadMemberInfo member)
    {
        if (!member.Mask.HasValue) return long.MaxValue;
        var mask = member.Mask.GetValueOrDefault();
        var shift = 0;
        while (((mask >> shift) & 1) == 0 && shift < 32) shift++;
        return mask >> shift;
    }

    static object BuildScalar(Type type, int length, long range, int seed)
    {
        if (type.IsArray)
            return BuildArray(type.GetElementType()!, length, seed);
        if (type == typeof(string))
            return new string('A', Math.Min(length > 1 ? length - 1 : 3, 8));
        if (type == typeof(bool))
            return seed % 2 == 1;
        if (type.IsEnum)
            return SmallestFittingEnumValue(type, range);
        if (type.FullName == "Bonsai.Harp.HarpVersion")
            return BuildHarpVersion(type, seed);
        if (type == typeof(float))
            return seed + 0.5f;
        if (type == typeof(double))
            return seed + 0.5d;
        var clamped = range >= long.MaxValue ? seed : (int)Math.Min(seed, range);
        return Convert.ChangeType(clamped, type, CultureInfo.InvariantCulture);
    }

    static object BuildArray(Type elementType, int length, int seed)
    {
        var array = Array.CreateInstance(elementType, Math.Max(1, length));
        for (int i = 0; i < array.Length; i++)
            array.SetValue(BuildScalar(elementType, 1, long.MaxValue, seed + i), i);
        return array;
    }

    static object SmallestFittingEnumValue(Type type, long fieldRange)
    {
        object? chosen = null;
        long chosenValue = long.MaxValue;
        foreach (var value in Enum.GetValues(type))
        {
            var numeric = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (numeric <= 0 || numeric > fieldRange) continue;
            if (numeric < chosenValue)
            {
                chosenValue = numeric;
                chosen = value;
            }
        }
        return chosen ?? Enum.GetValues(type).GetValue(0)!;
    }

    static object BuildHarpVersion(Type type, int seed)
    {
        var constructor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .First(c => c.GetParameters().Length >= 2);
        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameterType = Nullable.GetUnderlyingType(parameters[i].ParameterType) ?? parameters[i].ParameterType;
            if (parameterType.IsValueType && i < 2)
                arguments[i] = Convert.ChangeType(seed + i, parameterType, CultureInfo.InvariantCulture);
            else
                arguments[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
        }
        return constructor.Invoke(arguments);
    }

    public static string Canonicalize(object value, RegisterInfo register)
    {
        if (register.PayloadSpec != null)
        {
            var type = value.GetType();
            var members = register.PayloadSpec.Select(member =>
            {
                var field = type.GetField(member.Key)!;
                return $"\"{ToSnakeCase(member.Key)}\": {CanonicalizeLeaf(field.GetValue(value)!)}";
            });
            return $"{{{string.Join(", ", members)}}}";
        }

        return CanonicalizeLeaf(value);
    }

    static string CanonicalizeLeaf(object value)
    {
        var type = value.GetType();
        if (type == typeof(bool)) return (bool)value ? "1" : "0";
        if (type == typeof(string)) return $"\"{value}\"";
        if (type.IsEnum) return Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        if (type.FullName == "Bonsai.Harp.HarpVersion") return HarpVersionToJson(value);
        if (type == typeof(float) || type == typeof(double))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.000", CultureInfo.InvariantCulture);
        if (type.IsArray)
        {
            var array = (Array)value;
            var elements = new string[array.Length];
            for (int i = 0; i < array.Length; i++)
                elements[i] = CanonicalizeLeaf(array.GetValue(i)!);
            return $"[{string.Join(", ", elements)}]";
        }
        return Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    static string HarpVersionToJson(object value)
    {
        var type = value.GetType();
        var major = type.GetProperty("Major")?.GetValue(value);
        var minor = type.GetProperty("Minor")?.GetValue(value);
        return $"[{ConvertComponent(major)}, {ConvertComponent(minor)}]";
    }

    static string ConvertComponent(object? component)
    {
        if (component is null) return "0";
        return Convert.ToInt64(component, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    static string ToSnakeCase(string name)
    {
        return FirmwareNamingConvention.Instance.Apply(name).ToLowerInvariant();
    }
}
