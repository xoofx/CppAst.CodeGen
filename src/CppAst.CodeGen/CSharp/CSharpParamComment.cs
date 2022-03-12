// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpParamComment : CSharpXmlComment
    {
        public CSharpParamComment(string name) : base("param")
        {
            Attributes.Add(new CSharpXmlAttribute("name", name ?? throw new ArgumentNullException(nameof(name))));
            IsInline = true;
        }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            base.DumpTo(writer);
            writer.WriteLine();
        }
    }
}
