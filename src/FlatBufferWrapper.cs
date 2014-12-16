using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatBuffers
{
    public class FlatBufferWrapper : Table
    {
        public FlatBufferWrapper(TypeBuilder typeBuilder, string rootTypeName, ByteBuffer byteBuffer)
            : this(
                typeBuilder,
                rootTypeName,
                byteBuffer.GetInt(byteBuffer.position()) + byteBuffer.position(),
                byteBuffer) {}

        public FlatBufferWrapper(
            TypeBuilder typeBuilder,
            string rootTypeName,
            int bb_pos,
            ByteBuffer bb)
            : this(typeBuilder.Structs.Lookup(rootTypeName), bb_pos, bb) {}

        public FlatBufferWrapper(StructDef structDef, ByteBuffer bb)
            : this(
                structDef,
                bb.GetInt(bb.position()) + bb.position(),
                bb) {}

        public FlatBufferWrapper(StructDef structDef, int bb_pos, ByteBuffer bb) {
            this.bb_pos = bb_pos;
            this.bb = bb;
            StructDef = structDef;
        }


        public object this[string key] {
            get {
                var fieldDef = StructDef.Fields.Lookup(key);
                return fieldDef == null ? null : Get(fieldDef);
            }
            set { throw new NotImplementedException(); }
        }

        public object Get(FieldDef fieldDef) {
            var o = StructDef.Fixed ? fieldDef.Value.offset : __offset(fieldDef.Value.offset);
            if (fieldDef.Value.type.BaseType.IsScalar()) {
                switch (fieldDef.Value.type.BaseType) {
                    case BaseType.Bool:
                        return StructDef.Fixed || o != 0 ? bb.Get(o + bb_pos) > 0 : default(bool);
                    case BaseType.Byte:
                        return StructDef.Fixed || o != 0 ? bb.GetSbyte(o + bb_pos) : default(sbyte);
                    case BaseType.UByte:
                        return StructDef.Fixed || o != 0 ? bb.Get(o + bb_pos) : default(byte);
                    case BaseType.Short:
                        return StructDef.Fixed || o != 0 ? bb.GetShort(o + bb_pos) : default(short);
                    case BaseType.UShort:
                        return StructDef.Fixed || o != 0 ? bb.GetUshort(o + bb_pos) : default(ushort);
                    case BaseType.Int:
                        return StructDef.Fixed || o != 0 ? bb.GetInt(o + bb_pos) : default(int);
                    case BaseType.UInt:
                        return StructDef.Fixed || o != 0 ? bb.GetUint(o + bb_pos) : default(uint);
                    case BaseType.Long:
                        return StructDef.Fixed || o != 0 ? bb.GetLong(o + bb_pos) : default(long);
                    case BaseType.ULong:
                        return StructDef.Fixed || o != 0 ? bb.GetUlong(o + bb_pos) : default(ulong);
                    case BaseType.Float:
                        return StructDef.Fixed || o != 0 ? bb.GetFloat(o + bb_pos) : default(float);
                    case BaseType.Double:
                        return StructDef.Fixed || o != 0 ? bb.GetDouble(o + bb_pos) : default(double);
                }
            }
            else if (fieldDef.Value.type.BaseType == BaseType.String) {
                return StructDef.Fixed || o != 0 ? __string(o + bb_pos) : default(string);
            }
            else if (fieldDef.Value.type.BaseType == BaseType.Struct) {
                // assuming tables for now!
                return fieldDef.Value.type.StructDef.Fixed || o != 0
                    ? new FlatBufferWrapper(fieldDef.Value.type.StructDef, o + bb_pos, bb)
                    : null;
            }
            else if (fieldDef.Value.type.BaseType == BaseType.Vector) {
                if (fieldDef.Value.type.ElementType.IsScalar()) {
                    return VectorAsScalarArray(fieldDef, o);
                }
                else if (fieldDef.Value.type.ElementType == BaseType.String) {
                    var length = o != 0 ? __vector_len(o) : 0;
                    var stringArray = new string[length];
                    for (var i = 0; i < length; i++) {
                        stringArray[i] = o != 0 ? __string(__vector(o) + i*sizeof (int)) : null;
                    }
                    return stringArray;
                }
                else if (fieldDef.Value.type.ElementType == BaseType.Struct) {
                    var length = o != 0 ? __vector_len(o) : 0;
                    var flatBufferWrapperArray = new FlatBufferWrapper[length];
                    for (var i = 0; i < length; i++) {
                        if (fieldDef.Value.type.StructDef.Fixed) {
                            flatBufferWrapperArray[i] = o != 0
                                ? new FlatBufferWrapper(fieldDef.Value.type.StructDef, __vector(o) + i*sizeof (int),
                                    bb)
                                : null;
                        }
                        else {
                            flatBufferWrapperArray[i] = o != 0
                                ? new FlatBufferWrapper(fieldDef.Value.type.StructDef,
                                    __indirect(__vector(o) + i*sizeof (int)),
                                    bb)
                                : null;
                        }
                    }
                    return flatBufferWrapperArray;
                }
            }
            throw new NotImplementedException("Unsupported type! " + fieldDef.Value.type.BaseType);
        }

        public object VectorAsScalarArray(FieldDef fieldDef, int o) {
            var length = o != 0 ? __vector_len(o) : 0;
            switch (fieldDef.Value.type.ElementType) {
                case BaseType.Bool:
                    var boolArray = new bool[length];
                    for (var i = 0; i < length; i++) {
                        boolArray[i] = o != 0 && bb.Get(__vector(o) + i*sizeof (byte)) > 0;
                    }
                    return boolArray;
                case BaseType.Byte:
                    var sbyteArray = new sbyte[length];
                    for (var i = 0; i < length; i++) {
                        sbyteArray[i] = o != 0 ? bb.GetSbyte(__vector(o) + i*sizeof (sbyte)) : (sbyte) 0;
                    }
                    return sbyteArray;
                case BaseType.UByte:
                    var byteArray = new byte[length];
                    for (var i = 0; i < length; i++) {
                        byteArray[i] = o != 0 ? bb.Get(__vector(o) + i*sizeof (byte)) : (byte) 0;
                    }
                    return byteArray;
                case BaseType.Short:
                    var shortArray = new short[length];
                    for (var i = 0; i < length; i++) {
                        shortArray[i] = o != 0 ? bb.GetShort(__vector(o) + i*sizeof (short)) : (short) 0;
                    }
                    return shortArray;
                case BaseType.UShort:
                    var ushortArray = new ushort[length];
                    for (var i = 0; i < length; i++) {
                        ushortArray[i] = o != 0 ? bb.GetUshort(__vector(o) + i*sizeof (ushort)) : (ushort) 0;
                    }
                    return ushortArray;
                case BaseType.Int:
                    var intArray = new int[length];
                    for (var i = 0; i < length; i++) {
                        intArray[i] = o != 0 ? bb.GetInt(__vector(o) + i*sizeof (int)) : (int) 0;
                    }
                    return intArray;
                case BaseType.UInt:
                    var uintArray = new uint[length];
                    for (var i = 0; i < length; i++) {
                        uintArray[i] = o != 0 ? bb.GetUint(__vector(o) + i*sizeof (uint)) : (uint) 0;
                    }
                    return uintArray;
                case BaseType.Long:
                    var longArray = new long[length];
                    for (var i = 0; i < length; i++) {
                        longArray[i] = o != 0 ? bb.GetLong(__vector(o) + i*sizeof (long)) : (long) 0;
                    }
                    return longArray;
                case BaseType.ULong:
                    var ulongArray = new ulong[length];
                    for (var i = 0; i < length; i++) {
                        ulongArray[i] = o != 0 ? bb.GetUlong(__vector(o) + i*sizeof (ulong)) : (ulong) 0;
                    }
                    return ulongArray;
                case BaseType.Float:
                    var floatArray = new float[length];
                    for (var i = 0; i < length; i++) {
                        floatArray[i] = o != 0 ? bb.GetFloat(__vector(o) + i*sizeof (float)) : (float) 0;
                    }
                    return floatArray;
                case BaseType.Double:
                    var doubleArray = new double[length];
                    for (var i = 0; i < length; i++) {
                        doubleArray[i] = o != 0 ? bb.GetDouble(__vector(o) + i*sizeof (double)) : (double) 0;
                    }
                    return doubleArray;
            }
            throw new NotImplementedException();
        }

        public object GetOrDefault(string key, Func<string, object> func) {
            object ret;
            return TryGetValue(key, out ret) ? ret : func(key);
        }

        public bool TryGetValue(string key, out object retVal) {
            var fieldDef = StructDef.Fields.Lookup(key);
            if (fieldDef != null) {
                retVal = this[key];
                return true;
            }
            retVal = null;
            return false;
        }

        public object Count {
            get { return StructDef.Fields.Count; }
            private set { }
        }

        public T LookupOrDefault<T>(string key, T defaultValue) {
            var fieldDef = StructDef.Fields.Lookup(key);
            if (fieldDef != null) {
                return (T) this[key];
            }
            return defaultValue;
        }

        public TypeBuilder TypeBuilder;
        public StructDef StructDef;
    }

    public class FlatBufferBuilderWrapper {
        public FlatBufferBuilderWrapper(TypeBuilder typeBuilder, FlatBufferBuilder builder) {
            TypeBuilder = typeBuilder;
            Builder = builder;
            CurrentStructDef = null;
        }

        public int CreateString(string value) {
            return Builder.CreateString(value);
        }
        
        public void StartTable(string typeName) {
            CurrentStructDef = GetStructDef(typeName);
            Builder.StartObject(CurrentStructDef.Fields.Count);
        }

        public int EndTable() {
            CurrentStructDef = null;
            return Builder.EndObject();
        }

        public int CreateStruct(string typeName, object[] args) {
            var structDef = GetStructDef(typeName);
            return this.CreateStruct(structDef, args);
        }

        public int CreateStruct(StructDef structDef, object[] args) {
            Builder.Prep(structDef.MinAlign, structDef.ByteSize);
            for (var i = structDef.Fields.Count - 1; i >= 0; i--) {
                var fieldDef = structDef.Fields.Symbols[i];
                switch (fieldDef.Value.type.BaseType) {
                    case BaseType.Bool:
                        Builder.PutByte((bool)args[i] ? (byte) 1 : (byte) 0);
                        break;
                    case BaseType.Byte:
                        Builder.PutSbyte((sbyte) args[i]);
                        break;
                    case BaseType.UByte:
                        Builder.PutByte((byte) args[i]);
                        break;
                    case BaseType.Short:
                        Builder.PutShort((short) args[i]);
                        break;
                    case BaseType.UShort:
                        Builder.PutUshort((ushort) args[i]);
                        break;
                    case BaseType.Int:
                        Builder.PutInt((int) args[i]);
                        break;
                    case BaseType.UInt:
                        Builder.PutUint((uint) args[i]);
                        break;
                    case BaseType.Long:
                        Builder.PutLong((long) args[i]);
                        break;
                    case BaseType.ULong:
                        Builder.PutUlong((ulong) args[i]);
                        break;
                    case BaseType.Float:
                        Builder.PutFloat((float) args[i]);
                        break;
                    case BaseType.Double:
                        Builder.PutDouble((double) args[i]);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return Builder.Offset();
        }

        public void AddStruct(string fieldName, object[] args) {
            var fieldDef = GetFieldDef(fieldName);
            var posOffset = CreateStruct(fieldDef.Value.type.StructDef, args);
            AddPreallocatedField(fieldName, posOffset, 0);
        }

        public void StartVector(string typeName, string fieldName, int count) {
            var fieldDef = GetFieldDef(typeName, fieldName);
            var elementType = fieldDef.Value.type.VectorType;
            Builder.StartVector(elementType.InlineSize, count, elementType.InlineAlignment);
        }

        public int EndVector() {
            return Builder.EndVector();
        }

        public StructDef GetStructDef(string structName) {
            var structDef = TypeBuilder.Structs.Lookup(structName);
            if (structDef == null) throw new Exception();
            return structDef;
        }

        public FieldDef GetFieldDef(string fieldName, StructDef structDef = null) {
            var fieldDef = (structDef ?? CurrentStructDef).Fields.Lookup(fieldName);
            if (fieldName == null) throw new Exception();
            return fieldDef;
        }

        public FieldDef GetFieldDef(string structName, string fieldName) {
            return GetFieldDef(fieldName, GetStructDef(structName));
        }

        public T GetConstant<T>(FieldDef fieldDef) {
            var tryParse = typeof (T).GetMethod("TryParse", new Type[] {typeof(string), typeof(T).MakeByRefType()});
            T defaultValue = default(T);
            var args = new object[] {fieldDef.Value.constant, defaultValue};
            var result = (bool) tryParse.Invoke(null, args);
            if (!result) return defaultValue;
            return (T) args[1];
        }

        public void AddValue<T>(string fieldName, Action<int, T> action) {
            var fieldDef = GetFieldDef(fieldName);
            var fieldIdx = CurrentStructDef.Fields.LookupIdx(fieldName);
            var defaultValue = GetConstant<T>(fieldDef);
            action(fieldIdx, defaultValue);
        }

        public void AddBool(string fieldName, bool value) {
            AddValue<bool>(fieldName, (offset, defaultValue) =>
                Builder.AddByte(
                    offset, value ? (byte)1 : (byte)0, defaultValue ? (byte)1 : (byte)0));
        }

        public void AddBool(bool value) {
            Builder.AddByte(value ? (byte) 1 : (byte) 0);
        }

        public void AddChar(string fieldName, sbyte value) {
            AddValue<sbyte>(fieldName, (offset, defaultValue) =>
                Builder.AddSbyte(offset, value, defaultValue));
        }

        public void AddChar(sbyte value) {
            Builder.AddSbyte(value);
        }

        public void AddUChar(string fieldName, byte value) {
            AddValue<byte>(fieldName, (offset, defaultValue) =>
                Builder.AddByte(offset, value, defaultValue));
        }

        public void AddUChar(byte value) {
            Builder.AddByte(value);
        }

        public void AddShort(string fieldName, short value) {
            AddValue<short>(fieldName, (offset, defaultValue) =>
                Builder.AddShort(offset, value, defaultValue));
        }

        public void AddShort(short value) {
            Builder.AddShort(value);
        }

        public void AddUshort(string fieldName, ushort value) {
            AddValue<ushort>(fieldName, (offset, defaultValue) =>
                Builder.AddUshort(offset, value, defaultValue));
        }

        public void AddUshort(ushort value) {
            Builder.AddUshort(value);
        }

        public void AddInt(string fieldName, int value) {
            AddValue<int>(fieldName, (offset, defaultValue) =>
                Builder.AddInt(offset, value, defaultValue));
        }

        public void AddInt(int value) {
            Builder.AddInt(value);
        }

        public void AddUInt(string fieldName, uint value) {
            AddValue<uint>(fieldName, (offset, defaultValue) =>
                Builder.AddUint(offset, value, defaultValue));
        }

        public void AddUInt(uint value) {
            Builder.AddUint(value);
        }

        public void AddLong(string fieldName, long value) {
            AddValue<long>(fieldName, (offset, defaultValue) =>
                Builder.AddLong(offset, value, defaultValue));
        }

        public void AddLong(long value) {
            Builder.AddLong(value);
        }

        public void AddULong(string fieldName, ulong value) {
            AddValue<ulong>(fieldName, (offset, defaultValue) =>
                Builder.AddULong(offset, value, defaultValue));
        }

        public void AddULong(ulong value) {
            Builder.AddULong(value);
        }

        public void AddFloat(string fieldName, float value) {
            AddValue<float>(fieldName, (offset, defaultValue) =>
                Builder.AddFloat(offset, value, defaultValue));
        }

        public void AddFloat(float value) {
            Builder.AddFloat(value);
        }

        public void AddDouble(string fieldName, double value) {
            AddValue<double>(fieldName, (offset, defaultValue) =>
                Builder.AddDouble(offset, value, defaultValue));
        }

        public void AddDouble(double value) {
            Builder.AddDouble(value);
        }

        public void AddPreallocatedField(string fieldName, int posOffset, int voffset, StructDef structDef = null) {
            var fieldDef = CurrentStructDef.Fields.Lookup(fieldName);
            var fieldIdx = CurrentStructDef.Fields.LookupIdx(fieldName);
            if (fieldDef.Value.type.IsStruct && fieldDef.Value.type.StructDef.Fixed) {
                Builder.AddStruct(fieldIdx, posOffset, voffset);
            }
            else {
                Builder.AddOffset(fieldIdx, posOffset, voffset);
            }
        }

        public void AddTable(string fieldName, int posOffset, StructDef structDef = null) {
            AddPreallocatedField(fieldName, posOffset, 0, structDef);
        }

        public void AddTable(int tableOffset) {
            Builder.AddOffset(tableOffset);
        }

        public void AddString(int stringOffset) {
            Builder.AddOffset(stringOffset);
        }

        public void AddString(string fieldName, int posOffset, StructDef structDef = null) {
            AddPreallocatedField(fieldName, posOffset, 0, structDef);
        }

        public void AddVector(string fieldName, int posOffset, StructDef structDef = null) {
            AddPreallocatedField(fieldName, posOffset, 0, structDef);
        }

        public void Finish(int offset) {
            Builder.Finish(offset);
        }

        public TypeBuilder TypeBuilder;
        public FlatBufferBuilder Builder;
        public StructDef CurrentStructDef;
    }
}
