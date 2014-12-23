using System;
using System.Linq;
using System.Text;

namespace FlatBuffers {
    public class StructDef : Definition {
        public void PadLastField(int minAlign) {
            var padding = PaddingBytes(ByteSize, minAlign);
            ByteSize += padding;
            Fields.Last.Padding = padding;
        }

        private int PaddingBytes(int bufSize, int scalarSize) {
            return ((~bufSize) + 1) & (scalarSize - 1);
        }

        public bool SortBySize { get { return Attributes.Lookup("original_order") == null && !Fixed; } }

        private FieldDef AddFieldCreate(string name, FlatBuffersType type) {
            var fieldOffset = FieldIndexToOffset((ushort) Fields.Count);
            var fieldDef = new FieldDef {
                Name = name,
                Value = new Value {offset = fieldOffset, type = type},
                TypeBuilder = TypeBuilder,
            };
            if (Fixed) {
                var size = fieldDef.Value.type.InlineSize;
                var alignment = fieldDef.Value.type.InlineAlignment;
                // structs_ need to have a predictable format, so we need to align to
                // the largest scalar
                MinAlign = Math.Max(MinAlign, alignment);
                if (Fields.Count > 0) PadLastField(alignment);
                fieldDef.Value.offset = (ushort)ByteSize;
                ByteSize += size;
            }
            if (Fields.Add(fieldDef.Name, fieldDef))
                throw new Exception("field already exists!");
            return fieldDef;           
        }

        public FieldDef AddField(string name, FlatBuffersType type, string defaultValue = null,
            SymbolTable<Value> attributes = null) {
            if (Fixed && !type.BaseType.IsScalar() && !type.IsStruct)
                throw new Exception("structs may contain only scalar or struct fields");
            FieldDef typeField = null;
            if (type.BaseType == BaseType.Union) {
                // For union fields, add a second auto-generated field to hold the type
                typeField = AddField(name + "_type", type.EnumDef.UnderlyingType);
            }
            var field = AddFieldCreate(name, type);
            field.Attributes = attributes ?? field.Attributes;
            if (defaultValue != null) {
                if (!type.BaseType.IsScalar())
                    throw new Exception("default values currently only suppoerted for scalars");
                field.Value.constant = defaultValue;
            }
            if (type.EnumDef != null
                && type.BaseType.IsScalar()
                && !Fixed
                && !type.EnumDef.BitFlags
                && field.Value.constant != null
                && type.EnumDef.ReverseLookup(Int32.Parse(field.Value.constant)) != null)
                throw new Exception("enum " + type.EnumDef.Name +
                                    " does not have a declaration for this field's default of " + field.Value.constant);
            field.Deprecated = field.Attributes.Lookup("deprecated") != null;
            if (field.Deprecated && (Fixed || field.Value.type.BaseType.IsScalar()))
                throw new Exception("can't deprecate fields in a struct");
            var nested = field.Attributes.Lookup("nested_flatbuffer");
            if (nested != null) {
                if (nested.type.BaseType != BaseType.String)
                    throw new Exception("nested_flatbuffer attribute must be a string (the root type)");
                if (field.Value.type.BaseType != BaseType.Vector
                    || field.Value.type.ElementType != BaseType.UByte)
                    throw new Exception("nested_flatbuffer attribute may only apply to a vector of ubyte");
                TypeBuilder.LookupOrCreateStruct(nested.constant);
            }
            // If this field is a union, and it has a manually assigned id, the automatically added type field should have an id as well (of N - 1).
            if (typeField != null) {
                var attr = field.Id;
                if (attr != null) {
                    var id = int.Parse(attr.constant);
                    var value = new Value();
                    value.type = field.Id.type;
                    value.constant = (id - 1).ToString();
                    typeField.Attributes.Add("id", value);
                }
            }
            return field;
        }

        public bool OriginalOrder {
            get { return Attributes.Lookup("original_order") != null; }
        }

        public ushort FieldIndexToOffset(ushort fieldId) {
            const ushort fixedFields = 2; // Vtable size and Object size
            return (ushort) ((fieldId + fixedFields)*sizeof (ushort));
        }

        public override void Compile() {
            if (!Fixed) {
                for (var i = 0; i < Fields.Count; i++) {
                    var field = Fields[i];
                    field.Value.offset = FieldIndexToOffset((ushort) i);
                }
            }
            var forceAlign = Attributes.Lookup("force_align");
            if (Fixed && forceAlign != null) {
                var align = Int32.Parse(forceAlign.constant);
                if (forceAlign.type.BaseType != BaseType.Int
                    || align < MinAlign
                    || align > 256
                    || (align & (align - 1)) != 0)
                    throw new Exception(
                        "force_align must be a power of two integer ranging from the struct's natural alignment to 256");
                MinAlign = align;
            }
            PadLastField(MinAlign);
            if (!Fixed && Fields.Count > 0) {
                var countIdFields = Fields.Symbols.Sum(f => f.Id != null ? 1 : 0);
                if (countIdFields > 0) {
                    if (countIdFields != Fields.Count)
                        throw new Exception("either all fields or no fields must have an 'id' attribute");
                    Fields.Symbols = Fields.Symbols.OrderBy(f => int.Parse(f.Id.constant)).ToList();
                    // Verify we have a contiguous set and reassign vtable offsets.
                    for (var i = 0; i < Fields.Count; i++) {
                        if (i != int.Parse(Fields[i].Id.constant))
                            throw new Exception("Fields id's must be consecutive from 0, id " + i +
                                                " missing or set twice");
                        Fields[i].Value.offset = FieldIndexToOffset((ushort) i);
                    }
                }
            }
            // Check that no identifiers clash with auto generated fields.
            CheckClash("_type", BaseType.Union);
            CheckClash("Type", BaseType.Union);
            CheckClash("_length", BaseType.Vector);
            CheckClash("Length", BaseType.Vector);
        }

        public void CheckClash(string suffix, BaseType baseType) {
            foreach (var field in Fields.Symbols) {
                if (field.Name.Length > suffix.Length
                    && field.Name.EndsWith(suffix)
                    && field.Value.type.BaseType != BaseType.UType) {
                    var originalField = Fields.Lookup(field.Name.Substring(0, field.Name.Length - suffix.Length));
                    if (originalField != null && field.Value.type.BaseType == baseType)
                        throw new Exception("Field " + field.Name + " would clash with generated functions for field " +
                                            originalField.Name);
                }
            }
        }

        public void ToSchema(StringBuilder builder) {
            if (Fixed) builder.Append("struct ");
            else builder.Append("table ");
            builder.Append(Name);
            builder.Append(" {\n");
            foreach (var fieldDef in Fields.Symbols) {
                fieldDef.ToSchema(builder);
            }
            builder.Append("}\n");
        }

        public SymbolTable<FieldDef> Fields = new SymbolTable<FieldDef>();
        public bool Fixed = false;
        public bool Predecl = true;
        public int MinAlign = 1;
        public int ByteSize = 0;

    }
}