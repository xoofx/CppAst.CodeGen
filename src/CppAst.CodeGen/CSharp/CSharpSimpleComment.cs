// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpSimpleComment : CSharpComment
    {
        public override void DumpTo(CodeWriter writer)
        {
            writer.PushPrefix("// ");
            DumpChildrenTo(writer);
            writer.PopPrefix();
        }
    }
}