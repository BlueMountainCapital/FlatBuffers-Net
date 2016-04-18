using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlatBuffers.Test
{
    [TestClass]
    public class RuntimeTypeModelTest {
        internal TypeBuilder _typeBuilder = new TypeBuilder();

        public void CreateMonsterFlatBufferTypes() {
            if (_typeBuilder.Structs.Count > 0) return;
            var color = _typeBuilder.AddEnum("Color", BaseType.Byte);

            color.Add(new EnumVal {Name = "Red", Value = 0,});
            color.Add(new EnumVal {Name = "Green",}); // didn't set value
            color.Add(new EnumVal {Name = "Blue", Value = 2,});

            // each of the EnumVals should end up associated to a table.
            var any = _typeBuilder.AddUnion("Any");
            any.Add(new EnumVal { Name = "Monster" });
            any.Add(new EnumVal { Name = "Weapon" });
            any.Add(new EnumVal { Name = "Pickup" });

            var vec3 = _typeBuilder.AddStruct("Vec3");
            var vec3_x = vec3.AddField("x", new FlatBuffersType {BaseType = BaseType.Float});
            var vec3_y = vec3.AddField("y", new FlatBuffersType {BaseType = BaseType.Float});
            var vec3_z = vec3.AddField("z", new FlatBuffersType {BaseType = BaseType.Float});

            var minion = _typeBuilder.AddTable("Minion");
            var minion_name = minion.AddField("name", new FlatBuffersType {BaseType = BaseType.String});

            var monster = _typeBuilder.AddTable("Monster");
            var monster_pos = monster.AddField("pos", new FlatBuffersType {BaseType = BaseType.Struct, StructDef = vec3,});
            var monster_mana = monster.AddField("mana", new FlatBuffersType {BaseType = BaseType.Short});
            var monster_hp = monster.AddField("hp", new FlatBuffersType {BaseType = BaseType.Short});
            var monster_name = monster.AddField("name", new FlatBuffersType {BaseType = BaseType.String});
            var monster_friendly = monster.AddField("friendly", new FlatBuffersType {BaseType = BaseType.Bool});
            var monster_inventory = monster.AddField("inventory",
                new FlatBuffersType {BaseType = BaseType.Vector, ElementType = BaseType.UByte});
            var monster_color = monster.AddField("color",
                new FlatBuffersType {BaseType = BaseType.UByte, EnumDef = color});
            var monster_minions = monster.AddField("minions",
                new FlatBuffersType {BaseType = BaseType.Vector, ElementType = BaseType.Struct, StructDef = minion});
            var monster_main_minion = monster.AddField("mainMinion",
                new FlatBuffersType {BaseType = BaseType.Struct, StructDef = minion});

            // should result in two fields, thingy which is a table and thingy_type for the type enum
            var monster_thingy = monster.AddField("thingy",
                new FlatBuffersType {BaseType = BaseType.Union, EnumDef = any});

            var weapon = _typeBuilder.AddTable("Weapon");
            var weapon_name = weapon.AddField("name", new FlatBuffersType {BaseType = BaseType.String});

            var pickup = _typeBuilder.AddTable("Pickup");
            var pickup_name = pickup.AddField("name", new FlatBuffersType {BaseType = BaseType.String});

            _typeBuilder.Compile();

            var red = color.Values.Lookup("Red");
            var green = color.Values.Lookup("Green");
            var blue = color.Values.Lookup("Blue");
            Assert.AreEqual(0, red.Value);
            Assert.AreEqual(1, green.Value);
            Assert.AreEqual(2, blue.Value);

            Assert.AreEqual(0, vec3_x.Value.offset);
            Assert.AreEqual(4, vec3_y.Value.offset);
            Assert.AreEqual(8, vec3_z.Value.offset);
            Assert.IsTrue(vec3.Fixed);

            Assert.AreEqual(4, monster_pos.Value.offset);
            Assert.AreEqual(6, monster_mana.Value.offset);
            Assert.AreEqual(8, monster_hp.Value.offset);
            Assert.AreEqual(10, monster_name.Value.offset);
            Assert.AreEqual(12, monster_friendly.Value.offset);
            Assert.AreEqual(14, monster_inventory.Value.offset);
            Assert.AreEqual(16, monster_color.Value.offset);
        }

        internal FlatBufferWrapper GetMonsterWrapper(TypeBuilder typeBuilder) {
            var builder = new FlatBufferBuilder(1);
            var builderWrapper = new FlatBufferBuilderWrapper(typeBuilder, builder);
            var monsterName = builderWrapper.CreateString("Fred");
            builderWrapper.StartVector("Monster", "inventory", 2);
            builderWrapper.AddByte(3); //idx 1
            builderWrapper.AddByte(2); //idx 0
            var monsterInventory = builderWrapper.EndVector();
            var minion1_name = builderWrapper.CreateString("Banana");
            var minion2_name = builderWrapper.CreateString("Ananab");
            var main_minion_name = builderWrapper.CreateString("MainMinion");
            builderWrapper.StartTable("Minion");
            builderWrapper.AddString("name", minion1_name);
            var minion1 = builderWrapper.EndTable();
            builderWrapper.StartTable("Minion");
            builderWrapper.AddString("name", minion2_name);
            var minion2 = builderWrapper.EndTable();
            builderWrapper.StartTable("Minion");
            builderWrapper.AddString("name", main_minion_name);
            var mainMinion = builderWrapper.EndTable();
            builderWrapper.StartVector("Monster", "minions", 2);
            builderWrapper.AddTable(minion2); //idx 1
            builderWrapper.AddTable(minion1); //idx 0
            var minions = builderWrapper.EndVector();
            builderWrapper.StartTable("Monster");
            builderWrapper.AddStruct("pos", new object[] {1.0f, 2.0f, 3.0f}); // x, y, z
            builderWrapper.AddShort("mana", 42);
            builderWrapper.AddShort("hp", 17);
            builderWrapper.AddString("name", monsterName);
            builderWrapper.AddBool("friendly", true);
            builderWrapper.AddVector("inventory", monsterInventory);
            builderWrapper.AddByte("color", 2); // Blue
            builderWrapper.AddVector("minions", minions);
            builderWrapper.AddTable("mainMinion", mainMinion);
            builderWrapper.Finish(builderWrapper.EndTable());
            var beginData = builder.DataBuffer().position();
            var countData = builder.DataBuffer().Length - beginData;
            var byteBuffer = new ByteBuffer(builder.DataBuffer().Data.Skip(beginData).Take(countData).ToArray());
            return new FlatBufferWrapper(_typeBuilder, "Monster", byteBuffer);
        }

        [TestMethod]
        public void CreateAndAccessMonster() {
            CreateMonsterFlatBufferTypes();
            var wrapper = GetMonsterWrapper(_typeBuilder);
            var posWrapper = (FlatBufferWrapper) wrapper["pos"];
            Assert.AreEqual(1.0f, (float) posWrapper["x"]);
            Assert.AreEqual(2.0f, (float) posWrapper["y"]);
            Assert.AreEqual(3.0f, (float) posWrapper["z"]);
            Assert.AreEqual(42, (short) wrapper["mana"]);
            Assert.AreEqual(17, (short) wrapper["hp"]);
            Assert.AreEqual("Fred", (string) wrapper["name"]);
            Assert.AreEqual(true, (bool) wrapper["friendly"]);
            //TODO support for vector
            var inventoryAsArray = (byte[]) wrapper["inventory"];
            Assert.AreEqual(2, inventoryAsArray.Length);
            Assert.AreEqual((byte) 2, inventoryAsArray[0]);
            Assert.AreEqual((byte) 3, inventoryAsArray[1]);
            Assert.AreEqual((byte) 2, (byte) wrapper["color"]);
            var minionsAsArray = (FlatBufferWrapper[]) wrapper["minions"];
            Assert.AreEqual("Banana", (string) minionsAsArray[0]["name"]);
            Assert.AreEqual("Ananab", (string) minionsAsArray[1]["name"]);
            Assert.AreEqual("MainMinion", (string) ((FlatBufferWrapper)wrapper["mainMinion"])["name"]);
        }

        [TestMethod]
        public void RoundTripModelThroughSchema() {
            CreateMonsterFlatBufferTypes();
            var schema = _typeBuilder.ToSchema();
            var parser = new Parser();
            parser.Parse(schema);
            var newTypeBuilder = parser.TypeBuilder;
            newTypeBuilder.Compile();
            Assert.AreEqual(newTypeBuilder.Structs.Count, _typeBuilder.Structs.Count);
            Assert.AreEqual(newTypeBuilder.Enums.Count, _typeBuilder.Enums.Count);

            var wrapper = GetMonsterWrapper(newTypeBuilder);
            var posWrapper = (FlatBufferWrapper) wrapper["pos"];
            Assert.AreEqual(1.0f, (float)posWrapper["x"]);
            Assert.AreEqual(2.0f, (float)posWrapper["y"]);
            Assert.AreEqual(3.0f, (float)posWrapper["z"]);
            Assert.AreEqual(42, (short) wrapper["mana"]);
            Assert.AreEqual(17, (short) wrapper["hp"]);
            Assert.AreEqual("Fred", (string)wrapper["name"]);
            Assert.AreEqual(true, (bool) wrapper["friendly"]);
            //TODO support for vector
            var inventoryAsArray = (byte[])wrapper["inventory"];
            Assert.AreEqual(2, inventoryAsArray.Length);
            Assert.AreEqual((byte)2, inventoryAsArray[0]);
            Assert.AreEqual((byte)3, inventoryAsArray[1]);
            Assert.AreEqual((byte)2, (byte)wrapper["color"]);
            var minionsAsArray = (FlatBufferWrapper[]) wrapper["minions"];
            Assert.AreEqual("Banana", (string)minionsAsArray[0]["name"]);
            Assert.AreEqual("Ananab", (string)minionsAsArray[1]["name"]);
        }

        [TestMethod]
        public void Union_Monster() {
            CreateMonsterFlatBufferTypes();
            var builder = new FlatBufferBuilder(1);
            var builderWrapper = new FlatBufferBuilderWrapper(_typeBuilder, builder);
            var monster1_name = builderWrapper.CreateString("Monster1");
            builderWrapper.StartTable("Monster");
            builderWrapper.AddString("name", monster1_name);
            var monster1 = builderWrapper.EndTable();
            var monster2_name = builderWrapper.CreateString("Monster2");
            builderWrapper.StartTable("Monster");
            builderWrapper.AddString("name", monster2_name);
            builderWrapper.AddByte("thingy_type", 1); // Monster
            builderWrapper.AddTable("thingy", monster1);
            builderWrapper.Finish(builderWrapper.EndTable());
            var beginData = builder.DataBuffer().position();
            var countData = builder.DataBuffer().Length - beginData;
            var byteBuffer = new ByteBuffer(builder.DataBuffer().Data.Skip(beginData).Take(countData).ToArray());
            var wrapper = new FlatBufferWrapper(_typeBuilder, "Monster", byteBuffer);

            Assert.AreEqual("Monster2", (string) wrapper["name"]);
            Assert.AreEqual(1, (byte) wrapper["thingy_type"]);
            var thingy_wrapper = (FlatBufferWrapper) wrapper["thingy"];
            Assert.IsNotNull(thingy_wrapper);
            Assert.AreEqual("Monster1", (string) thingy_wrapper["name"]);
            Assert.AreEqual(0, (byte) thingy_wrapper["thingy_type"]); //None - not set!
        }


        [TestMethod]
        public void Union_Weapon() {
            CreateMonsterFlatBufferTypes();
            var builder = new FlatBufferBuilder(1);
            var builderWrapper = new FlatBufferBuilderWrapper(_typeBuilder, builder);
            var weapon_name = builderWrapper.CreateString("Weapon");
            builderWrapper.StartTable("Weapon");
            builderWrapper.AddString("name", weapon_name);
            var weapon = builderWrapper.EndTable();
            var monster_name = builderWrapper.CreateString("Monster2");
            builderWrapper.StartTable("Monster");
            builderWrapper.AddByte("thingy_type", 2); // Weapon
            builderWrapper.AddTable("thingy", weapon);
            builderWrapper.AddString("name", monster_name);
            builderWrapper.Finish(builderWrapper.EndTable());
            var beginData = builder.DataBuffer().position();
            var countData = builder.DataBuffer().Length - beginData;
            var byteBuffer = new ByteBuffer(builder.DataBuffer().Data.Skip(beginData).Take(countData).ToArray());
            var wrapper = new FlatBufferWrapper(_typeBuilder, "Monster", byteBuffer);

            Assert.AreEqual("Monster2", (string) wrapper["name"]);
            Assert.AreEqual(2, (byte) wrapper["thingy_type"]);
            var thingy_wrapper = (FlatBufferWrapper) wrapper["thingy"];
            Assert.IsNotNull(thingy_wrapper);
            Assert.AreEqual("Weapon", (string) thingy_wrapper["name"]);
        }

        [TestMethod]
        public void Union_Pickup() {
            CreateMonsterFlatBufferTypes();
            var builder = new FlatBufferBuilder(1);
            var builderWrapper = new FlatBufferBuilderWrapper(_typeBuilder, builder);
            var pickup_name = builderWrapper.CreateString("Pickup");
            builderWrapper.StartTable("Pickup");
            builderWrapper.AddString("name", pickup_name);
            var pickup = builderWrapper.EndTable();
            var monster_name = builderWrapper.CreateString("Monster2");
            builderWrapper.StartTable("Monster");
            builderWrapper.AddByte("thingy_type", 2); // Pickup
            builderWrapper.AddTable("thingy", pickup);
            builderWrapper.AddString("name", monster_name);
            builderWrapper.Finish(builderWrapper.EndTable());
            var beginData = builder.DataBuffer().position();
            var countData = builder.DataBuffer().Length - beginData;
            var byteBuffer = new ByteBuffer(builder.DataBuffer().Data.Skip(beginData).Take(countData).ToArray());
            var wrapper = new FlatBufferWrapper(_typeBuilder, "Monster", byteBuffer);

            Assert.AreEqual("Monster2", (string) wrapper["name"]);
            Assert.AreEqual(2, (byte) wrapper["thingy_type"]);
            var thingy_wrapper = (FlatBufferWrapper) wrapper["thingy"];
            Assert.IsNotNull(thingy_wrapper);
            Assert.AreEqual("Pickup", (string) thingy_wrapper["name"]);
        }

        [TestMethod]
        public void NullableStruct() {
            var typebuilder = new TypeBuilder();
            var twoIntStruct = typebuilder.AddStruct("TwoInt");
            twoIntStruct.AddField("a", new FlatBuffersType(BaseType.Int));
            twoIntStruct.AddField("b", new FlatBuffersType(BaseType.Int));

            var option = typebuilder.AddTable("NullableTwoInt");
            option.AddField("Some", new FlatBuffersType(BaseType.Struct, twoIntStruct));

            typebuilder.Compile();
            
            var builder = new FlatBufferBuilder(1);
            var builderWrapper = new FlatBufferBuilderWrapper(typebuilder, builder);
                        
            builderWrapper.StartTable("NullableTwoInt");
            //builderWrapper.AddStruct("Some", new object[] { 5, 10 });
            //builderWrapper.("Some", new object[0]);
            builderWrapper.Finish(builderWrapper.EndTable());

            var beginData = builder.DataBuffer().position();
            var countData = builder.DataBuffer().Length - beginData;
            var byteBuffer = new ByteBuffer(builder.DataBuffer().Data.Skip(beginData).Take(countData).ToArray());
            var wrapper = new FlatBufferWrapper(typebuilder, "NullableTwoInt", byteBuffer);

            var wr = (FlatBufferWrapper) wrapper["Some"];
            Assert.AreEqual(null, wr);

        }
    }
}
