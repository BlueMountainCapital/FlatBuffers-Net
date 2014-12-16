using System.ComponentModel;
using System.Text;

namespace FlatBuffers {
    public static class BaseTypeUtil {
        public static bool IsScalar(this BaseType t) {
            return t >= BaseType.UType && t <= BaseType.Double;
        }

        public static bool IsInteger(this BaseType t) {
            return t >= BaseType.UType && t <= BaseType.ULong;
        }

        public static bool IsFloat(this BaseType t) {
            return t == BaseType.Float || t == BaseType.Double;
        }

        public static int SizeOf(this BaseType t) {
            switch (t) {
                case BaseType.None:
                case BaseType.UType:
                case BaseType.Bool:
                case BaseType.UByte:
                    return sizeof (byte);
                case BaseType.Byte:
                    return sizeof (sbyte);
                case BaseType.Short:
                    return sizeof (short);
                case BaseType.UShort:
                    return sizeof (ushort);
                case BaseType.Int:
                case BaseType.String:
                case BaseType.Vector:
                case BaseType.Struct:
                case BaseType.Union:
                    return sizeof (int);
                case BaseType.UInt:
                    return sizeof (uint);
                case BaseType.Long:
                    return sizeof (long);
                case BaseType.ULong:
                    return sizeof (ulong);
                case BaseType.Float:
                    return sizeof (float);
                case BaseType.Double:
                    return sizeof (double);
            }
            throw new InvalidEnumArgumentException();
        }

        public static void ToSchema(this BaseType type, StringBuilder builder) {
            // don't worry about vectors, unions, etc... they're handled elsewhere.
            switch (type) {
                case BaseType.Bool:
                    builder.Append("bool");
                    break;
                case BaseType.Byte:
                    builder.Append("byte");
                    break;
                case BaseType.UByte:
                    builder.Append("ubyte");
                    break;
                case BaseType.Short:
                    builder.Append("short");
                    break;
                case BaseType.UShort:
                    builder.Append("ushort");
                    break;
                case BaseType.Int:
                    builder.Append("int");
                    break;
                case BaseType.UInt:
                    builder.Append("uint");
                    break;
                case BaseType.Long:
                    builder.Append("long");
                    break;
                case BaseType.ULong:
                    builder.Append("ulong");
                    break;
                case BaseType.Float:
                    builder.Append("float");
                    break;
                case BaseType.Double:
                    builder.Append("double");
                    break;
                case BaseType.String:
                    builder.Append("string");
                    break;
            }
        }
    }
}