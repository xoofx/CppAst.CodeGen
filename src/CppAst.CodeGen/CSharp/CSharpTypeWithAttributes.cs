// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpTypeWithAttributes : CSharpTypeWithElement, ICSharpContextualAttributesProvider
    {
        public CSharpTypeWithAttributes(CSharpType elementType) : base(elementType)
        {
            Attributes = new List<CSharpAttribute>();
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            ElementType.DumpTo(writer);
        }

        public List<CSharpAttribute> Attributes { get; }

        /// <inheritdoc />
        public IEnumerable<CSharpAttribute> GetContextualAttributes()
        {
            return Attributes;
        }
    }
}