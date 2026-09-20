#pragma warning disable IDE0005
using System;
#pragma warning restore IDE0005
using Bonsai.Harp;

namespace Harp.Generators.Tests
{
    public partial class CustomPayload
    {
        private static partial HarpVersion ParsePayload(uint[] payload)
        {
            return new HarpVersion((int)payload[0], (int)payload[1]);
        }

        private static partial uint[] FormatPayload(HarpVersion value)
        {
            return new[] { (uint)value.Major, (uint)value.Minor };
        }
    }

    public partial class CustomRawPayload
    {
        private static partial HarpVersion ParsePayload(byte[] payload, int offset, int count)
        {
            return PayloadMarshal.ReadHarpVersion(payload, offset);
        }

        private static partial byte[] FormatPayload(HarpVersion value)
        {
            var result = new byte[sizeof(uint) * RegisterLength];
            PayloadMarshal.Write(result, 0, result.Length, value);
            return result;
        }
    }

    public partial class CustomMemberConverter
    {
        private static partial int ParsePayloadData(byte[] payloadData, int offset, int count)
        {
            return PayloadMarshal.ReadInt16(payloadData, offset);
        }

        private static partial byte[] FormatPayloadData(int data)
        {
            var result = new byte[2];
            PayloadMarshal.Write(result, 0, (short)data);
            return result;
        }
    }
    
    public partial class BitmaskSplitter
    {
        private static partial int ParsePayloadLow(byte payloadLow)
        {
            return payloadLow;
        }

        private static partial byte FormatPayloadLow(int low)
        {
            return (byte)low;
        }
    }
}