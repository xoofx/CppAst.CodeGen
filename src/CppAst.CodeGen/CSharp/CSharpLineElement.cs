// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpLineElement : CSharpElement
    {
        public CSharpLineElement()
        {
            Text = string.Empty;
        }

        public CSharpLineElement(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public string Text { get; set; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            if (writer.Mode == CodeWriterMode.Full)
            {
                writer.WriteLine(Text);
            }
        }
    }
}