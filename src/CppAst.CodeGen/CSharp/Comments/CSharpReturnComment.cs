// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpReturnComment : CSharpComment
    {
        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write($"<returns>{Environment.NewLine}");
            DumpChildrenTo(writer);
            writer.WriteLine($"{Environment.NewLine}</returns>");
        }
    }
}