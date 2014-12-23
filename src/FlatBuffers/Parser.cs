using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlatBuffers
{
    public class Parser
    {
        public void Parse(string schemaStr) {
            var offset = 0;
            offset = ParseIncludeStar(offset, schemaStr);
            offset = ParseDeclStar(offset, schemaStr);
        }

        public int Consume(string expected, int offset, string schemaStr) {
            int i = 0;
            while (i < expected.Length && i + offset < schemaStr.Length) {
                if (schemaStr[i + offset] != expected[i]) throw new Exception("expected " + expected);
                i++;
            }
            if (i < expected.Length) throw new Exception("expected " + expected);
            return offset + expected.Length;
        }

        public int ParseIdentifier(int offset, string schemaStr, out string identifier ) {
            var builder = new StringBuilder();
            bool finished = false;
            for (int i = 0; i + offset < schemaStr.Length && !finished; i++) {
                var c = schemaStr[offset + i];
                if (i == 0 && (Char.IsLetter(c) || c == '_')) {
                    builder.Append(c);
                }
                else if (i > 0 && Char.IsLetterOrDigit(c) || c == '_') {
                    builder.Append(c);
                }
                else {
                    finished = true;
                }
            }
            if (!finished || builder.Length == 0) throw new Exception("Expected identifier");
            identifier = builder.ToString();
            return offset + identifier.Length;
        }

        public int ParseEscapedChar(int offset, string schemaStr, StringBuilder builder) {
            offset = Consume("\\", offset, schemaStr);
            switch (schemaStr[offset]) {
                case '\\':
                    builder.Append('\\');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case '"':
                    builder.Append('"');
                    break;
                default:
                    throw new Exception("Unrecognized escape character");
            }
            return offset + 1;
        }

        public int ParseStringConstant(int offset, string schemaStr, out string fileName) {
            var builder = new StringBuilder();
            var finished = false;
            offset = Consume("\"", offset, schemaStr);
            while (offset < schemaStr.Length && !finished) {
                var c = schemaStr[offset];
                if (c == '"') {
                    finished = true;
                    offset++;
                }
                else if (schemaStr[offset] == '\\') {
                    offset = ParseEscapedChar(offset, schemaStr, builder);
                }
                else {
                    builder.Append(c);
                    offset++;
                }
            }
            if (!finished) throw new Exception("Invalid string constant");
            fileName = builder.ToString();
            return offset;
        }

        public int ParseInclude(int offset, string schemaStr, out string fileName) {
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume("include", offset, schemaStr);
            offset = SkipWhitespace(offset, schemaStr);
            offset = ParseStringConstant(offset, schemaStr, out fileName);
            offset = SkipWhitespace(offset, schemaStr);
            //TODO perform the include!! for now... just continue
            return Consume(";", offset, schemaStr);
        }

        public int ParseIncludeStar(int offset, string schemaStr) {
            var finished = false;
            while (offset < schemaStr.Length && !finished) {
                offset = SkipWhitespace(offset, schemaStr);
                if (schemaStr[offset] == 'i') {
                    string filename;
                    offset = ParseInclude(offset, schemaStr, out filename);
                }
                else {
                    finished = true;
                }
            }
            return offset;
        }

        public int ParseNamespaceDecl(int offset, string schemaStr) {
            var namespace_ = new Namespace {Components = new List<string>()};
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume("namespace", offset, schemaStr);
            offset = SkipWhitespace(offset, schemaStr);
            string identifier;
            offset = ParseIdentifier(offset, schemaStr, out identifier);
            namespace_.Components.Add(identifier);
            offset = SkipWhitespace(offset, schemaStr);
            while (offset < schemaStr.Length && schemaStr[offset] == '.') {
                offset++;
                offset = ParseIdentifier(offset, schemaStr, out identifier);
                namespace_.Components.Add(identifier);
                offset = SkipWhitespace(offset, schemaStr);
            }
            offset = Consume(";", offset, schemaStr);
            TypeBuilder.Namespaces.Add(namespace_);
            return offset;
        }

        public int ParseAttributeDecl(int offset, string schemaStr) {
            string attribute;
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume("attribute", offset, schemaStr);
            offset = SkipWhitespace(offset, schemaStr);
            offset = ParseStringConstant(offset, schemaStr, out attribute);
            offset = Consume(";", offset, schemaStr);
            TypeBuilder.Attribute = attribute;
            return offset;
        }

        public int ParseTableDecl(int offset, string schemaStr) {
            offset = SkipWhitespace(offset, schemaStr);
            string name;           
            offset = ParseIdentifier(offset, schemaStr, out name);
            var structDef = TypeBuilder.AddTable(name);
            offset = ParseTypeDecl(offset, schemaStr, structDef);
            return offset;
        }

        public int ParseStructDecl(int offset, string schemaStr) {
            offset = SkipWhitespace(offset, schemaStr);
            string name;
            offset = ParseIdentifier(offset, schemaStr, out name);
            var structDef = TypeBuilder.AddStruct(name);
            offset = ParseTypeDecl(offset, schemaStr, structDef);
            return offset;
        }

        public int ParseTypeDecl(int offset, string schemaStr, StructDef structDef) {
            offset = SkipWhitespace(offset, schemaStr);
            // TODO?? offset = ParseMetaData(offset, schemaStr, structDef);
            offset = Consume("{", offset, schemaStr);
            offset = ParseFieldDeclPlus(offset, schemaStr, structDef);
            offset = Consume("}", offset, schemaStr);
            return offset;
        }

        public int ParseFieldDeclPlus(int offset, string schemaStr, StructDef structDef) {
            offset = ParseFieldDecl(offset, schemaStr, structDef);
            var finished = false;
            while (offset < schemaStr.Length && !finished) {
                offset = SkipWhitespace(offset, schemaStr);
                if (schemaStr[offset] == '}') {
                    finished = true;
                }
                else {
                    offset = ParseFieldDecl(offset, schemaStr, structDef);
                }
            }
            return offset;
        }

        public int ParseFieldDecl(int offset, string schemaStr, StructDef structDef) {
            offset = SkipWhitespace(offset, schemaStr);
            string identifier;
            offset = ParseIdentifier(offset, schemaStr, out identifier);
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume(":", offset, schemaStr);
            offset = SkipWhitespace(offset, schemaStr);
            FlatBuffersType type;
            string constantValue = null;
            offset = ParseType(offset, schemaStr, out type);
            offset = SkipWhitespace(offset, schemaStr);
            if (schemaStr[offset] == '=') {
                offset = ParseConstant(offset, schemaStr, out constantValue);
                offset = SkipWhitespace(offset, schemaStr);
            }
            /* TODO?? these are the "attributes" but correspond to "metadata" grammar rule
            if (schemaStr[offset] != ';') {
                offset = ParseMetaData(offset, schemaStr);
            }*/
            offset = Consume(";", offset, schemaStr);
            structDef.AddField(identifier, type, constantValue);
            return offset;
        }

        public int ParseConstant(int offset, string schemaStr, out string constantValue) {
            var builder = new StringBuilder();
            offset = SkipWhitespace(offset, schemaStr);
            if (schemaStr[offset] == '-') {
                builder.Append('-');
                offset++;
            }
            bool finished = false;
            while (offset < schemaStr.Length && !finished) {
                var c = schemaStr[offset];
                if (Char.IsDigit(c) || c == '.') {
                    builder.Append(c);
                    offset++;
                }
                else {
                    finished = true;
                }
            }
            constantValue = builder.ToString();
            return offset;
        }

        public int ParseType(int offset, string schemaStr, out FlatBuffersType type) {
            type = new FlatBuffersType();
            offset = SkipWhitespace(offset, schemaStr);
            if (schemaStr[offset] == '[') {
                return ParseVectorType(offset, schemaStr, out type);
            }
            else {
                type.BaseType = BaseType.None;
                offset = ParseTypeIdentifier(offset, schemaStr, type);
            }
            return offset;
        }

        public int ParseVectorType(int offset, string schemaStr, out FlatBuffersType type) {
            type = new FlatBuffersType();
            type.BaseType = BaseType.Vector;
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume("[", offset, schemaStr);
            offset = ParseTypeIdentifier(offset, schemaStr, type);
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume("]", offset, schemaStr);
            return offset;
        }

        public int ParseTypeIdentifier(int offset, string schemaStr, FlatBuffersType type) {
            offset = SkipWhitespace(offset, schemaStr);
            string identifier;
            offset = ParseIdentifier(offset, schemaStr, out identifier);
            var baseType = BaseType.None;
            if (identifier == "bool") baseType = BaseType.Bool;
            else if (identifier == "byte") baseType = BaseType.Byte;
            else if (identifier == "ubyte") baseType = BaseType.UByte;
            else if (identifier == "short") baseType = BaseType.Short;
            else if (identifier == "ushort") baseType = BaseType.UShort;
            else if (identifier == "int") baseType = BaseType.Int;
            else if (identifier == "uint") baseType = BaseType.UInt;
            else if (identifier == "long") baseType = BaseType.Long;
            else if (identifier == "ulong") baseType = BaseType.ULong;
            else if (identifier == "float") baseType = BaseType.Float;
            else if (identifier == "double") baseType = BaseType.Double;
            else if (identifier == "string") baseType = BaseType.String;

            Definition def = null;
            if (baseType == BaseType.None) {
                // must be a type defined earlier!
                def = TypeBuilder.Enums.Lookup(identifier);
                if (def == null) {
                    def = TypeBuilder.LookupOrCreateStruct(identifier);
                }
            }

            if (type.BaseType == BaseType.Vector) {
                if (def != null && def is EnumDef) {
                    var enumDef = def as EnumDef;
                    type.ElementType = enumDef.UnderlyingType.BaseType;
                    type.EnumDef = enumDef;
                }
                else if (def != null && def is StructDef) {
                    type.ElementType = BaseType.Struct;
                    type.StructDef = def as StructDef;
                } else if (baseType != BaseType.None) {
                    type.ElementType = baseType;
                }
                else {
                    throw new Exception("Unexpected type input");
                }
            }
            else if (def != null) {
                if (def is EnumDef) {
                    var enumDef = def as EnumDef;
                    type.BaseType = enumDef.UnderlyingType.BaseType;
                    type.EnumDef = enumDef;
                }
                else if (def is StructDef) {
                    type.BaseType = BaseType.Struct;
                    type.StructDef = def as StructDef;
                }
                else {
                    throw new Exception("Unexpected type input");
                }
            }
            else {
                type.BaseType = baseType;
            }
            return offset;
        }

        public int ParseEnumDecl(int offset, string schemaStr, bool isUnion) {
            offset = SkipWhitespace(offset, schemaStr);
            string name;
            offset = ParseIdentifier(offset, schemaStr, out name);
            var underlyingType = isUnion ? BaseType.UType : BaseType.Int; // by default
            offset = SkipWhitespace(offset, schemaStr);
            if (!isUnion && schemaStr[offset] == ':') {
                offset++;
                offset = SkipWhitespace(offset, schemaStr);
                var tempType = new FlatBuffersType {BaseType = BaseType.None};
                offset = ParseTypeIdentifier(offset, schemaStr, tempType);
                underlyingType = tempType.BaseType;
            }
            if (!underlyingType.IsScalar()) {
                throw new Exception("Enum must be a scalar type");
            }
            var enumDef = TypeBuilder.AddEnum(name, underlyingType);
            if (isUnion) {
                enumDef.UnderlyingType.EnumDef = enumDef;
            }
            //TODO metadata, again, but as "attributes" on the enumDef
            offset = SkipWhitespace(offset, schemaStr);
            offset = Consume("{", offset, schemaStr);
            offset = ParseEnumValDeclStar(offset, schemaStr, enumDef, isUnion);
            return offset;
        }

        private int ParseEnumValDeclStar(int offset, string schemaStr, EnumDef enumDef, bool isUnion) {
            var finished = false;
            var expectComma = false;
            while (offset < schemaStr.Length && !finished) {
                offset = SkipWhitespace(offset, schemaStr);
                if (schemaStr[offset] == '}') {
                    finished = true;
                    offset++;
                }
                else {
                    if (expectComma) {
                        offset = Consume(",", offset, schemaStr);
                        offset = SkipWhitespace(offset, schemaStr);
                    }
                    else {
                        expectComma = true;
                    }
                    offset = ParseEnumValDecl(offset, schemaStr, enumDef, isUnion);
                }
            }
            return offset;
        }

        private int ParseEnumValDecl(int offset, string schemaStr, EnumDef enumDef, bool isUnion) {
            string identifier;
            string constantValue = null;
            offset = ParseIdentifier(offset, schemaStr, out identifier);
            offset = SkipWhitespace(offset, schemaStr);
            if (schemaStr[offset] == '=') {
                offset++;
                offset = ParseConstant(offset, schemaStr, out constantValue);
            }
            var enumVal = new EnumVal();
            enumVal.Name = identifier;
            if (constantValue != null) enumVal.Value = long.Parse(constantValue);
            if (isUnion && enumVal.Value.Value != 0) enumVal.StructDef = TypeBuilder.LookupOrCreateStruct(identifier);
            enumDef.Add(enumVal);
            return offset;
        }

        public int ParseDecl(int offset, string schemaStr) {
            offset = SkipWhitespace(offset, schemaStr);
            if (offset >= schemaStr.Length) return offset;
            if (schemaStr[offset] == '{') throw new Exception("JSON objects not supported yet.");
            string identifier;
            offset = ParseIdentifier(offset, schemaStr, out identifier);
            if (identifier == "namespace") {
                offset = ParseNamespaceDecl(offset, schemaStr);
            }
            else if (identifier == "attribute") {
                offset = ParseAttributeDecl(offset, schemaStr);
            }
            else if (identifier == "table") {
                offset = ParseTableDecl(offset, schemaStr);
            }
            else if (identifier == "struct") {
                offset = ParseStructDecl(offset, schemaStr);
            }
            else if (identifier == "enum") {
                offset = ParseEnumDecl(offset, schemaStr, false);
            }
            else if (identifier == "union") {
                offset = ParseEnumDecl(offset, schemaStr, true);
            }
            else if (identifier == "attribute") {
                offset = ParseAttributeDecl(offset, schemaStr);
            }
            else if (identifier == "root_type") {
                offset = ParseRootTypeDecl(offset, schemaStr);
            }
            return offset;
        }

        public int ParseDeclStar(int offset, string schemaStr) {
            var finished = false;
            while (offset < schemaStr.Length) {
                offset = ParseDecl(offset, schemaStr);
            }
            return offset;
        }

        public int ParseRootTypeDecl(int offset, string schemaStr) {
            offset = SkipWhitespace(offset, schemaStr);
            string identifier;
            offset = ParseIdentifier(offset, schemaStr, out identifier);
            offset = Consume(";", offset, schemaStr);
            TypeBuilder.Root = TypeBuilder.LookupOrCreateStruct(identifier);
            return offset;
        }

        public int SkipWhitespace(int offset, string schemaStr) {
            while (offset < schemaStr.Length && Char.IsWhiteSpace((schemaStr[offset]))) offset++;
            return offset;
        }

        public TypeBuilder TypeBuilder = new TypeBuilder();
    }
}
