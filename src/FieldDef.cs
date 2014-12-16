using System.Text;

namespace FlatBuffers {
    public class FieldDef : Definition {
        public Value Id {
            get {
                return Attributes.Lookup("id");
            }
        }

        public override void Compile() {
            // Nothing to do.
        }

        public void ToSchema(StringBuilder builder) {
            builder.Append(Name);
            builder.Append(" : ");
            Value.type.ToSchema(builder);
            builder.Append(";\n");
        }

        public Value Value;
        public bool Deprecated = false;
        public bool Required = false;
        public int Padding = 0;
        public bool Used = false;
    }
}