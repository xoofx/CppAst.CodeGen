﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpProperty : CSharpElement, ICSharpWithComment, ICSharpAttributesProvider, ICSharpElementWithVisibility, ICSharpMember
    {
        public CSharpProperty(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Attributes = new List<CSharpAttribute>();
            AttributesForGet = new List<CSharpAttribute>();
            AttributesForSet = new List<CSharpAttribute>();
        }

        /// <inheritdoc />
        public CSharpComment? Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        /// <inheritdoc />
        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public CSharpType? ReturnType { get; set; }

        public List<CSharpAttribute> AttributesForGet { get; }

        public List<CSharpAttribute> AttributesForSet { get; }

        public string Name { get; set; }

        public CSharpField? LinkedField { get; set; }

        public string? GetBodyInlined { get; set; }

        public Action<CodeWriter, CSharpElement>? GetBody { get; set; }

        public Action<CodeWriter, CSharpElement>? SetBody { get; set; }

        public bool GetterOnly { get; set; }

        /// <inheritdoc />
        public virtual IEnumerable<CSharpAttribute> GetAttributes() => Attributes;

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            var mode = writer.Mode;
            if (mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            Visibility.DumpTo(writer);
            Modifiers.DumpTo(writer);
            ReturnType?.DumpReferenceTo(writer);
            writer.Write(" ");
            writer.Write(Name);

            if (GetBodyInlined != null)
            {
                writer.Write(" => ").Write(GetBodyInlined).WriteLine(";");
            }
            else if (GetBody == null && SetBody == null)
            {
                writer.Write(GetterOnly ? " { get; }" : " { get; set; }");
                writer.WriteLine();
            }
            else
            {
                if (mode == CodeWriterMode.Simple)
                {
                    writer.Write(" { get {...} set {...} }");
                    writer.WriteLine();
                }
                else
                {
                    writer.WriteLine();
                    writer.OpenBraceBlock();
                    if (GetBody != null)
                    {
                        AttributesForGet.DumpAttributesTo(writer);
                        writer.WriteLine("get");
                        writer.OpenBraceBlock();
                        GetBody(writer, this);
                        writer.CloseBraceBlock();
                    }

                    if (SetBody != null)
                    {
                        AttributesForSet.DumpAttributesTo(writer);
                        writer.WriteLine("set");
                        writer.OpenBraceBlock();
                        SetBody(writer, this);
                        writer.CloseBraceBlock();
                    }
                    writer.CloseBraceBlock();
                }
            }
        }
    }
}