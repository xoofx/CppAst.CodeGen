// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpXmlComment : CSharpComment
    {
        public CSharpXmlComment(string tagName)
        {
            TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
            Attributes = new List<CSharpXmlAttribute>();
        }

        public string TagName { get; set; }

        public bool IsInline { get; set; }

        public bool IsSelfClosing { get; set; }

        public List<CSharpXmlAttribute> Attributes { get; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write("<").Write(TagName);

            for (var i = 0; i < Attributes.Count; i++)
            {
                var attr = Attributes[i];
                writer.Write(" ");
                attr.DumpTo(writer);
            }

            if (IsSelfClosing)
            {
                writer.Write("/");
            }

            writer.Write(">");

            if (!IsInline)
            {
                writer.WriteLine();
            }

            if (!IsSelfClosing)
            {
                DumpChildrenTo(writer, IsInline);

                if (!IsInline)
                {
                    writer.WriteLine();
                }

                writer.Write("</").Write(TagName).Write(">");
            }

            if (!IsInline)
            {
                writer.WriteLine();
            }
        }
    }
}