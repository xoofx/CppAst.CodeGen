// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpTextComment : CSharpComment
    {
        public CSharpTextComment(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            Text = text;
        }

        public string Text { get; set; }

        public bool IsHtmlText { get; set; }

        public override void DumpTo(CodeWriter writer)
        {
            writer.Write(IsHtmlText ? Text : Text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
        }
    }
}