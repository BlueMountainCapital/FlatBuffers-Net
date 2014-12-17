using System.Text;

namespace FlatBuffers {
    public class FlatBuffersType {
        public FlatBuffersType(BaseType baseType = BaseType.None, StructDef structDef = null, EnumDef enumDef = null) {
            BaseType = baseType;
            ElementType = BaseType.None;
            StructDef = structDef;
            EnumDef = enumDef;
        }

        public FlatBuffersType VectorType {
            get {
                return new FlatBuffersType {BaseType = ElementType, StructDef = StructDef, EnumDef = EnumDef};
            }
        }

        public bool IsStruct {
            get { return BaseType == BaseType.Struct && StructDef.Fixed; }
        }

        public int InlineSize {
            get { return IsStruct ? StructDef.ByteSize : BaseType.SizeOf(); }
        }

        public int InlineAlignment {
            get { return IsStruct ? StructDef.MinAlign : BaseType.SizeOf(); }
        }

        public void ToSchema(StringBuilder builder) {
            if (BaseType == BaseType.Vector) {
                builder.Append('[');
                if (ElementType == BaseType.Struct) builder.Append(StructDef.Name);
                else if (EnumDef != null) builder.Append(EnumDef.Name);
                else ElementType.ToSchema(builder);
                builder.Append(']');
            }
            else if (BaseType == BaseType.Struct) {
                builder.Append(StructDef.Name);
            }
            else if (EnumDef != null) builder.Append(EnumDef.Name);
            else BaseType.ToSchema(builder);
        }

        public BaseType BaseType;
        public BaseType ElementType;
        public StructDef StructDef;
        public EnumDef EnumDef;
    }
}