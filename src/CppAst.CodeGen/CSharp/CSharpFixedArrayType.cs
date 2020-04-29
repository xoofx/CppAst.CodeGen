// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpFixedArrayType : CSharpTypeWithElement
    {
        public CSharpFixedArrayType(CSharpType elementType, int size) : base(elementType)
        {
            Size = size;
        }

        public int Size { get; set; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            ElementType?.DumpReferenceTo(writer);
            writer.Write($"[{Size}]");
        }
    }
}