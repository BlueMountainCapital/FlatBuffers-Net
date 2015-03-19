using System;

namespace FlatBuffers {
    [Serializable]
    public abstract class Definition {
        public abstract void Compile();

        public string Name;
        public string DocComment;
        public SymbolTable<Value> Attributes = new SymbolTable<Value>();
        public bool Generated;
        public Namespace? DefinedNamespace;
        public TypeBuilder TypeBuilder;
    }
}