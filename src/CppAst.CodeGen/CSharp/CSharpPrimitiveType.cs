// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpPrimitiveType : CSharpType
    {
        public static readonly CSharpPrimitiveType Void = new CSharpPrimitiveType(CSharpPrimitiveKind.Void);
        public static readonly CSharpPrimitiveType Bool = new CSharpPrimitiveType(CSharpPrimitiveKind.Bool);
        public static readonly CSharpPrimitiveType Char = new CSharpPrimitiveType(CSharpPrimitiveKind.Char);
        public static readonly CSharpPrimitiveType SByte = new CSharpPrimitiveType(CSharpPrimitiveKind.SByte);
        public static readonly CSharpPrimitiveType Byte = new CSharpPrimitiveType(CSharpPrimitiveKind.Byte);
        public static readonly CSharpPrimitiveType Short = new CSharpPrimitiveType(CSharpPrimitiveKind.Short);
        public static readonly CSharpPrimitiveType UShort = new CSharpPrimitiveType(CSharpPrimitiveKind.UShort);
        public static readonly CSharpPrimitiveType Int = new CSharpPrimitiveType(CSharpPrimitiveKind.Int);
        public static readonly CSharpPrimitiveType UInt = new CSharpPrimitiveType(CSharpPrimitiveKind.UInt);
        public static readonly CSharpPrimitiveType Long = new CSharpPrimitiveType(CSharpPrimitiveKind.Long);
        public static readonly CSharpPrimitiveType ULong = new CSharpPrimitiveType(CSharpPrimitiveKind.ULong);
        public static readonly CSharpPrimitiveType Float = new CSharpPrimitiveType(CSharpPrimitiveKind.Float);
        public static readonly CSharpPrimitiveType Double = new CSharpPrimitiveType(CSharpPrimitiveKind.Double);
        public static readonly CSharpPrimitiveType Object = new CSharpPrimitiveType(CSharpPrimitiveKind.Object);
        public static readonly CSharpPrimitiveType String = new CSharpPrimitiveType(CSharpPrimitiveKind.String);
        public static readonly CSharpPrimitiveType IntPtr = new CSharpPrimitiveType(CSharpPrimitiveKind.IntPtr);

        private CSharpPrimitiveType(CSharpPrimitiveKind kind)
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
                    writer.Write("IntPtr");
                    break;
                default:
                    throw new InvalidOperationException($"{Kind} is not supported");
            }

        }

        /// <inheritdoc />
        public override void DumpReferenceTo(CodeWriter writer) => DumpTo(writer);
    }
}