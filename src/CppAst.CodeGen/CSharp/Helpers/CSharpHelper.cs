// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CppAst.CodeGen.CSharp
{
    public static class CSharpHelper
    {
        private const string LetterLowerChar = @"\p{Ll}";
        private const string LetterLowerOrDigitChar = "[" + LetterLowerChar + DigitChar + "]";
        private const string LetterUpperChar = @"\p{Lu}";
        private const string LetterUpperOrDigitChar = "[" + LetterUpperChar + DigitChar + "]";
        private const string DigitChar = @"\p{N}";

        private const string LowerWord = LetterLowerChar + LetterLowerOrDigitChar + "*";
        private const string UpperWord = LetterUpperChar + LetterUpperOrDigitChar + "*";
        private const string PascalWord = LetterUpperChar + LetterLowerOrDigitChar + "+";

        private static readonly Regex MatchLowerCase = new Regex($"^{LowerWord}$");
        private static readonly Regex MatchCamelCase = new Regex($"^{LowerWord}({PascalWord})+$");
        private static readonly Regex MatchPascalCase = new Regex($"^({PascalWord})+$");
        private static readonly Regex MatchScreamingCase = new Regex($"^{UpperWord}$");

        private static readonly Regex MatchCamelSnakeCase = new Regex($"^{LowerWord}(_{PascalWord})+$");
        private static readonly Regex MatchLowerSnakeCase = new Regex($"^{LowerWord}(_{LetterLowerOrDigitChar}+)+$");
        private static readonly Regex MatchPascalSnakeCase = new Regex($"^{PascalWord}(_{PascalWord})+$");
        private static readonly Regex MatchScreamingSnakeCase = new Regex($"^{UpperWord}(_{LetterUpperOrDigitChar}+)+$");

        public static bool IsSnake(this CSharpCasingKind casingKind)
        {
            return casingKind >= CSharpCasingKind.CamelSnake;
        }

        public static bool IsCamel(this CSharpCasingKind casingKind)
        {
            return casingKind == CSharpCasingKind.Camel || casingKind == CSharpCasingKind.CamelSnake;
        }

        public static bool IsLower(this CSharpCasingKind casingKind)
        {
            return casingKind == CSharpCasingKind.Lower || casingKind == CSharpCasingKind.LowerSnake;
        }

        public static bool IsPascal(this CSharpCasingKind casingKind)
        {
            return casingKind == CSharpCasingKind.Pascal || casingKind == CSharpCasingKind.PascalSnake;
        }

        public static string ToPascal(string text)
        {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            if (text.Length == 0) { return text; }

            var builder = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var value = text[i];

                if (i == 0 && char.IsLower(value))
                {
                    value = char.ToUpper(value);
                }

                if (value == '_')
                {
                    builder.Append(char.ToUpper(text[i + 1]));
                    i++;
                }
                else
                {
                    builder.Append(value);
                }
            }

            return builder.ToString();
        }

        internal static string AppendWithCasing(string name, CSharpCasingKind nameCasingKind, string nameToAppend, CSharpCasingKind nameToAppendCasingKind)
        {
            // Could be improved to better merge casing
            if (!string.IsNullOrEmpty(nameToAppend))
            {
                if ((nameCasingKind.IsSnake() || nameToAppendCasingKind.IsSnake()))
                {
                    if (nameCasingKind.IsPascal() && !nameToAppendCasingKind.IsPascal())
                    {
                        name = $"{name}_{ToPascal(nameToAppend)}";
                    }
                    else
                    {
                        name = $"{name}_{nameToAppend}";
                    }
                }
                else
                {
                    if (nameCasingKind.IsPascal() && !nameToAppendCasingKind.IsPascal())
                    {
                        name = name + ToPascal(nameToAppend);
                    }
                    else
                    {
                        if (nameCasingKind == CSharpCasingKind.Lower && nameToAppendCasingKind == CSharpCasingKind.Lower)
                        {
                            name = $"{name}_{nameToAppend}";
                        }
                        else
                        {
                            name += nameToAppend;
                        }
                    }
                }
            }
            return name;
        }

        public static CSharpCasingKind GetCSharpCasingKind(string name)
        {
            if (MatchLowerCase.Match(name).Success) return CSharpCasingKind.Lower;
            if (MatchCamelCase.Match(name).Success) return CSharpCasingKind.Camel;
            if (MatchPascalCase.Match(name).Success) return CSharpCasingKind.Pascal;
            if (MatchScreamingCase.Match(name).Success) return CSharpCasingKind.Screaming;
            if (MatchCamelSnakeCase.Match(name).Success) return CSharpCasingKind.CamelSnake;
            if (MatchLowerSnakeCase.Match(name).Success) return CSharpCasingKind.LowerSnake;
            if (MatchPascalSnakeCase.Match(name).Success) return CSharpCasingKind.PascalSnake;
            if (MatchScreamingSnakeCase.Match(name).Success) return CSharpCasingKind.ScreamingSnake;

            return CSharpCasingKind.Undefined;
        }

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
                default:
                    return CallingConvention.Cdecl;
            }
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