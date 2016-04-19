using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FlatBuffers
{
    [Serializable]
    public class FlatBufferWrapper : Table, ISerializable
    {
        private readonly bool _forceDefaults = false;

        public FlatBufferWrapper(TypeBuilder typeBuilder, string rootTypeName, ByteBuffer byteBuffer, bool forceDefaults = false)
            : this(
                typeBuilder,
                rootTypeName,
                byteBuffer.GetInt(byteBuffer.Position) + byteBuffer.Position,
                byteBuffer) {
            _forceDefaults = forceDefaults;
        }

        public FlatBufferWrapper(
            TypeBuilder typeBuilder,
            string rootTypeName,
            int bb_pos,
            ByteBuffer bb)
            : this(typeBuilder.Structs.Lookup(rootTypeName), bb_pos, bb) {}

        public FlatBufferWrapper(StructDef structDef, ByteBuffer bb)
            : this(
                structDef,
                bb.GetInt(bb.Position) + bb.Position,
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
        }

        public object Get(FieldDef fieldDef) {
            var o = StructDef.Fixed ? fieldDef.Value.offset : __offset(fieldDef.Value.offset);
            if (fieldDef.Value.type.BaseType.IsScalar()) {
                switch (fieldDef.Value.type.BaseType) {
                    case BaseType.Bool:
                        return StructDef.Fixed || o != 0 ? bb.Get(o + bb_pos) > 0 : _forceDefaults ? null : (object) default(bool);
                    case BaseType.Byte:
                        return StructDef.Fixed || o != 0 ? bb.GetSbyte(o + bb_pos) : _forceDefaults ? null : (object)default(sbyte);
                    case BaseType.UByte:
                        return StructDef.Fixed || o != 0 ? bb.Get(o + bb_pos) : _forceDefaults ? null : (object)default(byte);
                    case BaseType.Short:
                        return StructDef.Fixed || o != 0 ? bb.GetShort(o + bb_pos) : _forceDefaults ? null : (object)default(short);
                    case BaseType.UShort:
                        return StructDef.Fixed || o != 0 ? bb.GetUshort(o + bb_pos) : _forceDefaults ? null : (object)default(ushort);
                    case BaseType.Int:
                        return StructDef.Fixed || o != 0 ? bb.GetInt(o + bb_pos) : _forceDefaults ? null : (object)default(int);
                    case BaseType.UInt:
                        return StructDef.Fixed || o != 0 ? bb.GetUint(o + bb_pos) : _forceDefaults ? null : (object)default(uint);
                    case BaseType.Long:
                        return StructDef.Fixed || o != 0 ? bb.GetLong(o + bb_pos) : _forceDefaults ? null : (object)default(long);
                    case BaseType.ULong:
                        return StructDef.Fixed || o != 0 ? bb.GetUlong(o + bb_pos) : _forceDefaults ? null : (object)default(ulong);
                    case BaseType.Float:
                        return StructDef.Fixed || o != 0 ? bb.GetFloat(o + bb_pos) : _forceDefaults ? null : (object)default(float);
                    case BaseType.Double:
                        return StructDef.Fixed || o != 0 ? bb.GetDouble(o + bb_pos) : _forceDefaults ? null : (object)default(double);
                    case BaseType.UType:
                        return StructDef.Fixed || o != 0 ? bb.Get(o + bb_pos) : _forceDefaults ? null : (object)default(byte);
                }
            }
            else if (fieldDef.Value.type.BaseType == BaseType.String) {
                return StructDef.Fixed || o != 0 ? __string(o + bb_pos) : default(string);
            }
            else if (fieldDef.Value.type.BaseType == BaseType.Struct) {
                // assuming tables for now!
                if (StructDef.Fixed || o != 0) {
                    if (fieldDef.Value.type.StructDef.Fixed) {
                        return new FlatBufferWrapper(fieldDef.Value.type.StructDef, o + bb_pos, bb);
                    }
                    else if (o != 0) {
                        return new FlatBufferWrapper(fieldDef.Value.type.StructDef, __indirect(o + bb_pos), bb);
                    }
                }
                return null;
            }
            else if (fieldDef.Value.type.BaseType == BaseType.Union) {
                var unionEnumDef = fieldDef.Value.type.EnumDef;
                var unionTypeValue = this[fieldDef.Name + "_type"];
                var valueStructDef = unionEnumDef.ReverseLookup((byte) unionTypeValue).StructDef;
                return o != 0
                    ? __union(new FlatBufferWrapper(valueStructDef, bb), o)
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

        public StructDef StructDef;

        public byte[] Bytes { get { return bb.Data; } }

        #region Serialization support

        [Serializable]
        private struct Data {
            public StructDef StructDef;            
            public byte[] ByteBuf;        
        }

        [Serializable]
        protected class DataSurrogate : IObjectReference, ISerializable {
            
            [NonSerialized] private readonly SerializationInfo _info;

            protected DataSurrogate() { }

            protected DataSurrogate(SerializationInfo info, StreamingContext context) {
                _info = info;
            }

            public object GetRealObject(StreamingContext context) {
                var data = (Data)_info.GetValue("data", typeof(Data));
                var buf = new ByteBuffer(data.ByteBuf);
                return new FlatBufferWrapper(data.StructDef, buf);
            }

            // placeholder, just to capture Serialization info via special constructor
            public void GetObjectData(SerializationInfo info, StreamingContext context) {
                throw new NotImplementedException();
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            var data = new Data {  
                StructDef = this.StructDef,
                ByteBuf = this.bb.Data,
            };

            info.AddValue("data", data);
            info.SetType(typeof(DataSurrogate));
        }

        #endregion
    }
}
