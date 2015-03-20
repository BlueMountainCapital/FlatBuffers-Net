using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlatBuffers.Test {
    [TestClass]
    public class SerializationTest {
        public static MemoryStream SerializeToStream(object o) {
            var stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            return stream;
        }

        public static object DeserializeFromStream(MemoryStream stream) {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            object o = formatter.Deserialize(stream);
            return o;
        }

        public static T RoundTripper<T>(T obj) {
            return (T)DeserializeFromStream(SerializeToStream(obj));
        }

        [TestMethod]
        public void FlatBufferWrapperIsSerializable() {
            var rtm = new RuntimeTypeModelTest();
            rtm.CreateMonsterFlatBufferTypes();
            var wrapper = rtm.GetMonsterWrapper(rtm._typeBuilder);
            var cloned = RoundTripper(wrapper);

            var posWrapper = (FlatBufferWrapper)cloned["pos"];
            Assert.AreEqual(1.0f, (float)posWrapper["x"]);
            Assert.AreEqual(2.0f, (float)posWrapper["y"]);
            Assert.AreEqual(3.0f, (float)posWrapper["z"]);
            Assert.AreEqual(42, (short)cloned["mana"]);
            Assert.AreEqual(17, (short)cloned["hp"]);
            Assert.AreEqual("Fred", (string)cloned["name"]);
            Assert.AreEqual(true, (bool)cloned["friendly"]);
            //TODO support for vector
            var inventoryAsArray = (byte[])cloned["inventory"];
            Assert.AreEqual(2, inventoryAsArray.Length);
            Assert.AreEqual((byte)2, inventoryAsArray[0]);
            Assert.AreEqual((byte)3, inventoryAsArray[1]);
            Assert.AreEqual((byte)2, (byte)cloned["color"]);
            var minionsAsArray = (FlatBufferWrapper[])cloned["minions"];
            Assert.AreEqual("Banana", (string)minionsAsArray[0]["name"]);
            Assert.AreEqual("Ananab", (string)minionsAsArray[1]["name"]);
        }
    }
}
