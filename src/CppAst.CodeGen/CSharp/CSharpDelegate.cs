// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpDelegate : CSharpNamedType, ICSharpWithComment, ICSharpAttributesProvider, ICSharpElementWithVisibility
    {
        public CSharpDelegate(string name) : base(name)
        {
            Attributes = new List<CSharpAttribute>();
            Parameters = new List<CSharpParameter>();
        }

        /// <inheritdoc />
        public CSharpComment Comment { get; set; }

        /// <inheritdoc />
        public CSharpVisibility Visibility { get; set; }

        public List<CSharpAttribute> Attributes { get; }

        public CSharpType ReturnType { get; set; }

        public List<CSharpParameter> Parameters { get; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            if (writer.Mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            ReturnType?.DumpContextualAttributesTo(writer, false, CSharpAttributeScope.Return);
            Visibility.DumpTo(writer);
            writer.Write("delegate ");
            ReturnType?.DumpReferenceTo(writer);
            writer.Write(" ");
            writer.Write(Name);
            Parameters.DumpTo(writer);
            writer.Write(";");
            writer.WriteLine();
        }

        /// <inheritdoc />
        public virtual IEnumerable<CSharpAttribute> GetAttributes() => Attributes;
    }
}