﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CSharpTypeWithMembers : CSharpNamedType, ICSharpWithComment, ICSharpContainer, ICSharpAttributesProvider, ICSharpElementWithVisibility
    {
        protected CSharpTypeWithMembers(string name) : base(name)
        {
            Attributes = new List<CSharpAttribute>();
            Members = new CSharpContainerList<CSharpElement>(this);
            BaseTypes = new List<CSharpType>();
            Visibility = CSharpVisibility.Public;
            PrimaryConstructorParameters = new List<CSharpParameter>();
        }

        public CSharpComment? Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        /// <inheritdoc />
        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public List<CSharpType> BaseTypes { get; }

        /// <summary>
        /// Gets or sets a boolean indicating if this type is a record.
        /// </summary>
        public bool IsRecord { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating if this type should have primary constructor parameters.
        /// </summary>
        public bool ForcePrimaryConstructorParameters { get; set; }

        /// <summary>
        /// Record Parameters if <see cref="IsRecord"/> is true or <see cref="ForcePrimaryConstructorParameters"/> is true.
        /// </summary>
        public List<CSharpParameter> PrimaryConstructorParameters { get; }

        /// <inheritdoc />
        public CSharpContainerList<CSharpElement> Members { get; }

        /// <inheritdoc />
        ICSharpContainer? ICSharpContainer.Parent => Parent as ICSharpContainer;

        /// <inheritdoc />
        public void ValidateMember(CSharpElement element)
        {
            if (element is CSharpCompilation || element is CSharpGeneratedFile || element is CSharpNamespace)
            {
                throw new ArgumentException($"Cannot add a {element.GetType().Name} to members of a {GetType().Name}");
            }
        }

        /// <inheritdoc />
        public virtual IEnumerable<CSharpAttribute> GetAttributes() => Attributes;

        protected abstract string DeclarationKind { get; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            var mode = writer.Mode;
            if (mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            Visibility.DumpTo(writer);
            Modifiers.DumpTo(writer);

            if (IsRecord)
            {
                writer.Write("record ");
            }
            writer.Write(DeclarationKind);
            writer.Write(" ");

            writer.Write(Name);

            if (ForcePrimaryConstructorParameters || (IsRecord && PrimaryConstructorParameters.Count > 0))
            {
                writer.Write("(");
                for (var i = 0; i < PrimaryConstructorParameters.Count; i++)
                {
                    var parameter = PrimaryConstructorParameters[i];
                    if (i > 0) writer.Write(", ");
                    parameter.DumpTo(writer);
                }
                writer.Write(")");
            }

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

            if (IsRecord && Members.Count == 0)
            {
                writer.WriteLine(";");
            }
            else
            {
                writer.WriteLine();

                if (mode == CodeWriterMode.Full)
                {
                    writer.OpenBraceBlock();
                    this.DumpMembersTo(writer);
                    writer.CloseBraceBlock();
                }
                else
                {
                    writer.WriteLine(" { ... }");
                }
            }
        }
    }
}
