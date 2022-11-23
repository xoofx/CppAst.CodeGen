// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    /// <summary>
    /// Represents a function pointer. 
    /// </summary>
    public class CSharpFunctionPointer : CSharpType
    {
        public CSharpFunctionPointer(CSharpType returnType)
        {
            Parameters = new List<CSharpType>();
            UnmanagedCallingConvention = new List<string>();
            ReturnType = returnType;
        }
        
        /// <summary>
        /// Gets or sets a boolean indicating if this is an unmanaged function pointer.
        /// </summary>
        public bool IsUnmanaged { get; set; }

        /// <summary>
        /// Calling convention attributes (Cdecl, Stdcall, Thiscall, Fastcall + attribute names).
        /// </summary>
        public List<string> UnmanagedCallingConvention { get; }

        /// <summary>
        /// The parameters of this function pointer.
        /// </summary>
        public List<CSharpType> Parameters { get; }
        
        /// <summary>
        /// Gets or sets the return type.
        /// </summary>
        public CSharpType ReturnType { get; set; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            writer.Write("delegate*");
            if (IsUnmanaged)
            {
                writer.Write("unmanaged");
                if (UnmanagedCallingConvention.Count > 0)
                {
                    writer.Write("[");
                    for (var i = 0; i < UnmanagedCallingConvention.Count; i++)
                    {
                        var callConv = UnmanagedCallingConvention[i];
                        if (i > 0) writer.Write(", ");
                        writer.Write(callConv);
                    }
                    writer.Write("]");

                }
            }
            writer.Write("<");
            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameterType = Parameters[i];
                if (i > 0) writer.Write(", ");
                parameterType.DumpReferenceTo(writer);
            }
            if (Parameters.Count > 0) writer.Write(", ");

            ReturnType.DumpReferenceTo(writer);
            writer.Write(">");
        }

        public override void DumpReferenceTo(CodeWriter writer)
        {
            DumpTo(writer);
        }
    }
}