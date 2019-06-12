// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpParamComment : CSharpComment
    {
        public string Name { get; set; }
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write("<param name=\"").Write(Name).Write("\">");
            DumpChildrenTo(writer);
            writer.WriteLine("</param>");
        }
    }
}