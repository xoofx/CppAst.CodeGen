// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public static class CSharpUnmanagedCallingConventionExtension
    {
        public static string GetUnmanagedCallConvType(this CallingConvention callingConvention)
        {
            return callingConvention switch
            {
                CallingConvention.Cdecl => "Cdecl",
                CallingConvention.StdCall => "Stdcall",
                CallingConvention.ThisCall => "Thiscall",
                CallingConvention.FastCall => "Fastcall",
                CallingConvention.Winapi => "Winapi"
            };
        }
    }
}