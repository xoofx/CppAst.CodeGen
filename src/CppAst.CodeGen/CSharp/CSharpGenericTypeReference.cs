// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp;

public class CSharpGenericTypeReference : CSharpType
{
    public CSharpGenericTypeReference(CSharpType baseType)
    {
        BaseType = baseType;
        TypeArguments = new();
    }

    public CSharpGenericTypeReference(CSharpType baseType, params CSharpType[] typeArguments) : this(baseType)
    {
        TypeArguments.AddRange(typeArguments);
    }

    public CSharpGenericTypeReference(string name) : this(new CSharpFreeType(name))
    {
    }

    public CSharpGenericTypeReference(string name, params CSharpType[] typeArguments) : this(new CSharpFreeType(name))
    {
        TypeArguments.AddRange(typeArguments);
    }

    public CSharpType BaseType { get; set; }

    public List<CSharpType> TypeArguments { get; }

    /// <inheritdoc />
    public override void DumpTo(CodeWriter writer)
    {
        BaseType.DumpReferenceTo(writer);

        // Allows to have a GenericTypeReference that don't have type arguments
        if (TypeArguments.Count > 0)
        {
            writer.Write("<");
            for (var i = 0; i < TypeArguments.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }

                TypeArguments[i].DumpReferenceTo(writer);
            }

            writer.Write(">");
        }
    }

    /// <inheritdoc />
    public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
}