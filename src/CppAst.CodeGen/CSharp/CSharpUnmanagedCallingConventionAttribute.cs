// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpUnmanagedCallingConventionAttribute : CSharpAttribute
    {
        public CSharpUnmanagedCallingConventionAttribute(CallingConvention convention)
        {
            Convention = convention;
        }

        public CallingConvention Convention { get; set; }


        public override string ToText()
        {
            return $"UnmanagedCallConv(CallConvs = new Type[] {{ typeof(CallConv{Convention.GetUnmanagedCallConvType()}) }})";
        }
    }
}