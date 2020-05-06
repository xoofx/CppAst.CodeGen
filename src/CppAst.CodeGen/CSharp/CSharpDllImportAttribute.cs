// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpDllImportAttribute : CSharpAttribute
    {
        public CSharpDllImportAttribute(string dllName)
        {
            DllName = dllName;
        }

        public string DllName { get; set; }

        public string EntryPoint { get; set; }
        public CharSet? CharSet { get; set; }
        public bool? SetLastError { get; set; }
        public bool? ExactSpelling { get; set; }
        public bool? PreserveSig { get; set; }

        public CallingConvention? CallingConvention { get; set; }
        public bool? BestFitMapping { get; set; }
        public bool? ThrowOnUnmappableChar { get; set; }

        /// <inheritdoc />
        public override string ToText()
        {
            var builder = new StringBuilder();
            builder.Append("DllImport(");
            builder.Append(DllName);
            if (EntryPoint != null)
            {
                builder.Append(", EntryPoint = ");
                builder.Append(EntryPoint);
            }

            if (CharSet.HasValue)
            {
                builder.Append(", CharSet = CharSet.");
                builder.Append(CharSet.Value.ToString());
            }

            if (SetLastError.HasValue)
            {
                builder.Append(", SetLastError = ");
                builder.Append(SetLastError.Value ? "true" : "false");
            }

            if (ExactSpelling.HasValue)
            {
                builder.Append(", ExactSpelling = ");
                builder.Append(ExactSpelling.Value ? "true" : "false");
            }

            if (PreserveSig.HasValue)
            {
                builder.Append(", PreserveSig = ");
                builder.Append(PreserveSig.Value ? "true" : "false");
            }

            if (CallingConvention.HasValue)
            {
                builder.Append(", CallingConvention = CallingConvention.");
                builder.Append(CallingConvention.Value.ToString());
            }

            if (BestFitMapping.HasValue)
            {
                builder.Append(", BestFitMapping = ");
                builder.Append(BestFitMapping.Value ? "true" : "false");
            }

            if (ThrowOnUnmappableChar.HasValue)
            {
                builder.Append(", ThrowOnUnmappableChar = ");
                builder.Append(ThrowOnUnmappableChar.Value ? "true" : "false");
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}