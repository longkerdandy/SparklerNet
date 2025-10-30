# Sparkplug Array Types
 All array types use the bytes_value field of the Metric value field. They are simply little-endian packed byte arrays.
 For example, consider an Int32 array with two decimal values [123456789, 987654321]
 Array converted to little endian hex: [0x15CD5B07, 0xB168DE3A]
 The bytes_value of the Sparkplug Metric must be: [0x15, 0xCD, 0x5B, 0x07, 0xB1, 0x68, 0xDE, 0x3A]

## Int8Array
- Int8Array as an array of packed little endian int8 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 22
- Example (Decimal to Metric bytes_value): [-23, 123] → [0xEF, 0x7B]

## Int16Array
- Int16Array as an array of packed little endian int16 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 23
- Example (Decimal to Metric bytes_value): [-30000, 30000] → [0xD0, 0x8A, 0x30, 0x75]

## Int32Array
- Int32Array as an array of packed little endian int32 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 24
- Example (Decimal to Metric bytes_value): [-1, 315338746] → [0xFF, 0xFF, 0xFF, 0xFF, 0xFA, 0xAF, 0xCB, 0x12]

## Int64Array
- Int64Array as an array of packed little endian int64 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 25
- Example (Decimal to Metric bytes_value): [-4270929666821191986, -3601064768563266876] → [0xCE, 0x06, 0x72, 0xAC, 0x18, 0x9C, 0xBA, 0xC4, 0xC4, 0xBA, 0x9C, 0x18, 0xAC, 0x72, 0x06, 0xCE]

## UInt8Array
- UInt8Array as an array of packed little endian uint8 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 26
- Example (Decimal to Metric bytes_value): [23, 250] → [0x17, 0xFA]

## UInt16Array
- UInt16Array as an array of packed little endian uint16 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 27
- Example (Decimal to Metric bytes_value): [30, 52360] → [0x1E, 0x00, 0x88, 0xCC]

## UInt32Array
- UInt32Array as an array of packed little endian uint32 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 28
- Example (Decimal to Metric bytes_value): [52, 3293969225] → [0x34, 0x00, 0x00, 0x00, 0x49, 0xFB, 0x55, 0xC4]

## UInt64Array
- UInt64Array as an array of packed little endian uint64 bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 29
- Example (Decimal to Metric bytes_value): [52, 16444743074749521625] → [0x34, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xD9, 0x9E, 0x02, 0xD1, 0xB2, 0x76, 0x37, 0xE4]

## FloatArray
- FloatArray as an array of packed little endian 32-bit float bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 30
- Example (Decimal to Metric bytes_value): [1.23, 89.341] → [0x3F, 0x9D, 0x70, 0xA4, 0x42, 0xB2, 0xAE, 0x98]

## DoubleArray
- DoubleArray as an array of packed little endian 64-bit float bytes
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 31
- Example (Decimal to Metric bytes_value): [12.354213, 1022.9123213] → [0x40, 0x28, 0xB5, 0x5B, 0x68, 0x05, 0xA2, 0xD7, 0x40, 0x8F, 0xF7, 0x4C, 0x6F, 0x1C, 0x17, 0x8E]

## BooleanArray
- BooleanArray as an array of bit-packed bytes preceded by a 4-byte integer that represents the total number of boolean values
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 32
- Example (boolean array to Metric bytes_value): [false, false, true, true, false, true, false, false, true, true, false, true] → [0x0C, 0x00, 0x00, 0x00, 0x34, 0xD0], Note an X above is a do not care. It can be either 1 or 0 but must be present so the array ends on a byte boundary.

## StringArray
- StringArray as an array of null terminated strings
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 33
- Example (string array to Metric bytes_value): [ABC, hello] → [0x41, 0x42, 0x43, 0x00, 0x68, 0x65, 0x6c, 0x6c, 0x6f, 0x00]

## DateTimeArray
- DateTimeArray as an array of packed little endian bytes where each DateTime value is an 8-byte value representing the number of milliseconds since epoch in UTC
- Google Protocol Buffer Type: bytes
- Sparkplug enum value: 34
- Example (DateTime array → ms since epoch → Metric bytes_value): [Wednesday, October 21, 2009 5:27:55.335 AM, Friday, June 24, 2022 9:57:55 PM] → [1256102875335, 1656107875000] → [0xC7, 0xD0, 0x90, 0x75, 0x24, 0x01, 0xB8, 0xBA, 0xB8, 0x97, 0x81, 0x01]