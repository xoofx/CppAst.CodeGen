// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpFreeMember: CSharpElement
    {
        public string Text { get; set; } = string.Empty;

        public override void DumpTo(CodeWriter writer)
        {
            writer.WriteLine(Text);
        }
    }
}