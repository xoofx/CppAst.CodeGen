// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpComment : CSharpElement
    {
        protected CSharpComment()
        {
            Children = new List<CSharpComment>();
        }

        public List<CSharpComment> Children { get; }

        protected internal void DumpChildrenTo(CodeWriter writer)
        {
            foreach (var children in Children)
            {
                children.DumpTo(writer);
            }
        }
    }
}