using System;

namespace FlatBuffers {
    [Serializable]
    public class Value {
        public FlatBuffersType type;
        public string constant;
        public ushort offset;
    }
}