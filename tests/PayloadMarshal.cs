#pragma warning disable IDE0005
using System;
#pragma warning restore IDE0005
using Bonsai.Harp;

namespace Harp.Generators.Tests;

internal static partial class PayloadMarshal
{
    internal static HarpVersion ReadHarpVersion(ArraySegment<byte> segment)
    {
        var major = segment.Array[segment.Offset];
        var minor = segment.Array[segment.Offset + 1];
        return new HarpVersion(major, minor);
    }

    internal static void Write(ArraySegment<byte> segment, HarpVersion value)
    {
        segment.Array[segment.Offset] = (byte)value.Major.GetValueOrDefault();
        segment.Array[segment.Offset + 1] = (byte)value.Minor.GetValueOrDefault();
    }
}
