using System.Collections.Generic;
using System.Text;

namespace FlatBuffers {
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