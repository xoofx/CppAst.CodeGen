// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp;

public class CSharpGenericTypeReference : CSharpType
{
    public CSharpGenericTypeReference(string name, CSharpType[] typeArguments)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TypeArguments = typeArguments ?? throw new ArgumentNullException(nameof(typeArguments));
    }

    public string Name { get; set; }

    public CSharpType[] TypeArguments { get; set; }

    /// <inheritdoc />
    public override void DumpTo(CodeWriter writer)
    {
        writer.Write(Name);
        writer.Write("<");
        for (var i = 0; i < TypeArguments.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(", ");
            }

            TypeArguments[i].DumpReferenceTo(writer);
        }

        writer.Write(">");
    }

    /// <inheritdoc />
    public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
}