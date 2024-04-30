// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpLibraryImportAttribute : CSharpAttribute
    {
        public CSharpLibraryImportAttribute(string dllName)
        {
            DllName = dllName;
        }

        public string DllName { get; set; }

        public string? EntryPoint { get; set; }
        
        /// <inheritdoc />
        public override string ToText()
        {
            var builder = new StringBuilder();
            builder.Append("global::System.Runtime.InteropServices.LibraryImport(");
            builder.Append(DllName);
            if (EntryPoint != null)
            {
                builder.Append(", EntryPoint = ");
                builder.Append(EntryPoint);
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}