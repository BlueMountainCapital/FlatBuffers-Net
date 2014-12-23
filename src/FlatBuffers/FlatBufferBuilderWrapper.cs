using System;

namespace FlatBuffers {
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

        public void AddSbyte(string fieldName, sbyte value) {
            AddValue<sbyte>(fieldName, (offset, defaultValue) =>
                Builder.AddSbyte(offset, value, defaultValue));
        }

        public void AddSbyte(sbyte value) {
            Builder.AddSbyte(value);
        }

        public void AddByte(string fieldName, byte value) {
            AddValue<byte>(fieldName, (offset, defaultValue) =>
                Builder.AddByte(offset, value, defaultValue));
        }

        public void AddByte(byte value) {
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