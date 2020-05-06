// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpParamComment : CSharpComment
    {
        public CSharpParamComment(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; set; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write("<param name=\"")
                  .Write(Name
                  .Replace("&", "&amp;")
                  .Replace("<", "&lt;")
                  .Replace(">", "&gt;")
                  .Replace(".", "")
                  .Replace("#", "")
                  .Replace("()", ""))
                  .Write("\">");
            DumpChildrenTo(writer);
            writer.WriteLine("</param>");
        }
    }
}