using System;
using CppAst.CodeGen.Common;
using NUnit.Framework;
using Zio.FileSystems;

namespace CppAst.CodeGen.Tests
{
    public class CodeWriterTests
    {
        private CodeWriter GetNewCodeWriter()
        {
            return new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Full) { NewLine = "\n" });
        }

        [Test]
        public void TestSimple()
        {
            var codeWriter = GetNewCodeWriter();

            codeWriter.Write("a");
            codeWriter.Write("b");
            Assert.AreEqual("ab", codeWriter.ToString());

            codeWriter.WriteLine("c");

            Assert.AreEqual("abc\n", codeWriter.ToString());

            codeWriter.WriteLine("d");

            Assert.AreEqual("abc\nd\n", codeWriter.ToString());

            codeWriter.Write("e\r\n");

            Assert.AreEqual("abc\nd\ne\n", codeWriter.ToString());

            codeWriter.Write("f\r\ng\n");

            Assert.AreEqual("abc\nd\ne\nf\ng\n", codeWriter.ToString());

            codeWriter.Write("h\ni");

            Assert.AreEqual("abc\nd\ne\nf\ng\nh\ni", codeWriter.ToString());
        }

        [Test]
        public void TestSimpleWithIndent()
        {
            var codeWriter = GetNewCodeWriter();

            codeWriter.Indent();

            codeWriter.Write("a");
            codeWriter.Write("b");
            Assert.AreEqual("    ab", codeWriter.ToString());

            codeWriter.WriteLine("c");

            Assert.AreEqual("    abc\n", codeWriter.ToString());

            codeWriter.WriteLine("d");

            Assert.AreEqual("    abc\n    d\n", codeWriter.ToString());

            codeWriter.Write("e\r\n");

            Assert.AreEqual("    abc\n    d\n    e\n", codeWriter.ToString());

            codeWriter.Write("f\r\ng\n");

            Assert.AreEqual("    abc\n    d\n    e\n    f\n    g\n", codeWriter.ToString());

            codeWriter.Write("h\ni");

            Assert.AreEqual("    abc\n    d\n    e\n    f\n    g\n    h\n    i", codeWriter.ToString());
        }

        [Test]
        public void TestSimpleWithPrefix()
        {
            var codeWriter = GetNewCodeWriter();

            codeWriter.PushPrefix("// ");

            codeWriter.Write("a");
            codeWriter.Write("b");
            Assert.AreEqual("// ab", codeWriter.ToString());

            codeWriter.WriteLine("c");

            Assert.AreEqual("// abc\n", codeWriter.ToString());

            codeWriter.WriteLine("d");

            Assert.AreEqual("// abc\n// d\n", codeWriter.ToString());

            codeWriter.Write("e\r\n");

            Assert.AreEqual("// abc\n// d\n// e\n", codeWriter.ToString());

            codeWriter.Write("f\r\ng\n");

            Assert.AreEqual("// abc\n// d\n// e\n// f\n// g\n", codeWriter.ToString());

            codeWriter.Write("h\ni");

            Assert.AreEqual("// abc\n// d\n// e\n// f\n// g\n// h\n// i", codeWriter.ToString());
        }

        [Test]
        public void TestSimpleWithIndentAndPrefix()
        {
            var codeWriter = GetNewCodeWriter();

            codeWriter.Indent();
            codeWriter.PushPrefix("// ");

            codeWriter.Write("a");
            codeWriter.Write("b");
            Assert.AreEqual("    // ab", codeWriter.ToString());

            codeWriter.WriteLine("c");

            Assert.AreEqual("    // abc\n", codeWriter.ToString());

            codeWriter.WriteLine("d");

            Assert.AreEqual("    // abc\n    // d\n", codeWriter.ToString());

            codeWriter.Write("e\r\n");

            Assert.AreEqual("    // abc\n    // d\n    // e\n", codeWriter.ToString());

            codeWriter.Write("f\r\ng\n");

            Assert.AreEqual("    // abc\n    // d\n    // e\n    // f\n    // g\n", codeWriter.ToString());

            codeWriter.Write("h\ni");

            Assert.AreEqual("    // abc\n    // d\n    // e\n    // f\n    // g\n    // h\n    // i", codeWriter.ToString());
        }

        [Test]
        public void TestIndents()
        {
            var codeWriter = GetNewCodeWriter();
            Assert.Throws<InvalidOperationException>(() => codeWriter.UnIndent());

            codeWriter.Indent();
            codeWriter.Indent();
            codeWriter.Write("a");
            Assert.AreEqual("        a", codeWriter.ToString());
            codeWriter.WriteLine();
            Assert.AreEqual("        a\n", codeWriter.ToString());
            codeWriter.UnIndent();
            codeWriter.WriteLine("b");
            Assert.AreEqual("        a\n    b\n", codeWriter.ToString());
        }

        [Test]
        public void TestPrefixes()
        {
            var codeWriter = GetNewCodeWriter();
            Assert.Throws<InvalidOperationException>(() => codeWriter.UnIndent());

            codeWriter.PushPrefix("    //");
            codeWriter.PushPrefix("/ ");
            codeWriter.Write("a");
            Assert.AreEqual("    /// a", codeWriter.ToString());
            codeWriter.WriteLine();
            Assert.AreEqual("    /// a\n", codeWriter.ToString());
            codeWriter.PopPrefix();
            codeWriter.WriteLine("b");
            Assert.AreEqual("    /// a\n    //b\n", codeWriter.ToString());
        }
    }
}