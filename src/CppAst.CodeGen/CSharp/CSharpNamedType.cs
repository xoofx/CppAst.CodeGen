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

        public string Name { get; set; }

        public override void DumpReferenceTo(CodeWriter writer)
        {
            writer.Write(Name);
        }
    }
}