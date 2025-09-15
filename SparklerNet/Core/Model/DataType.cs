namespace SparklerNet.Core.Model;

/// <summary>
///     The Sparkplug defined data types used for various Sparkplug constructs including Metric datatypes Property Value
///     types, DataSet types, and Template Parameter types.
///     Corresponding to the data type definitions in the Sparkplug's Protobuf schema file.
/// </summary>
public enum DataType
{
    // Unknown placeholder for future expansion.
    Unknown = 0,

    // Basic Types
    Int8 = 1,
    Int16 = 2,
    Int32 = 3,
    Int64 = 4,
    UInt8 = 5,
    UInt16 = 6,
    UInt32 = 7,
    UInt64 = 8,
    Float = 9,
    Double = 10,
    Boolean = 11,
    String = 12,
    DateTime = 13,
    Text = 14,

    // Additional Metric Types
    UUID = 15,
    DataSet = 16,
    Bytes = 17,
    File = 18,
    Template = 19,

    // Additional PropertyValue Types
    PropertySet = 20,
    PropertySetList = 21,

    // Array Types
    Int8Array = 22,
    Int16Array = 23,
    Int32Array = 24,
    Int64Array = 25,
    UInt8Array = 26,
    UInt16Array = 27,
    UInt32Array = 28,
    UInt64Array = 29,
    FloatArray = 30,
    DoubleArray = 31,
    BooleanArray = 32,
    StringArray = 33,
    DateTimeArray = 34
}