// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpStructLayoutAttribute : CSharpAttribute
    {
        public CSharpStructLayoutAttribute(LayoutKind layoutKind)
        {
            LayoutKind = layoutKind;
        }

        public LayoutKind LayoutKind { get; set; }

        public CharSet? CharSet { get; set; }

        public int? Pack { get; set; }

        public int? Size { get; set; }

        /// <inheritdoc />
        public override string ToText()
        {
            var builder = new StringBuilder();
            builder.Append("StructLayout(LayoutKind.");
            builder.Append(LayoutKind.ToString());
            if (CharSet.HasValue)
            {
                builder.Append(", CharSet = CharSet.");
                builder.Append(CharSet.Value.ToString());
            }

            if (Pack.HasValue)
            {
                builder.Append(", Pack = ");
                builder.Append(Pack.Value.ToString());
            }

            if (Size.HasValue)
            {
                builder.Append(", Size = ");
                builder.Append(Size.Value.ToString());
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}