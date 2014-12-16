using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlatBuffers;

namespace FlatBuffers.Test
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void SkipWhitespace() {
            var parser = new Parser();
            Assert.AreEqual(0, parser.SkipWhitespace(0, ""));
            Assert.AreEqual(0, parser.SkipWhitespace(0, ";"));
            Assert.AreEqual(1, parser.SkipWhitespace(0, " ;"));
            Assert.AreEqual(3, parser.SkipWhitespace(1, ";\t ;"));
        }

        public void Throws<T>(Action a) where T : Exception {
            bool caught = false;
            try {
                a();
            }
            catch (T) {
                caught = true;
            }
            Assert.IsTrue(caught);
        }

        [TestMethod]
        public void Consume() {
            var parser = new Parser();
            var schemaStr = "include but ignore rest";
            Assert.AreEqual(7, parser.Consume("include", 0, schemaStr));
            Assert.AreEqual(11, parser.Consume("but", 8, schemaStr));
            Throws<Exception>(() => parser.Consume("ignore", 8, schemaStr));
            Throws<Exception>(() => parser.Consume("resting", 19, schemaStr));
            Assert.AreEqual(schemaStr.Length, parser.Consume("rest", 19, schemaStr));
        }

        [TestMethod]
        public void ParseIdentifier() {
            var parser = new Parser();
            string identifier;
            Assert.AreEqual(3, parser.ParseIdentifier(0, "foo ", out identifier));
            Assert.AreEqual("foo", identifier);
            Assert.AreEqual(7, parser.ParseIdentifier(4, "foo bar ", out identifier));
            Assert.AreEqual("bar", identifier);
            Assert.AreEqual(7, parser.ParseIdentifier(4, "foo bar;", out identifier));
            Assert.AreEqual("bar", identifier);
            Throws<Exception>(() => parser.ParseIdentifier(0, "9bar ", out identifier));
            Throws<Exception>(() => parser.ParseIdentifier(0, ";bar ", out identifier));
            Throws<Exception>(() => parser.ParseIdentifier(0, " bar ", out identifier));
        }

        [TestMethod]
        public void ParseStringConstant() {
            var parser = new Parser();
            string stringConstant;
            Assert.AreEqual(5, parser.ParseStringConstant(0, "\"foo\"", out stringConstant));
            Assert.AreEqual("foo", stringConstant);
            Assert.AreEqual(7, parser.ParseStringConstant(0, "\"\\\"foo\"", out stringConstant));
            Assert.AreEqual("\"foo", stringConstant);
            Throws<Exception>(() => parser.ParseStringConstant(0, "\"foo", out stringConstant));
        }

        [TestMethod]
        public void ParseInclude() {
            var parser = new Parser();
            string identifier;
            var schemaStr = "include \"foo\";";
            Assert.AreEqual(schemaStr.Length, parser.ParseInclude(0, schemaStr, out identifier));
            Assert.AreEqual("foo", identifier);
            schemaStr = "  \t\ninclude \"foo\";\r\ninclude \"bar\"; include \"bad; ";
            Assert.AreEqual(18, parser.ParseInclude(0, schemaStr, out identifier));
            Assert.AreEqual("foo", identifier);
            Assert.AreEqual(34, parser.ParseInclude(18, schemaStr, out identifier));
            Assert.AreEqual("bar", identifier);
            Throws<Exception>(() => parser.ParseInclude(30, schemaStr, out identifier));
        }

        [TestMethod]
        public void ParseIncludeStar() {
            var parser = new Parser();
            Assert.AreEqual(0, parser.ParseIncludeStar(0, ""));
            Assert.AreEqual(2, parser.ParseIncludeStar(0, "  table"));
            var schemaStr = "include \"foo\";";
            Assert.AreEqual(schemaStr.Length, parser.ParseIncludeStar(0, schemaStr));
            schemaStr = "include \"foo\"; include \"bar\";";
            Assert.AreEqual(schemaStr.Length, parser.ParseIncludeStar(0, schemaStr));
            var expectedEnd = schemaStr.Length + 1; // add one for whitespace!
            schemaStr = "include \"foo\"; include \"bar\"; table";
            Assert.AreEqual(expectedEnd, parser.ParseIncludeStar(0, schemaStr));
        }

        [TestMethod]
        public void ParseNamespaceDecl() {
            var parser = new Parser();
            parser.ParseNamespaceDecl(0, "namespace foo;");
            Assert.AreEqual(1, parser.TypeBuilder.Namespaces.Count);
            var namespace_ = parser.TypeBuilder.Namespaces[0];
            Assert.AreEqual(1, namespace_.Components.Count);
            Assert.AreEqual("foo", namespace_.Components[0]);

            parser = new Parser();
            parser.ParseNamespaceDecl(0, "namespace foo.bar;");
            Assert.AreEqual(1, parser.TypeBuilder.Namespaces.Count);
            namespace_ = parser.TypeBuilder.Namespaces[0];
            Assert.AreEqual(2, namespace_.Components.Count);
            Assert.AreEqual("foo", namespace_.Components[0]);
            Assert.AreEqual("bar", namespace_.Components[1]);
        }

        [TestMethod]
        public void ParseAttributeDecl() {
            var parser = new Parser();
            var schemaStr = "attribute \"foo\";";
            Assert.AreEqual(schemaStr.Length, parser.ParseAttributeDecl(0, schemaStr));
            Assert.AreEqual("foo", parser.TypeBuilder.Attribute);
        }

        [TestMethod]
        public void ParseTableDecl() {
            var parser = new Parser();
            var schemaStr = "table foo { bar : int; } ";
            Assert.AreEqual(schemaStr.Length - 1, parser.ParseDecl(0, schemaStr));
            Assert.AreEqual(1, parser.TypeBuilder.Structs.Count);
            var tableDef = parser.TypeBuilder.Structs.Lookup("foo");
            Assert.IsNotNull(tableDef);
            Assert.AreEqual("foo", tableDef.Name);
            Assert.AreEqual(1, tableDef.Fields.Count);
            var fieldDef = tableDef.Fields.Lookup("bar");
            Assert.IsNotNull(fieldDef);
            Assert.AreEqual("bar", fieldDef.Name);
            Assert.AreEqual(BaseType.Int, fieldDef.Value.type.BaseType);
        }

        [TestMethod]
        public void ParseEnumDecl() {
            var parser = new Parser();
            var schemaStr = "enum foo { bar = 1, baz, buzz = 3 }";
            Assert.AreEqual(schemaStr.Length, parser.ParseDecl(0, schemaStr));
            Assert.AreEqual(1, parser.TypeBuilder.Enums.Count);
            var enumDef = parser.TypeBuilder.Enums.Lookup("foo");
            Assert.IsNotNull(enumDef);
            Assert.AreEqual("foo", enumDef.Name);
            Assert.AreEqual(BaseType.Int, enumDef.UnderlyingType.BaseType);
            Assert.AreEqual(3, enumDef.Values.Count);
            var enumVal = enumDef.Values.Lookup("bar");
            Assert.IsNotNull(enumVal);
            Assert.AreEqual("bar", enumVal.Name);
            Assert.AreEqual(1, enumVal.Value);
            enumVal = enumDef.Values.Lookup("baz");
            Assert.IsNotNull(enumVal);
            Assert.AreEqual("baz", enumVal.Name);
            Assert.IsNull(enumVal.Value); // won't be computed till compile processing
            enumVal = enumDef.Values.Lookup("buzz");
            Assert.IsNotNull(enumVal);
            Assert.AreEqual("buzz", enumVal.Name);
            Assert.AreEqual(3, enumVal.Value);
        }

        //TODO Many Many more tests.
    }
}
