// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpTypeWithElement : CSharpType
    {
        protected CSharpTypeWithElement(CSharpType elementType)
        {
            ElementType = elementType;
        }

        public CSharpType ElementType { get; set; }

        /// <inheritdoc />
        public override void DumpReferenceTo(CodeWriter writer)
        {
            DumpTo(writer);
        }
    }
}