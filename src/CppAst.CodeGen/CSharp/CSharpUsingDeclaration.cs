// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpUsingDeclaration : CSharpElement
    {
        public CSharpUsingDeclaration(string reference)
        {
            Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        }

        public bool IsStatic { get; set; }

        public string Alias { get; set; }

        public string Reference { get; set; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write("using ");
            if (IsStatic)
            {
                writer.Write("static ");
            }

            if (Alias != null)
            {
                writer.Write(Alias);
                writer.Write(" = ");
            }

            writer.Write(Reference);
            writer.Write(";");
            writer.WriteLine();
        }
    }
}