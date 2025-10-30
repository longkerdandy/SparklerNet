using System.Text;
using static Google.Protobuf.ByteString;
using ProtoMetric = SparklerNet.Core.Protobuf.Payload.Types.Metric;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="Metric" /> and <see cref="ProtoMetric" />.
/// </summary>
public static class MetricConverter
{
    /// <summary>
    ///     Converts a <see cref="Metric" /> to a Protobuf <see cref="ProtoMetric" />.
    /// </summary>
    /// <param name="metric">The Metric to convert.</param>
    /// <returns>The converted Protobuf Metric.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metric" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the metric data type is not supported.</exception>
    public static ProtoMetric ToProtoMetric(this Metric metric)
    {
        ArgumentNullException.ThrowIfNull(metric);

        var protoMetric = new ProtoMetric
        {
            // Set basic properties
            IsHistorical = metric.IsHistorical ?? false,
            IsTransient = metric.IsTransient ?? false,
            IsNull = metric.IsNull
        };

        // Set optional properties if they have values
        if (metric.Name != null) protoMetric.Name = metric.Name;
        if (metric.Alias.HasValue) protoMetric.Alias = metric.Alias.Value;
        if (metric.Timestamp.HasValue) protoMetric.Timestamp = (ulong)metric.Timestamp.Value;
        if (metric.DateType.HasValue) protoMetric.Datatype = (uint)metric.DateType.Value;
        if (metric.Metadata != null) protoMetric.Metadata = metric.Metadata.ToProtoMetaData();
        if (metric.Properties != null) protoMetric.Properties = metric.Properties.ToProtoPropertySet();

        // Only set the value if it's not null and DateType is specified
        if (metric is not { DateType: not null, IsNull: false }) return protoMetric;

        // Set the value based on the data type
        Action valueAssignment = metric.DateType switch
        {
            DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.UInt8 or DataType.UInt16 or DataType.UInt32 =>
                () => protoMetric.IntValue = Convert.ToUInt32(metric.Value),
            DataType.Int64 or DataType.UInt64 => () => protoMetric.LongValue = Convert.ToUInt64(metric.Value),
            DataType.Float => () => protoMetric.FloatValue = Convert.ToSingle(metric.Value),
            DataType.Double => () => protoMetric.DoubleValue = Convert.ToDouble(metric.Value),
            DataType.Boolean => () => protoMetric.BooleanValue = Convert.ToBoolean(metric.Value),
            DataType.DateTime => () => protoMetric.LongValue = metric.Value is long value
                ? Convert.ToUInt64(value)
                : throw new NotSupportedException("Value for DateTime type must be long"),
            DataType.String or DataType.Text or DataType.UUID =>
                () => protoMetric.StringValue = metric.Value!.ToString()!,
            DataType.Bytes or DataType.File => () => protoMetric.BytesValue = metric.Value is byte[] bytes
                ? CopyFrom(bytes)
                : throw new NotSupportedException("Value for Bytes/File type must be byte[]"),
            DataType.DataSet => () => protoMetric.DatasetValue = metric.Value is DataSet dataSet
                ? dataSet.ToProtoDataSet()
                : throw new NotSupportedException("Value for DataSet type must be dataset"),
            DataType.Template => () => protoMetric.TemplateValue = metric.Value is Template template
                ? template.ToProtoTemplate()
                : throw new NotSupportedException("Value for Template type must be template"),
            DataType.Int8Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((sbyte[])metric.Value!)),
            DataType.UInt8Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((byte[])metric.Value!)),
            DataType.Int16Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((short[])metric.Value!)),
            DataType.UInt16Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((ushort[])metric.Value!)),
            DataType.Int32Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((int[])metric.Value!)),
            DataType.UInt32Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((uint[])metric.Value!)),
            DataType.Int64Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((long[])metric.Value!)),
            DataType.UInt64Array => () => protoMetric.BytesValue = CopyFrom(SerializeArray((ulong[])metric.Value!)),
            DataType.FloatArray => () => protoMetric.BytesValue = CopyFrom(SerializeArray((float[])metric.Value!)),
            DataType.DoubleArray => () => protoMetric.BytesValue = CopyFrom(SerializeArray((double[])metric.Value!)),
            DataType.BooleanArray => () => protoMetric.BytesValue = CopyFrom(SerializeArray((bool[])metric.Value!)),
            DataType.DateTimeArray => () => protoMetric.BytesValue = CopyFrom(SerializeArray((long[])metric.Value!)),
            DataType.StringArray => () => protoMetric.BytesValue = CopyFrom(SerializeArray((string[])metric.Value!)),
            _ => throw new NotSupportedException($"Data type {metric.DateType} is not supported in Metric conversion")
        };
        // Execute the conversion action
        valueAssignment();

        return protoMetric;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoMetric" /> to a <see cref="Metric" />.
    /// </summary>
    /// <param name="protoMetric">The Protobuf Metric to convert.</param>
    /// <returns>The converted Metric.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoMetric" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the metric data type is not supported.</exception>
    public static Metric ToMetric(this ProtoMetric protoMetric)
    {
        ArgumentNullException.ThrowIfNull(protoMetric);

        var dataType = protoMetric.Datatype != 0 ? (DataType?)protoMetric.Datatype : null;

        // Create a new Metric with basic properties
        var metric = new Metric
        {
            Name = protoMetric.Name, // Will be null if not set
            Alias = protoMetric.Alias != 0 ? protoMetric.Alias : null,
            Timestamp = protoMetric.Timestamp != 0 ? (long?)protoMetric.Timestamp : null,
            DateType = dataType,
            IsHistorical = protoMetric.IsHistorical,
            IsTransient = protoMetric.IsTransient,
            Metadata = protoMetric.Metadata?.ToMetaData(),
            Properties = protoMetric.Properties?.ToPropertySet()
        };

        // Convert the value based on the data type if it's not null
        if (dataType != null && !protoMetric.IsNull)
            metric.Value = dataType switch
            {
                DataType.Int8 => (sbyte)protoMetric.IntValue,
                DataType.Int16 => (short)protoMetric.IntValue,
                DataType.Int32 => (int)protoMetric.IntValue,
                DataType.UInt8 => (byte)protoMetric.IntValue,
                DataType.UInt16 => (ushort)protoMetric.IntValue,
                DataType.UInt32 => protoMetric.IntValue,
                DataType.Int64 => (long)protoMetric.LongValue,
                DataType.UInt64 => protoMetric.LongValue,
                DataType.Float => protoMetric.FloatValue,
                DataType.Double => protoMetric.DoubleValue,
                DataType.Boolean => protoMetric.BooleanValue,
                DataType.DateTime => (long)protoMetric.LongValue,
                DataType.String or DataType.Text or DataType.UUID => protoMetric.StringValue,
                DataType.Bytes or DataType.File => protoMetric.BytesValue!.ToByteArray(),
                DataType.DataSet => protoMetric.DatasetValue?.ToDataSet(),
                DataType.Template => protoMetric.TemplateValue?.ToTemplate(),
                DataType.Int8Array => DeserializeArray<sbyte>(protoMetric.BytesValue!.ToByteArray()),
                DataType.UInt8Array => DeserializeArray<byte>(protoMetric.BytesValue!.ToByteArray()),
                DataType.Int16Array => DeserializeArray<short>(protoMetric.BytesValue!.ToByteArray()),
                DataType.UInt16Array => DeserializeArray<ushort>(protoMetric.BytesValue!.ToByteArray()),
                DataType.Int32Array => DeserializeArray<int>(protoMetric.BytesValue!.ToByteArray()),
                DataType.UInt32Array => DeserializeArray<uint>(protoMetric.BytesValue!.ToByteArray()),
                DataType.Int64Array => DeserializeArray<long>(protoMetric.BytesValue!.ToByteArray()),
                DataType.UInt64Array => DeserializeArray<ulong>(protoMetric.BytesValue!.ToByteArray()),
                DataType.FloatArray => DeserializeArray<float>(protoMetric.BytesValue!.ToByteArray()),
                DataType.DoubleArray => DeserializeArray<double>(protoMetric.BytesValue!.ToByteArray()),
                DataType.BooleanArray => DeserializeArray<bool>(protoMetric.BytesValue!.ToByteArray()),
                DataType.DateTimeArray => DeserializeArray<long>(protoMetric.BytesValue!.ToByteArray()),
                DataType.StringArray => DeserializeArray<string>(protoMetric.BytesValue!.ToByteArray()),
                _ => throw new NotSupportedException($"Data type {dataType} is not supported in Metric conversion")
            };

        return metric;
    }

    /// <summary>
    ///     Serializes an array into a byte array according to Sparkplug protocol specifications.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array to serialize.</param>
    /// <returns>A byte array representing the serialized array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array" /> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when the array element type is not supported for serialization.</exception>
    public static byte[] SerializeArray<T>(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);

        var elementType = typeof(T);

        // Handle different array types based on element types
        if (elementType == typeof(sbyte)) // Int8Array
            return Array.ConvertAll((sbyte[])Convert.ChangeType(array, typeof(sbyte[])), b => (byte)b);

        if (elementType == typeof(byte)) // UInt8Array
            return (byte[])Convert.ChangeType(array, typeof(byte[]));

        if (elementType == typeof(short)) // Int16Array
        {
            var result = new byte[array.Length * 2];
            Buffer.BlockCopy((short[])Convert.ChangeType(array, typeof(short[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(ushort)) // UInt16Array
        {
            var result = new byte[array.Length * 2];
            Buffer.BlockCopy((ushort[])Convert.ChangeType(array, typeof(ushort[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(int)) // Int32Array
        {
            var result = new byte[array.Length * 4];
            Buffer.BlockCopy((int[])Convert.ChangeType(array, typeof(int[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(uint)) // UInt32Array
        {
            var result = new byte[array.Length * 4];
            Buffer.BlockCopy((uint[])Convert.ChangeType(array, typeof(uint[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(long)) // Int64Array / DateTimeArray
        {
            var result = new byte[array.Length * 8];
            Buffer.BlockCopy((long[])Convert.ChangeType(array, typeof(long[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(ulong)) // UInt64Array
        {
            var result = new byte[array.Length * 8];
            Buffer.BlockCopy((ulong[])Convert.ChangeType(array, typeof(ulong[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(float)) // FloatArray
        {
            var result = new byte[array.Length * 4];
            Buffer.BlockCopy((float[])Convert.ChangeType(array, typeof(float[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(double)) // DoubleArray
        {
            var result = new byte[array.Length * 8];
            Buffer.BlockCopy((double[])Convert.ChangeType(array, typeof(double[])), 0, result, 0, result.Length);
            return result;
        }

        if (elementType == typeof(bool)) // BooleanArray
        {
            // First 4 bytes: total number of boolean values (little-endian)
            var countBytes = BitConverter.GetBytes(array.Length);
            if (!BitConverter.IsLittleEndian) Array.Reverse(countBytes);

            // Calculate the required byte count for bit-packing (rounded up to full byte)
            var byteCount = (array.Length + 7) / 8;
            var boolBytes = new byte[byteCount];

            // Pack booleans into bytes (1 bit per boolean, first boolean in the least significant bit)
            var boolArray = (bool[])Convert.ChangeType(array, typeof(bool[]));
            for (var i = 0; i < boolArray.Length; i++)
                if (boolArray[i])
                    boolBytes[i / 8] |= (byte)(1 << (i % 8));

            // Combine count and boolean data
            var result = new byte[4 + byteCount];
            Buffer.BlockCopy(countBytes, 0, result, 0, 4);
            Buffer.BlockCopy(boolBytes, 0, result, 4, byteCount);
            return result;
        }

        // ReSharper disable once InvertIf
        if (elementType == typeof(string)) // StringArray
        {
            // For each string, add the bytes of the string (UTF-8) followed by a null byte (0x00)
            var stringArray = (string[])Convert.ChangeType(array, typeof(string[]));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            foreach (var str in stringArray)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (str != null)
                {
                    var bytes = Encoding.UTF8.GetBytes(str);
                    writer.Write(bytes);
                }

                writer.Write((byte)0); // null terminator
            }

            return stream.ToArray();
        }

        throw new NotSupportedException($"Array element type {elementType.Name} is not supported in Metric conversion");
    }

    /// <summary>
    ///     Deserializes a byte array into an array of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="bytes">The byte array to deserialize.</param>
    /// <returns>An array of the specified type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the element type is not supported for deserialization.</exception>
    public static T[] DeserializeArray<T>(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var elementType = typeof(T);

        // Handle empty array case
        if (bytes.Length == 0)
            return [];

        // Handle different array types based on element types
        if (elementType == typeof(sbyte)) // Int8Array
            return (T[])(object)Array.ConvertAll(bytes, b => (sbyte)b);

        if (elementType == typeof(byte)) // UInt8Array
            return (T[])bytes.Clone();

        if (elementType == typeof(short)) // Int16Array
        {
            var result = new short[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(ushort)) // UInt16Array
        {
            var result = new ushort[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(int)) // Int32Array
        {
            var result = new int[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(uint)) // UInt32Array
        {
            var result = new uint[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(long)) // Int64Array / DateTimeArray
        {
            var result = new long[bytes.Length / 8];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(ulong)) // UInt64Array
        {
            var result = new ulong[bytes.Length / 8];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(float)) // FloatArray
        {
            var result = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(double)) // DoubleArray
        {
            var result = new double[bytes.Length / 8];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return (T[])(object)result;
        }

        if (elementType == typeof(bool)) // BooleanArray
        {
            // First 4 bytes: total number of boolean values
            var countBytes = new byte[4];
            Buffer.BlockCopy(bytes, 0, countBytes, 0, 4);
            if (!BitConverter.IsLittleEndian) Array.Reverse(countBytes);

            var count = BitConverter.ToInt32(countBytes, 0);
            var result = new bool[count];

            // If there are no booleans, return an empty array
            if (count == 0)
                return (T[])(object)result;

            // Extract boolean values from bit-packed bytes
            for (var i = 0; i < count; i++)
            {
                var byteIndex = i / 8 + 4; // +4 for the count bytes
                var bitOffset = i % 8;
                result[i] = (bytes[byteIndex] & (1 << bitOffset)) != 0;
            }

            return (T[])(object)result;
        }

        // ReSharper disable once InvertIf
        if (elementType == typeof(string)) // StringArray
        {
            var result = new List<string?>();
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            while (stream.Position < stream.Length)
            {
                var strBytes = new List<byte>();
                byte b;

                // Read until null terminator
                while ((b = reader.ReadByte()) != 0)
                    strBytes.Add(b);

                // Convert to string (null if no bytes read before null terminator to match SerializeArray behavior)
                result.Add(strBytes.Count > 0 ? Encoding.UTF8.GetString([.. strBytes]) : null);
            }

            return (T[])(object)result.ToArray();
        }

        throw new NotSupportedException($"Array element type {elementType.Name} is not supported in Metric conversion");
    }
}