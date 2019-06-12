// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpMethod : CSharpElement, ICSharpAttributesProvider, ICSharpElementWithVisibility
    {
        public CSharpMethod()
        {
            Attributes = new List<CSharpAttribute>();
            Parameters = new List<CSharpParameter>();
            Visibility = CSharpVisibility.Public;
        }

        public CSharpComment Comment { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        public CSharpVisibility Visibility { get; set; }

        public CSharpModifiers Modifiers { get; set; }

        public CSharpType ReturnType { get; set; }

        public bool IsConstructor { get; set; }
        
        public string Name { get; set; }

        public List<CSharpParameter> Parameters { get; }

        public Action<CodeWriter, CSharpElement> Body { get; set; }

        public virtual IEnumerable<CSharpAttribute> GetAttributes()
        {
            return Attributes;
        }

        public override void DumpTo(CodeWriter writer)
        {
            var mode = writer.Mode;
            if (mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            ReturnType?.DumpContextualAttributesTo(writer, false, CSharpAttributeScope.Return);
            Visibility.DumpTo(writer);
            Modifiers.DumpTo(writer);
            if (IsConstructor)
            {
                writer.Write((Parent as CSharpNamedType)?.Name);
            }
            else
            {
                ReturnType?.DumpReferenceTo(writer);
                writer.Write(" ");
                writer.Write(Name);
            }
            Parameters.DumpTo(writer);

            if (Body != null)
            {
                if (mode == CodeWriterMode.Full)
                {
                    writer.WriteLine();
                    writer.OpenBraceBlock();
                    Body?.Invoke(writer, this);
                    writer.CloseBraceBlock();
                }
                else
                {
                    writer.WriteLine(" { ... }");
                }
            }
            else
            {
                writer.WriteLine(";");
            }
        }
    }
}