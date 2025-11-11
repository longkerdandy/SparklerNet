using System.Collections;
using System.Reflection;
using SparklerNet.Core.Model.Conversion;
using Xunit;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class MetricArrayTypeTests
{
    [Fact]
    public void SerializeArray_Int8Array_ReturnsCorrectBytes()
    {
        sbyte[] input = [-23, 123];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length, result.Length);
    }

    [Fact]
    public void SerializeArray_UInt8Array_ReturnsCorrectBytes()
    {
        byte[] input = [23, 250];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length, result.Length);
    }

    [Fact]
    public void SerializeArray_Int16Array_ReturnsCorrectBytes()
    {
        short[] input = [-30000, 30000];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length * 2, result.Length);
    }

    [Fact]
    public void SerializeArray_UInt16Array_ReturnsCorrectBytes()
    {
        ushort[] input = [30, 52360];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length * 2, result.Length);
    }

    [Fact]
    public void SerializeArray_Int32Array_ReturnsCorrectBytes()
    {
        int[] input = [-1, 315338746];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length * 4, result.Length);
    }

    [Fact]
    public void SerializeArray_UInt32Array_ReturnsCorrectBytes()
    {
        uint[] input = [52, 3293969225];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length * 4, result.Length);
    }

    [Fact]
    public void SerializeArray_Int64Array_ReturnsCorrectBytes()
    {
        long[] input = [-4270929666821191986, -3601064768563266876];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length * 8, result.Length);
    }

    [Fact]
    public void SerializeArray_UInt64Array_ReturnsCorrectBytes()
    {
        ulong[] input = [52, 16444743074749521625];
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(input.Length * 8, result.Length);
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

    [Theory]
    [ClassData(typeof(BooleanArrayTestData))]
    public void SerializeArray_BooleanArray_ReturnsCorrectBytes(bool[] input)
    {
        var result = MetricConverter.SerializeArray(input);

        var countBytes = BitConverter.GetBytes(input.Length);
        for (var i = 0; i < 4; i++)
            Assert.Equal(countBytes[i], result[i]);

        var expectedByteCount = 4 + (input.Length + 7) / 8;
        Assert.Equal(expectedByteCount, result.Length);
    }

    [Theory]
    [ClassData(typeof(StringArrayTestData))]
    public void SerializeArray_StringArray_ReturnsCorrectBytes(string[] input)
    {
        var expected = "ABC\0hello\0"u8.ToArray();
        var result = MetricConverter.SerializeArray(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [ClassData(typeof(LongArrayTestData))]
    public void SerializeArray_LongArray_ReturnsCorrectBytes(long[] input)
    {
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

    [Theory]
    [ClassData(typeof(StringArrayWithNullTestData))]
    public void SerializeArray_StringArrayWithNullValues_HandlesNullsCorrectly(string?[] input)
    {
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
        var emptyArray = Array.Empty<byte>();
        var result = MetricConverter.SerializeArray(emptyArray);
        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeArray_Int8Array_ReturnsCorrectValues()
    {
        sbyte[] input = [-23, 123];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<sbyte>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_UInt8Array_ReturnsCorrectValues()
    {
        byte[] input = [23, 250];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<byte>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_Int16Array_ReturnsCorrectValues()
    {
        short[] input = [-30000, 30000];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<short>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_UInt16Array_ReturnsCorrectValues()
    {
        ushort[] input = [30, 52360];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<ushort>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_Int32Array_ReturnsCorrectValues()
    {
        int[] input = [-1, 315338746];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<int>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_UInt32Array_ReturnsCorrectValues()
    {
        uint[] input = [52, 3293969225];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<uint>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_Int64Array_ReturnsCorrectValues()
    {
        long[] input = [-4270929666821191986, -3601064768563266876];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<long>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_UInt64Array_ReturnsCorrectValues()
    {
        ulong[] input = [52, 16444743074749521625];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<ulong>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_FloatArray_ReturnsCorrectValues()
    {
        float[] input = [1.23f, 89.341f];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<float>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i], 5);
    }

    [Fact]
    public void DeserializeArray_DoubleArray_ReturnsCorrectValues()
    {
        double[] input = [12.354213, 1022.9123213];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<double>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i], 9);
    }

    [Fact]
    public void DeserializeArray_BooleanArray_ReturnsCorrectValues()
    {
        bool[] input = [false, false, true, true, false, true, false, false, true, true, false, true];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<bool>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_StringArray_ReturnsCorrectValues()
    {
        string[] input = ["ABC", "hello"];
        var bytes = MetricConverter.SerializeArray(input);
        var result = MetricConverter.DeserializeArray<string>(bytes);

        Assert.Equal(input.Length, result.Length);
        for (var i = 0; i < input.Length; i++) Assert.Equal(input[i], result[i]);
    }

    [Fact]
    public void DeserializeArray_NullBytes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MetricConverter.DeserializeArray<int>(null!));
    }

    [Fact]
    public void DeserializeArray_UnsupportedType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => MetricConverter.DeserializeArray<char>("ab"u8.ToArray()));
    }

    [Theory]
    [ClassData(typeof(StringArrayWithNullTestData))]
    public void DeserializeArray_StringArrayWithNullValues_HandlesNullsCorrectly(string?[] original)
    {
        var input = MetricConverter.SerializeArray(original);
        var result = MetricConverter.DeserializeArray<string>(input);

        Assert.Equal(original, result);
    }

    [Theory]
    [InlineData(typeof(int[]))]
    [InlineData(typeof(string[]))]
    public void DeserializeArray_EmptyBytes_ReturnsEmptyArray(Type arrayType)
    {
        var deserializeMethod =
            typeof(MetricConverter).GetMethod("DeserializeArray", BindingFlags.Public | BindingFlags.Static);
        var genericMethod = deserializeMethod!.MakeGenericMethod(arrayType.GetElementType()!);
        var result = (Array)genericMethod.Invoke(null, [Array.Empty<byte>()])!;

        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeArray_BooleanArrayWithZeroCount_ReturnsEmptyArray()
    {
        // ReSharper disable once UseUtf8StringLiteral
        byte[] input = [0, 0, 0, 0]; // Count = 0
        var result = MetricConverter.DeserializeArray<bool>(input);

        Assert.Empty(result);
    }

    private class BooleanArrayTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new[] { false, false, true, true, false, true, false, false, true, true, false, true }];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class StringArrayTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new[] { "ABC", "hello" }];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class StringArrayWithNullTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new[] { "test", null, "null" }];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class LongArrayTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new[] { 1256102875335, 1656107875000 }];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}