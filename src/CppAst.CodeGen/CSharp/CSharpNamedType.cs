// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpNamedType : CSharpType, ICSharpMember
    {
        protected CSharpNamedType(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public override void DumpReferenceTo(CodeWriter writer)
        {
            if (Parent is CSharpNamedType namedType)
            {
                namedType.DumpReferenceTo(writer);
                writer.Write(".");
            }
            writer.Write(Name);
        }
    }
}