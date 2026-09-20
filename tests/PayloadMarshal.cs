#pragma warning disable IDE0005
using System;
#pragma warning restore IDE0005
using Bonsai.Harp;

namespace Harp.Generators.Tests;

internal static partial class PayloadMarshal
{
    internal static HarpVersion ReadHarpVersion(byte[] array, int offset)
    {
        return new HarpVersion(array[offset], array[offset + 1]);
    }

    internal static void Write(byte[] array, int offset, int count, HarpVersion value)
    {
        array[offset] = (byte)value.Major;
        array[offset + 1] = (byte)value.Minor;
    }
}
