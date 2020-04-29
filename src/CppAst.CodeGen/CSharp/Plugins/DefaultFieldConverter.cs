// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultFieldConverter : ICSharpConverterPlugin
    {
        private const string BitFieldName = "__bitfield__";

        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.FieldConverters.Add(ConvertField);
        }

        public static CSharpElement ConvertField(CSharpConverter converter, CppField cppField, CSharpElement context)
        {
            // Early exit if this is a global variable (we don't handle dllexport)
            bool isConst = cppField.Type is CppQualifiedType qualifiedType && qualifiedType.Qualifier == CppTypeQualifier.Const;
            var isGlobalVariable = (!(cppField.Parent is CppClass) && !isConst) || cppField.StorageQualifier == CppStorageQualifier.Static;
            if (isGlobalVariable)
            {
                return null;
            }

            var isParentClass = cppField.Parent is CppClass;

            var csContainer = converter.GetCSharpContainer(cppField, context);

            var isUnion = ((cppField.Parent as CppClass)?.ClassKind ?? CppClassKind.Struct) == CppClassKind.Union;

            var csFieldName = converter.GetCSharpName(cppField, (CSharpElement)csContainer);

            if (cppField.IsBitField)
            {
                CSharpBitField csBitFieldStorage = null;
                for (var index = csContainer.Members.Count - 1; index >= 0; index--)
                {
                    var member = csContainer.Members[index];
                    if (member is CSharpField csPreviousField)
                    {
                        csBitFieldStorage = csPreviousField as CSharpBitField;
                        break;
                    }
                }

                if (csBitFieldStorage == null || (csBitFieldStorage.CurrentBitWidth + cppField.BitFieldWidth) > csBitFieldStorage.MaxBitWidth)
                {
                    var canonicalType = (CppPrimitiveType)cppField.Type.GetCanonicalType();
                    csBitFieldStorage = new CSharpBitField(BitFieldName + csContainer.Members.Count)
                    {
                        Visibility = CSharpVisibility.Private,
                    };
                    switch (canonicalType.Kind)
                    {
                        case CppPrimitiveKind.Bool:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.Byte;
                            csBitFieldStorage.MaxBitWidth = 8;
                            break;
                        case CppPrimitiveKind.WChar:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.UShort;
                            csBitFieldStorage.MaxBitWidth = 16;
                            break;
                        case CppPrimitiveKind.Char:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.Byte;
                            csBitFieldStorage.MaxBitWidth = 8;
                            break;
                        case CppPrimitiveKind.Short:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.Short;
                            csBitFieldStorage.MaxBitWidth = 16;
                            break;
                        case CppPrimitiveKind.Int:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.Int;
                            csBitFieldStorage.MaxBitWidth = 32;
                            break;
                        case CppPrimitiveKind.LongLong:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.Long;
                            csBitFieldStorage.MaxBitWidth = 64;
                            break;
                        case CppPrimitiveKind.UnsignedChar:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.Byte;
                            csBitFieldStorage.MaxBitWidth = 8;
                            break;
                        case CppPrimitiveKind.UnsignedShort:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.UShort;
                            csBitFieldStorage.MaxBitWidth = 16;
                            break;
                        case CppPrimitiveKind.UnsignedInt:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.UInt;
                            csBitFieldStorage.MaxBitWidth = 32;
                            break;
                        case CppPrimitiveKind.UnsignedLongLong:
                            csBitFieldStorage.FieldType = CSharpPrimitiveType.ULong;
                            csBitFieldStorage.MaxBitWidth = 64;
                            break;
                        default:
                            csBitFieldStorage.FieldType = new CSharpFreeType("unsupported_bitfield_type_" + canonicalType);
                            csBitFieldStorage.MaxBitWidth = 128;
                            break;
                    }
                    csContainer.Members.Add(csBitFieldStorage);
                }

                int currentBitOffset = csBitFieldStorage.CurrentBitWidth;
                csBitFieldStorage.CurrentBitWidth += cppField.BitFieldWidth;

                var csProperty = new CSharpProperty(csFieldName)
                {
                    CppElement = cppField,
                    LinkedField = csBitFieldStorage,
                };
                converter.ApplyDefaultVisibility(csProperty, csContainer);
                csProperty.Comment = converter.GetCSharpComment(cppField, csProperty);

                var bitmask = (1L << cppField.BitFieldWidth) - 1;
                var bitmaskStr = $"0b{Convert.ToString(bitmask, 2)}";
                var notBitMaskStr = Convert.ToString(~(bitmask << currentBitOffset), 2);
                if (notBitMaskStr.Length > csBitFieldStorage.MaxBitWidth)
                {
                    notBitMaskStr = notBitMaskStr.Substring(notBitMaskStr.Length - csBitFieldStorage.MaxBitWidth);
                }

                csProperty.ReturnType = converter.GetCSharpType(cppField.Type, csProperty);
                csProperty.GetBody = (writer, element) =>
                {
                    writer.Write("return unchecked((");
                    csProperty.ReturnType.DumpReferenceTo(writer);
                    writer.Write(")");
                    writer.Write($"(({csBitFieldStorage.Name} >> {currentBitOffset}) & {bitmaskStr})");
                    writer.Write(");");
                    writer.WriteLine();
                };
                csProperty.SetBody = (writer, element) =>
                {
                    writer.Write($"{csBitFieldStorage.Name} = ({csBitFieldStorage.Name} & unchecked((");
                    csBitFieldStorage.FieldType.DumpReferenceTo(writer);
                    writer.Write($")0b{notBitMaskStr})) | ((((");
                    csBitFieldStorage.FieldType.DumpReferenceTo(writer);
                    writer.Write(")value) & (unchecked((");
                    csBitFieldStorage.FieldType.DumpReferenceTo(writer);
                    writer.Write($"){bitmaskStr})) << {currentBitOffset}));");
                    writer.WriteLine();
                };

                csContainer.Members.Add(csProperty);
                return csProperty;
            }

            var parentName = cppField.Parent is CppClass cppClass ? cppClass.Name : string.Empty;
            var csField = new CSharpField(csFieldName) { CppElement = cppField };
            converter.ApplyDefaultVisibility(csField, csContainer);

            if (isConst)
            {
                if (isParentClass)
                {
                    csField.Modifiers |= CSharpModifiers.ReadOnly;
                }
                else
                {
                    csField.Modifiers |= CSharpModifiers.Const;
                }
            }

            csContainer.Members.Add(csField);

            csField.Comment = converter.GetCSharpComment(cppField, csField);

            if (isUnion)
            {
                csField.Attributes.Add(new CSharpFreeAttribute("FieldOffset(0)"));
                converter.AddUsing(csContainer, "System.Runtime.InteropServices");
            }
            csField.FieldType = converter.GetCSharpType(cppField.Type, csField);

            if (cppField.InitExpression != null)
            {
                if (cppField.InitExpression.Kind == CppExpressionKind.Unexposed)
                {
                    csField.InitValue = cppField.InitValue?.Value?.ToString();
                }
                else
                {
                    csField.InitValue = converter.ConvertExpression(cppField.InitExpression, context, csField.FieldType);
                }
            }

            return csField;
        }
    }
}