using System.Collections.Generic;
using System.Linq;

namespace FlatBuffers {
    public class SymbolTable<T> where T : class {
        public SymbolTable() {
            Symbols = new List<T>();
            _symbolsDict = new Dictionary<string, T>();
            _symbolsIdxDict = new Dictionary<string, int>();
        } 

        public bool Add(string name, T e) {
            _symbolsIdxDict[name] = Symbols.Count;
            Symbols.Add(e);
            if (_symbolsDict.ContainsKey(name)) return true;
            _symbolsDict[name] = e;
            return false;
        }

        public T Lookup(string name) {
            T ret;
            if (_symbolsDict.TryGetValue(name, out ret)) {
                return ret;
            }
            return null;
        }

        public int LookupIdx(string name) {
            int ret;
            if (!_symbolsIdxDict.TryGetValue(name, out ret)) return -1;
            return ret;
        }

        public int Count {
            get { return Symbols.Count; }
        }

        public T Last {
            get { return Symbols.Last(); }
        }

        public T this[int index] {
            get { return Symbols[index]; }
        }

        public List<T> Symbols; 
        private Dictionary<string, T> _symbolsDict;
        private Dictionary<string, int> _symbolsIdxDict;
    }
}