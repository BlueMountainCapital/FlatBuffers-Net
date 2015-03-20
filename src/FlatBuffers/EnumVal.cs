using System;
using System.Collections.Generic;
using System.Text;

namespace FlatBuffers {
    [Serializable]
    public class EnumVal {
        public EnumVal() {
            DocComment = new List<string>();
        }

        public void ToSchema(StringBuilder builder) {
            builder.Append(Name);
            if (Value.HasValue) {
                builder.Append(" = ");
                builder.Append(Value.Value);
            }
        }

        public string Name;
        public List<string> DocComment;
        public long? Value;
        public StructDef StructDef;
    }
}