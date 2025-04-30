// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpPrimitiveType : CSharpType
    {
        public static CSharpPrimitiveType Void() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Void);
        public static CSharpPrimitiveType Bool() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Bool);
        public static CSharpPrimitiveType Char() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Char);
        public static CSharpPrimitiveType SByte() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.SByte);
        public static CSharpPrimitiveType Byte() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Byte);
        public static CSharpPrimitiveType Short() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Short);
        public static CSharpPrimitiveType UShort() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.UShort);
        public static CSharpPrimitiveType Int() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Int);
        public static CSharpPrimitiveType UInt() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.UInt);
        public static CSharpPrimitiveType Long() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Long);
        public static CSharpPrimitiveType ULong() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.ULong);
        public static CSharpPrimitiveType Float() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Float);
        public static CSharpPrimitiveType Double() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Double);
        public static CSharpPrimitiveType Object() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.Object);
        public static CSharpPrimitiveType Int128() => new CSharpPrimitiveType(CSharpPrimitiveKind.Int128);
        public static CSharpPrimitiveType UInt128() => new CSharpPrimitiveType(CSharpPrimitiveKind.UInt128);
        public static CSharpPrimitiveType String() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.String);
        public static CSharpPrimitiveType IntPtr() =>  new CSharpPrimitiveType(CSharpPrimitiveKind.IntPtr);
        public static CSharpPrimitiveType UIntPtr() => new CSharpPrimitiveType(CSharpPrimitiveKind.UIntPtr);

        public CSharpPrimitiveType(CSharpPrimitiveKind kind)
        {
            Kind = kind;
        }

        public CSharpPrimitiveKind Kind { get; }

        /// <inheritdoc />
        public override void DumpTo(CodeWriter writer)
        {
            switch (Kind)
            {
                case CSharpPrimitiveKind.Void:
                    writer.Write("void");
                    break;
                case CSharpPrimitiveKind.Bool:
                    writer.Write("bool");
                    break;
                case CSharpPrimitiveKind.Char:
                    writer.Write("char");
                    break;
                case CSharpPrimitiveKind.SByte:
                    writer.Write("sbyte");
                    break;
                case CSharpPrimitiveKind.Byte:
                    writer.Write("byte");
                    break;
                case CSharpPrimitiveKind.Short:
                    writer.Write("short");
                    break;
                case CSharpPrimitiveKind.UShort:
                    writer.Write("ushort");
                    break;
                case CSharpPrimitiveKind.Int:
                    writer.Write("int");
                    break;
                case CSharpPrimitiveKind.UInt:
                    writer.Write("uint");
                    break;
                case CSharpPrimitiveKind.Long:
                    writer.Write("long");
                    break;
                case CSharpPrimitiveKind.ULong:
                    writer.Write("ulong");
                    break;
                case CSharpPrimitiveKind.Float:
                    writer.Write("float");
                    break;
                case CSharpPrimitiveKind.Double:
                    writer.Write("double");
                    break;
                case CSharpPrimitiveKind.Object:
                    writer.Write("object");
                    break;
                case CSharpPrimitiveKind.String:
                    writer.Write("string");
                    break;
                case CSharpPrimitiveKind.IntPtr:
                    writer.Write("nint");
                    break;
                case CSharpPrimitiveKind.UIntPtr:
                    writer.Write("nuint");
                    break;
                case CSharpPrimitiveKind.Int128:
                    writer.Write("System.Int128");
                    break;
                case CSharpPrimitiveKind.UInt128:
                    writer.Write("System.UInt128");
                    break;
                default:
                    throw new InvalidOperationException($"{Kind} is not supported");
            }

        }

        /// <inheritdoc />
        public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
    }
}