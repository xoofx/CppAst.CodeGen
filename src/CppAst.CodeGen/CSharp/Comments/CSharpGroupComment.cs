// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    /// <summary>
    /// A group of comments
    /// </summary>
    public class CSharpGroupComment : CSharpComment
    {
        public CSharpGroupComment()
        {
        }

        public override void DumpTo(CodeWriter writer)
        {
            DumpChildrenTo(writer);
        }
    }
}