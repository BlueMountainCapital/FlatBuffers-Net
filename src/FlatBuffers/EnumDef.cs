using System;
using System.Text;

namespace FlatBuffers {
    [Serializable]
    public class EnumDef : Definition {
        public EnumVal ReverseLookup(int enumIndex, bool skipUnionDefault = true) {
            int skip = 0;
            if (IsUnion && skipUnionDefault) skip++;
            for (var index = 0 + skip; index < Values.Count; index++) {
                if (Values[index].Value == enumIndex) return Values[index];
            }
            return null;
        }

        public void Add(EnumVal value) {
            if (Values.Add(value.Name, value)) {
                throw new Exception("enum value already exists");
            }
        }

        public bool BitFlags {
            get {
                return _BitFlags_cached.HasValue ?
                    _BitFlags_cached.Value :
                    (_BitFlags_cached = Attributes.Lookup("bit_flags") != null).Value;
            } 
        }

        public override void Compile() {
            long previousValue = long.MinValue;
            for (var i = 0; i < Values.Count; i++) {
                var value = Values[i];
                if (IsUnion && value.Name != "NONE" && value.StructDef == null) {
                    value.StructDef = TypeBuilder.LookupOrCreateStruct(value.Name);
                }
                if (value.Value.HasValue && value.Value.Value < previousValue) {
                    throw new Exception("enum values must be specified in ascending order");
                }
                if (!value.Value.HasValue) {
                    value.Value = previousValue + 1;
                }
                previousValue = value.Value.Value;
                if (BitFlags) {
                    if (value.Value >= UnderlyingType.BaseType.SizeOf()*8)
                        throw new Exception("bit flag out of range of underlying integral type");
                    value.Value = 1L << (int) value.Value.Value;
                }
            }
        }

        public void ToSchema(StringBuilder builder) {
            builder.AppendLine();
            if (IsUnion) {
                builder.Append("union ");
                builder.Append(Name);
            }
            else {
                builder.Append("enum ");
                builder.Append(Name);
                builder.Append(" : ");
                UnderlyingType.BaseType.ToSchema(builder);
            }
            builder.Append(" {\n");
            var addComma = false;
            foreach (var enumVal in Values.Symbols) {
                if (IsUnion && enumVal.Name == "NONE") continue;
                if (addComma) builder.Append(", ");
                else addComma = true;
                enumVal.ToSchema(builder);
            }
            builder.Append("}\n");
        }

        public SymbolTable<EnumVal> Values = new SymbolTable<EnumVal>();
        public bool IsUnion = false;
        public FlatBuffersType UnderlyingType = new FlatBuffersType();
        private bool? _BitFlags_cached;
  
    }
}