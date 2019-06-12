// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public static class CSharpHelper
    {
        public static CallingConvention GetCSharpCallingConvention(this CppCallingConvention cppCallingConvention)
        {
            switch (cppCallingConvention)
            {
                case CppCallingConvention.C:
                case CppCallingConvention.Default:
                    return CallingConvention.Cdecl;
                case CppCallingConvention.X86StdCall:
                    return CallingConvention.StdCall;
                case CppCallingConvention.X86FastCall:
                    return CallingConvention.FastCall;
                case CppCallingConvention.X86ThisCall:
                    return CallingConvention.ThisCall;
                case CppCallingConvention.Win64:
                    return CallingConvention.Winapi;

                case CppCallingConvention.X86Pascal:
                case CppCallingConvention.AAPCS:
                case CppCallingConvention.AAPCS_VFP:
                case CppCallingConvention.X86RegCall:
                case CppCallingConvention.IntelOclBicc:
                case CppCallingConvention.X86_64SysV:
                case CppCallingConvention.X86VectorCall:
                case CppCallingConvention.Swift:
                case CppCallingConvention.PreserveMost:
                case CppCallingConvention.PreserveAll:
                case CppCallingConvention.AArch64VectorCall:
                case CppCallingConvention.Invalid:
                case CppCallingConvention.Unexposed:
                    break;
            }
            return CallingConvention.Cdecl;
        }

        public static CSharpPrimitiveType GetCSharpPrimitive(CppPrimitiveType cppType)
        {
            switch (cppType.Kind)
            {
                case CppPrimitiveKind.Void:
                    return CSharpPrimitiveType.Void;
                case CppPrimitiveKind.Bool:
                    return CSharpPrimitiveType.Bool;
                case CppPrimitiveKind.WChar:
                    return CSharpPrimitiveType.Char;
                case CppPrimitiveKind.Char:
                    return CSharpPrimitiveType.SByte;
                case CppPrimitiveKind.Short:
                    return CSharpPrimitiveType.Short;
                case CppPrimitiveKind.Int:
                    return CSharpPrimitiveType.Int;
                case CppPrimitiveKind.LongLong:
                    return CSharpPrimitiveType.Long;
                case CppPrimitiveKind.UnsignedChar:
                    return CSharpPrimitiveType.Byte;
                case CppPrimitiveKind.UnsignedShort:
                    return CSharpPrimitiveType.UShort;
                case CppPrimitiveKind.UnsignedInt:
                    return CSharpPrimitiveType.UInt;
                case CppPrimitiveKind.UnsignedLongLong:
                    return CSharpPrimitiveType.ULong;
                case CppPrimitiveKind.Float:
                    return CSharpPrimitiveType.Float;
                case CppPrimitiveKind.Double:
                    return CSharpPrimitiveType.Double;
                case CppPrimitiveKind.LongDouble:
                    return CSharpPrimitiveType.Double;
                default:
                    throw new ArgumentOutOfRangeException($"The kind {cppType.Kind} is not supported");
            }
        }

        public static string EscapeName(string name)
        {
            // From https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
            switch (name)
            {
                case "abstract":
                case "as":
                case "base":
                case "bool":
                case "break":
                case "byte":
                case "case":
                case "catch":
                case "char":
                case "checked":
                case "class":
                case "const":
                case "continue":
                case "decimal":
                case "default":
                case "delegate":
                case "do":
                case "double":
                case "else":
                case "enum":
                case "event":
                case "explicit":
                case "extern":
                case "false":
                case "finally":
                case "fixed":
                case "float":
                case "for":
                case "foreach":
                case "goto":
                case "if":
                case "implicit":
                case "in":
                case "int":
                case "interface":
                case "internal":
                case "is":
                case "lock":
                case "long":
                case "namespace":
                case "new":
                case "null":
                case "object":
                case "operator":
                case "out":
                case "override":
                case "params":
                case "private":
                case "protected":
                case "public":
                case "readonly":
                case "ref":
                case "return":
                case "sbyte":
                case "sealed":
                case "short":
                case "sizeof":
                case "stackalloc":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "this":
                case "throw":
                case "true":
                case "try":
                case "typeof":
                case "uint":
                case "ulong":
                case "unchecked":
                case "unsafe":
                case "ushort":
                case "using":
                case "virtual":
                case "void":
                case "volatile":
                case "while":
                    return $"@{name}";

                default:
                    return name;
            }
        }
    }
}