// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp;

/// <summary>
/// Represents a generic parameter type.
/// </summary>
public class CSharpGenericParameterType : CSharpNamedType
{
    public CSharpGenericParameterType(string name) : base(name)
    {
        WhereClauses = new();
    }

    public bool IsOut { get; set; }
    
    public List<CSharpWhereClause> WhereClauses { get; }

    public void DumpWhereClausesTo(CodeWriter writer)
    {
        if (WhereClauses.Count > 0)
        {
            writer.Write(" where ");
            writer.Write(Name);
            writer.Write(" : ");
            for (int i = 0; i < WhereClauses.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }
                WhereClauses[i].DumpTo(writer);
            }
        }
    }
    
    public override void DumpTo(CodeWriter writer)
    {
        if (IsOut)
        {
            writer.Write("out ");
        }
        writer.Write(Name);
    }
}

public class CSharpWhereClause : CSharpElement
{
    public CSharpWhereClause(string constraint)
    {
        Constraint = constraint;
    }

    public CSharpWhereClause(CSharpType type)
    {
        Type = type;
    }

    public string? Constraint { get; set; }

    public CSharpType? Type { get; set; }

    public override void DumpTo(CodeWriter writer)
    {
        if (Constraint != null)
        {
            writer.Write(Constraint);
        }
        else if (Type != null)
        {
            Type.DumpReferenceTo(writer);
        }
    }
}