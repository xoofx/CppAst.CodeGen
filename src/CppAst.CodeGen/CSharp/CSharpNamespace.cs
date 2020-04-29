// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpNamespace : CSharpElement, ICSharpContainer
    {
        public CSharpNamespace(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Members = new CSharpContainerList<CSharpElement>(this);
        }

        public string Name { get; set; }

        /// <inheritdoc />
        public CSharpContainerList<CSharpElement> Members { get; }

        /// <inheritdoc />
        ICSharpContainer ICSharpContainer.Parent => Parent as ICSharpContainer;

        /// <inheritdoc />
        public void ValidateMember(CSharpElement element)
        {
            if (element is CSharpCompilation || element is CSharpGeneratedFile)
            {
                throw new ArgumentException($"Cannot add a {element.GetType().Name} to members of a {nameof(CSharpNamespace)}");
            }
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write("namespace ");
            writer.Write(Name);
            if (writer.Mode == CodeWriterMode.Full)
            {
                writer.WriteLine();
                writer.OpenBraceBlock();
                {
                    this.DumpMembersTo(writer);
                }
                writer.CloseBraceBlock();
            }
            else
            {
                writer.WriteLine(" { ... }");
            }
        }
    }
}