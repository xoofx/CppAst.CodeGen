// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpTypeWithMembers : CSharpNamedType, ICSharpContainer, ICSharpAttributesProvider, ICSharpElementWithVisibility
    {
        protected CSharpTypeWithMembers(string name) : base(name)
        {
            Attributes = new List<CSharpAttribute>();
            Members = new CSharpContainerList<CSharpElement>(this);
            BaseTypes = new List<CSharpType>();
            Visibility = CSharpVisibility.Public;
        }

        public CSharpComment Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public List<CSharpType> BaseTypes { get; }

        public CSharpContainerList<CSharpElement> Members { get; }

        ICSharpContainer ICSharpContainer.Parent => Parent as ICSharpContainer;

        public void ValidateMember(CSharpElement element)
        {
            if (element is CSharpCompilation || element is CSharpGeneratedFile || element is CSharpNamespace)
            {
                throw new ArgumentException($"Cannot add a {element.GetType().Name} to members of a {this.GetType().Name}");
            }
        }

        public virtual IEnumerable<CSharpAttribute> GetAttributes()
        {
            return Attributes;
        }

        protected abstract string DeclarationKind { get; }

        public override void DumpTo(CodeWriter writer)
        {
            var mode = writer.Mode;
            if (mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            Visibility.DumpTo(writer);
            Modifiers.DumpTo(writer);

            writer.Write(DeclarationKind);
            writer.Write(" ");

            writer.Write(Name);

            if (BaseTypes.Count > 0)
            {
                writer.Write(" : ");
                for (var i = 0; i < BaseTypes.Count; i++)
                {
                    var baseType = BaseTypes[i];
                    if (i > 0) writer.Write(", ");
                    baseType.DumpReferenceTo(writer);
                }
            }
            writer.WriteLine();

            if (mode == CodeWriterMode.Full)
            {
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