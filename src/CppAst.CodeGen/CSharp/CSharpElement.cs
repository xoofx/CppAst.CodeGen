// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;
using Zio.FileSystems;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpElement : ICSharpElement
    {
        public CppElement CppElement { get; internal set; }

        public CSharpElement Parent { get; internal set; }

        public abstract void DumpTo(CodeWriter writer);

        public override string ToString()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Simple));
            DumpTo(writer);
            return writer.CurrentWriter.ToString();
        }

        public string ToFullString()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Full));
            DumpTo(writer);
            return writer.CurrentWriter.ToString();
        }
    }
}