// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp;

/// <summary>
/// Generates C# 14.0 extensions.
/// </summary>
public class CSharpExtension : CSharpElement, ICSharpWithComment, ICSharpContainer
{
    public CSharpExtension(CSharpParameter thisParameter)
    {
        Members = new CSharpContainerList<CSharpElement>(this);
        ThisParameter = thisParameter ?? throw new ArgumentNullException(nameof(thisParameter));
    }

    public CSharpComment? Comment { get; set; }

    public CSharpParameter ThisParameter { get; set; }

    /// <summary>
    /// The generic parameters of this type.
    /// </summary>
    public List<CSharpGenericParameterType> GenericParameters { get; } = new List<CSharpGenericParameterType>();

    /// <inheritdoc />
    public CSharpContainerList<CSharpElement> Members { get; }

    /// <inheritdoc />
    ICSharpContainer? ICSharpContainer.Parent => Parent as ICSharpContainer;

    /// <inheritdoc />
    public void ValidateMember(CSharpElement element)
    {
        if (!(element is CSharpMethod || element is CSharpProperty))
        {
            throw new ArgumentException($"Cannot add a {element.GetType().Name} to members of a {GetType().Name}");
        }
    }
    /// <inheritdoc />
    public override void DumpTo(CodeWriter writer)
    {
        var mode = writer.Mode;
        if (mode == CodeWriterMode.Full) Comment?.DumpTo(writer);

        writer.Write("extension");
        if (GenericParameters.Count > 0)
        {
            writer.Write("<");
            for (var i = 0; i < GenericParameters.Count; i++)
            {
                var genericParameter = GenericParameters[i];
                if (i > 0) writer.Write(", ");
                genericParameter.DumpReferenceTo(writer);
            }
            writer.Write(">");
        }
        writer.Write("(");
        ThisParameter.DumpTo(writer);
        writer.Write(")");

        // Write where clauses
        if (GenericParameters.Count > 0)
        {
            foreach (var genericParameter in GenericParameters)
            {
                genericParameter.DumpWhereClausesTo(writer);
            }
        }

        if (mode == CodeWriterMode.Full)
        {
            writer.WriteLine();
            writer.OpenBraceBlock();
            this.DumpMembersTo(writer);
            writer.CloseBraceBlock();
        }
    }
}
