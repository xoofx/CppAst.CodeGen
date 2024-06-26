﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;
using Zio.FileSystems;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpType : CSharpElement
    {
        public abstract void DumpReferenceTo(CodeWriter writer);
        
        public string GetName()
        {
            var writer = new CodeWriter(new CodeWriterOptions(new MemoryFileSystem(), CodeWriterMode.Simple));
            DumpReferenceTo(writer);
            return writer.CurrentWriter!.ToString()!;
        }
    }
}