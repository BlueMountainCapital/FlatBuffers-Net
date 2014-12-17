using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace FlatBuffers {
    public class TypeBuilder {
        public EnumDef AddUnion(string name) {
            return AddEnum(name, BaseType.UType);
        }

        public EnumDef AddEnum(string name, BaseType t) {
            if (!t.IsInteger()) throw new InvalidEnumArgumentException("Underlying enum type must be integral.");
            var def = new EnumDef {
                Name = name,
                IsUnion = t == BaseType.UType,
                TypeBuilder = this,
            };
            if (Enums.Add(name, def)) throw new Exception("enum already exists!");
            def.UnderlyingType.BaseType = t;
            def.UnderlyingType.EnumDef = def;
            if (def.IsUnion) {
                def.Values.Add("NONE", new EnumVal {Name = "NONE", Value = 0,});
            }
            return def;
        }

        public StructDef AddStruct(string name, bool isFixed = true) {
            var def = LookupOrCreateStruct(name);
            def.Predecl = false;
            def.Fixed = isFixed;
            return def;
        }

        public StructDef AddTable(string name) {
            return AddStruct(name, false);
        }

        public StructDef LookupOrCreateStruct(string name) {
            var def = Structs.Lookup(name);
            if (def == null) {
                def = new StructDef {
                    Name = name,
                    Predecl = true,
                    TypeBuilder = this,
                };
                Structs.Add(name, def);
            }
            return def;
        }

        public void Compile() {
            // process everything, calculating tables, etc...
            foreach (var s in Structs.Symbols) {
                if (s.Predecl)
                    throw new Exception("type referenced but not defined: " + s.Name);
                s.Compile();
            }
            foreach (var e in Enums.Symbols) {
                e.Compile();
                if (e.IsUnion) {
                    foreach (var value in e.Values.Symbols) {
                        if (value.StructDef != null && value.StructDef.Fixed)
                            throw new Exception("only tables can be union elements: " + value.Name);
                    }
                }
            }
        }

        public string ToSchema() {
            var builder = new StringBuilder();
            foreach (var enumDef in Enums.Symbols) {
                enumDef.ToSchema(builder);
            }
            foreach (var structDef in Structs.Symbols) {
                structDef.ToSchema(builder);
            }
            if (Root != null) {
                builder.Append("\nroot_type ");
                builder.Append(Root.Name);
                builder.Append(";\n");
            }
            return builder.ToString();
        }

        public SymbolTable<StructDef> Structs = new SymbolTable<StructDef>();
        public SymbolTable<EnumDef> Enums = new SymbolTable<EnumDef>();
        public List<Namespace> Namespaces = new List<Namespace>();
        public StructDef Root = null;
        public string Attribute = null;
    }
}