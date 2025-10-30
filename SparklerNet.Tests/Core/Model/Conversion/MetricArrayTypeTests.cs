using System.Diagnostics.CodeAnalysis;
using SparklerNet.Core.Model.Conversion;
using Xunit;

namespace SparklerNet.Tests.Core.Model.Conversion;

[SuppressMessage("Performance", "CA1861")]
public class MetricArrayTypeTests
{
    [Fact]
    public void SerializeArray_Int8Array_ReturnsCorrectBytes()
    {
        sbyte[] input = [-23, 123];
        var result = MetricConverter.SerializeArray(input);

        Assert.Equal(input.Length, result.Length);
        var expected = Array.ConvertAll(input, b => (byte)b);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_UInt8Array_ReturnsCorrectBytes()
    {
        byte[] input = [23, 250];
        byte[] expected = [0x17, 0xFA];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_Int16Array_ReturnsCorrectBytes()
    {
        short[] input = [-30000, 30000];
        // ReSharper disable once UseUtf8StringLiteral
        byte[] expected = [0xD0, 0x8A, 0x30, 0x75];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_UInt16Array_ReturnsCorrectBytes()
    {
        ushort[] input = [30, 52360];
        byte[] expected = [0x1E, 0x00, 0x88, 0xCC];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_Int32Array_ReturnsCorrectBytes()
    {
        int[] input = [-1, 315338746];
        byte[] expected = [0xFF, 0xFF, 0xFF, 0xFF, 0xFA, 0xAF, 0xCB, 0x12];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_UInt32Array_ReturnsCorrectBytes()
    {
        uint[] input = [52, 3293969225];
        byte[] expected = [0x34, 0x00, 0x00, 0x00, 0x49, 0xFB, 0x55, 0xC4];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_Int64Array_ReturnsCorrectBytes()
    {
        long[] input = [-4270929666821191986, -3601064768563266876];
        byte[] expected =
            [0xCE, 0x06, 0x72, 0xAC, 0x18, 0x9C, 0xBA, 0xC4, 0xC4, 0xBA, 0x9C, 0x18, 0xAC, 0x72, 0x06, 0xCE];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_UInt64Array_ReturnsCorrectBytes()
    {
        ulong[] input = [52, 16444743074749521625];
        byte[] expected =
            [0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD9, 0x9E, 0x02, 0xD1, 0xB2, 0x76, 0x37, 0xE4];

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_FloatArray_ReturnsCorrectBytes()
    {
        float[] input = [1.23f, 89.341f];
        var result = MetricConverter.SerializeArray(input);

        Assert.Equal(input.Length * 4, result.Length);
        var deserialized = new float[input.Length];
        Buffer.BlockCopy(result, 0, deserialized, 0, result.Length);

        for (var i = 0; i < input.Length; i++)
            Assert.Equal(input[i], deserialized[i], 5);
    }

    [Fact]
    public void SerializeArray_DoubleArray_ReturnsCorrectBytes()
    {
        double[] input = [12.354213, 1022.9123213];
        var result = MetricConverter.SerializeArray(input);

        Assert.Equal(input.Length * 8, result.Length);
        var deserialized = new double[input.Length];
        Buffer.BlockCopy(result, 0, deserialized, 0, result.Length);

        for (var i = 0; i < input.Length; i++)
            Assert.Equal(input[i], deserialized[i], 9);
    }

    [Fact]
    public void SerializeArray_BooleanArray_ReturnsCorrectBytes()
    {
        bool[] input = [false, false, true, true, false, true, false, false, true, true, false, true];
        var result = MetricConverter.SerializeArray(input);

        var countBytes = BitConverter.GetBytes(input.Length);
        for (var i = 0; i < 4; i++)
            Assert.Equal(countBytes[i], result[i]);

        var expectedByteCount = 4 + (input.Length + 7) / 8;
        Assert.Equal(expectedByteCount, result.Length);
    }

    [Fact]
    public void SerializeArray_StringArray_ReturnsCorrectBytes()
    {
        string[] input = ["ABC", "hello"];
        var expected = "ABC\0hello\0"u8.ToArray();

        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_DateTimeArray_ReturnsCorrectBytes()
    {
        long[] input = [1256102875335, 1656107875000];
        var result = MetricConverter.SerializeArray(input);

        Assert.Equal(input.Length * 8, result.Length);
        for (var i = 0; i < input.Length; i++)
        {
            var expectedValueBytes = BitConverter.GetBytes(input[i]);
            for (var j = 0; j < 8; j++)
                Assert.Equal(expectedValueBytes[j], result[i * 8 + j]);
        }
    }

    [Fact]
    public void SerializeArray_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MetricConverter.SerializeArray<int>(null!));
    }

    [Fact]
    public void SerializeArray_UnsupportedType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => MetricConverter.SerializeArray(['a', 'b', 'c']));
    }

    [Fact]
    public void SerializeArray_StringArrayWithNullValues_HandlesNullsCorrectly()
    {
        string?[] input = ["test", null, "null"];
        var result = MetricConverter.SerializeArray(input);

        byte[] expected;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write("test"u8.ToArray());
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write("null"u8.ToArray());
            writer.Write((byte)0);
            expected = stream.ToArray();
        }

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeArray_EmptyArray_ReturnsEmptyBytes()
    {
        Assert.Empty(MetricConverter.SerializeArray<int>([]));
        Assert.Empty(MetricConverter.SerializeArray<string>([]));

        var boolResult = MetricConverter.SerializeArray<bool>([]);
        Assert.Equal(4, boolResult.Length);
        // ReSharper disable once UseUtf8StringLiteral
        Assert.Equal([0, 0, 0, 0], boolResult);
    }

    [Fact]
    public void DeserializeArray_Int8Array_ReturnsCorrectValues()
    {
        // Use SerializeArray to generate accurate byte representation
        sbyte[] original = [-23, 123];
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<sbyte>(input);

        Assert.Equal(2, result.Length);
        Assert.Equal(original, result);
    }

    [Fact]
    public void DeserializeArray_UInt8Array_ReturnsCorrectValues()
    {
        byte[] input = [0x17, 0xFA]; // [23, 250] in hex
        var result = MetricConverter.DeserializeArray<byte>(input);

        Assert.Equal(new byte[] { 23, 250 }, result);
    }

    [Fact]
    public void DeserializeArray_Int16Array_ReturnsCorrectValues()
    {
        // ReSharper disable once UseUtf8StringLiteral
        byte[] input = [0xD0, 0x8A, 0x30, 0x75]; // [-30000, 30000] in hex
        var result = MetricConverter.DeserializeArray<short>(input);

        Assert.Equal(new short[] { -30000, 30000 }, result);
    }

    [Fact]
    public void DeserializeArray_UInt16Array_ReturnsCorrectValues()
    {
        byte[] input = [0x1E, 0x00, 0x88, 0xCC]; // [30, 52360] in hex
        var result = MetricConverter.DeserializeArray<ushort>(input);

        Assert.Equal(new ushort[] { 30, 52360 }, result);
    }

    [Fact]
    public void DeserializeArray_Int32Array_ReturnsCorrectValues()
    {
        byte[] input = [0xFF, 0xFF, 0xFF, 0xFF, 0xFA, 0xAF, 0xCB, 0x12]; // [-1, 315338746] in hex
        var result = MetricConverter.DeserializeArray<int>(input);

        Assert.Equal(new[] { -1, 315338746 }, result);
    }

    [Fact]
    public void DeserializeArray_UInt32Array_ReturnsCorrectValues()
    {
        byte[] input = [0x34, 0x00, 0x00, 0x00, 0x49, 0xFB, 0x55, 0xC4]; // [52, 3293969225] in hex
        var result = MetricConverter.DeserializeArray<uint>(input);

        Assert.Equal(new[] { 52U, 3293969225U }, result);
    }

    [Fact]
    public void DeserializeArray_Int64Array_ReturnsCorrectValues()
    {
        byte[] input = [0xCE, 0x06, 0x72, 0xAC, 0x18, 0x9C, 0xBA, 0xC4, 0xC4, 0xBA, 0x9C, 0x18, 0xAC, 0x72, 0x06, 0xCE];
        var result = MetricConverter.DeserializeArray<long>(input);

        Assert.Equal(new[] { -4270929666821191986L, -3601064768563266876L }, result);
    }

    [Fact]
    public void DeserializeArray_UInt64Array_ReturnsCorrectValues()
    {
        byte[] input = [0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD9, 0x9E, 0x02, 0xD1, 0xB2, 0x76, 0x37, 0xE4];
        var result = MetricConverter.DeserializeArray<ulong>(input);

        Assert.Equal(new[] { 52UL, 16444743074749521625UL }, result);
    }

    [Fact]
    public void DeserializeArray_FloatArray_ReturnsCorrectValues()
    {
        // Use more accurate byte representation by converting directly from float values
        float[] original = [1.23f, 89.341f];
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<float>(input);

        Assert.Equal(2, result.Length);
        Assert.Equal(original[0], result[0], 5);
        Assert.Equal(original[1], result[1], 5);
    }

    [Fact]
    public void DeserializeArray_DoubleArray_ReturnsCorrectValues()
    {
        // Use more accurate byte representation by converting directly from double values
        double[] original = [12.354213, 1022.9123213];
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<double>(input);

        Assert.Equal(2, result.Length);
        Assert.Equal(original[0], result[0], 9);
        Assert.Equal(original[1], result[1], 9);
    }

    [Fact]
    public void DeserializeArray_BooleanArray_ReturnsCorrectValues()
    {
        // Use SerializeArray to generate accurate byte representation and avoid bit pattern calculation errors
        bool[] original = [false, false, true, true, false, true, false, false, true, true, false, true];
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<bool>(input);

        Assert.Equal(12, result.Length);
        Assert.Equal(original, result);
    }

    [Fact]
    public void DeserializeArray_StringArray_ReturnsCorrectValues()
    {
        // ReSharper disable once UseUtf8StringLiteral
        byte[] input =
            [0x41, 0x42, 0x43, 0x00, 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x00]; // [ABC, hello] with null terminators
        var result = MetricConverter.DeserializeArray<string>(input);

        Assert.Equal(new[] { "ABC", "hello" }, result);
    }

    [Fact]
    public void DeserializeArray_DateTimeArray_ReturnsCorrectValues()
    {
        // Use SerializeArray to generate accurate byte representation
        long[] original = [1256102875335L, 1656107875000L];
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<long>(input);

        Assert.Equal(2, result.Length);
        Assert.Equal(original, result);
    }

    [Fact]
    public void DeserializeArray_NullBytes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MetricConverter.DeserializeArray<int>(null!));
    }

    [Fact]
    public void DeserializeArray_UnsupportedType_ThrowsNotSupportedException()
    {
        // ReSharper disable once UseUtf8StringLiteral
        Assert.Throws<NotSupportedException>(() => MetricConverter.DeserializeArray<char>([0x61, 0x62]));
    }

    [Fact]
    public void DeserializeArray_StringArrayWithNullValues_HandlesNullsCorrectly()
    {
        // Use SerializeArray to generate accurate byte representation
        string?[] original = ["test", null, "null"];
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<string>(input);

        Assert.Equal(original, result);
    }

    [Fact]
    public void DeserializeArray_EmptyBytes_ReturnsEmptyArray()
    {
        Assert.Empty(MetricConverter.DeserializeArray<int>([]));
        Assert.Empty(MetricConverter.DeserializeArray<string>([]));
    }

    [Fact]
    public void DeserializeArray_BooleanArrayWithZeroCount_ReturnsEmptyArray()
    {
        // ReSharper disable once UseUtf8StringLiteral
        byte[] input = [0x00, 0x00, 0x00, 0x00]; // Count = 0
        var result = MetricConverter.DeserializeArray<bool>(input);

        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeArray_SerializationDeserializationRoundTrip_WorksCorrectly()
    {
        // Test round trip for various array types
        var originalSByte = new sbyte[] { -128, 127, 0 };
        var roundTripSByte = MetricConverter.DeserializeArray<sbyte>(MetricConverter.SerializeArray(originalSByte));
        Assert.Equal(originalSByte, roundTripSByte);

        var originalDouble = new[] { 1.1, 2.2, 3.3 };
        var roundTripDouble = MetricConverter.DeserializeArray<double>(MetricConverter.SerializeArray(originalDouble));
        Assert.Equal(originalDouble, roundTripDouble);

        var originalBool = new[] { true, false, true, true, false };
        var roundTripBool = MetricConverter.DeserializeArray<bool>(MetricConverter.SerializeArray(originalBool));
        Assert.Equal(originalBool, roundTripBool);

        var originalString = new[] { "test", null, "hello" };
        var roundTripString = MetricConverter.DeserializeArray<string>(MetricConverter.SerializeArray(originalString));
        Assert.Equal(originalString, roundTripString);
    }
}