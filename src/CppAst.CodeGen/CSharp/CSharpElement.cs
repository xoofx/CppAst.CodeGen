﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;
using Zio.FileSystems;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpElement : ICSharpElement
    {
        public static readonly CSharpElement Empty = new CSharpEmptyElement();

        public CppElement? CppElement { get; set; }

        public CSharpElement? Parent { get; set; }

        public abstract void DumpTo(CodeWriter writer);

        /// <inheritdoc />
        public override string ToString()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Simple));
            DumpTo(writer);
            return writer.CurrentWriter!.ToString()!;
        }

        public string ToFullString()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Full));
            DumpTo(writer);
            return writer.CurrentWriter!.ToString()!;
        }

        private sealed class CSharpEmptyElement : CSharpElement
        {
            public override void DumpTo(CodeWriter writer)
            {
            }
        }
    }
}