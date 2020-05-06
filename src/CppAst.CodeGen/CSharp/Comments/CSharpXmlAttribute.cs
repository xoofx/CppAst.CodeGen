// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpXmlAttribute
    {
        public CSharpXmlAttribute(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; set; }

        public string Value { get; set; }

        public void DumpTo(CodeWriter writer)
        {
            writer.Write(Name)
                  .Write("=\"")
                  .Write(Value
                      .Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace(".", "")
                      .Replace("#", "")
                      .Replace("()", ""))
                  .Write("\"");
        }
    }
}