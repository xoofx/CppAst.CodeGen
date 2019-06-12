// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpTextComment : CSharpComment
    {
        public CSharpTextComment(string text)
        {
            Text = text;
        }

        public string Text { get; set; }

        public bool IsRawText { get; set; }

        public override void DumpTo(CodeWriter writer)
        {
            // TODO: escape
            writer.Write(Text);
        }
    }
}