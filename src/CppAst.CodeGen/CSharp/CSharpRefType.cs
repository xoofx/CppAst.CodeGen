// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpRefType : CSharpTypeWithElement
    {
        public CSharpRefType(CSharpRefKind refKind, CSharpType elementType) : base(elementType)
        {
            Kind = refKind;
        }

        public CSharpRefKind Kind { get; set; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            Kind.DumpTo(writer);
            ElementType?.DumpReferenceTo(writer);
        }
    }
}