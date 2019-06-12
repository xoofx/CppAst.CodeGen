// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpEnumItem : CSharpElement, ICSharpWithComment, ICSharpAttributesProvider
    {
        public CSharpEnumItem(string name) : this(name, null)
        {
        }

        public CSharpEnumItem(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            Attributes = new List<CSharpAttribute>();
        }

        public List<CSharpAttribute> Attributes { get; }

        public CSharpComment Comment { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public virtual IEnumerable<CSharpAttribute> GetAttributes()
        {
            return Attributes;
        }

        public override void DumpTo(CodeWriter writer)
        {
            if (writer.Mode == CodeWriterMode.Full) Comment?.DumpTo(writer);
            this.DumpAttributesTo(writer);
            writer.Write(Name);
            if (Value != null)
            {
                writer.Write(" = ");
                writer.Write(Value);
            }

            writer.Write(",");
            writer.WriteLine();
        }
    }
}