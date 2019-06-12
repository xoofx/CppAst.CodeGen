// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultTypeConverter: ICSharpConverterPlugin
    {
        private static readonly string StringTypeKey = typeof(DefaultTypeConverter).FullName + "." + nameof(StringTypeKey);
        private static readonly string BoolTypeKey = typeof(DefaultTypeConverter).FullName + "." + nameof(BoolTypeKey);
        private static readonly CppQualifiedType ConstChar = new CppQualifiedType(CppTypeQualifier.Const, CppPrimitiveType.Char);
        private static readonly CppQualifiedType ConstVoid = new CppQualifiedType(CppTypeQualifier.Const, CppPrimitiveType.Void);

        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpTypeResolvers.Add(GetCSharpType);
        }

        public static CSharpType GetCSharpType(CSharpConverter converter, CppType cppType, CSharpElement context)
        {
            // Early exit for primitive types
            if (cppType is CppPrimitiveType cppPrimitiveType)
            {
                // Special case for bool
                if (cppPrimitiveType.Kind == CppPrimitiveKind.Bool)
                {
                    return GetBoolType(converter);
                }

                return CSharpHelper.GetCSharpPrimitive(cppPrimitiveType);
            }

            // Check if a particular CppType has been already converted
            var csType = converter.FindCSharpType(cppType);
            if (csType != null)
            {
                return csType;
            }

            // Pre-decode the type by extracting any const/pointer and get the element type directly
            DecodeSimpleType(cppType, out bool isConst, out bool isPointer, out CppType elementType);

            if (isPointer)
            {
                if (isConst && elementType.Equals(CppPrimitiveType.Char))
                {
                    // const char* => string (with marshal)
                    csType = GetStringType(converter);
                }
                else
                {
                    var pointedCSharpType = converter.FindCSharpType(elementType);

                    if (context is CSharpParameter)
                    {
                        switch (elementType.TypeKind)
                        {
                            case CppTypeKind.Array:
                                break;
                            case CppTypeKind.Reference:
                                break;
                            case CppTypeKind.Qualified:
                                var qualifiedType = (CppQualifiedType) elementType;
                                csType = new CSharpRefType(qualifiedType.Qualifier == CppTypeQualifier.Const ? CSharpRefKind.In : CSharpRefKind.Ref, converter.GetCSharpType(qualifiedType.ElementType, context));
                                break;
                            case CppTypeKind.Function:
                                csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(elementType, context));
                                break;
                            case CppTypeKind.Typedef:
                                csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(elementType, context));
                                break;
                            case CppTypeKind.StructOrClass:
                                csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(elementType, context));
                                break;
                            case CppTypeKind.Enum:
                                csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(elementType, context));
                                break;
                            case CppTypeKind.TemplateParameterType:
                                break;
                            case CppTypeKind.Unexposed:
                                break;
                            case CppTypeKind.Primitive:
                                var cppPrimitive = (CppPrimitiveType) elementType;
                                if (cppPrimitive.Kind != CppPrimitiveKind.Void)
                                {
                                    csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(elementType, context));
                                }
                                break;
                            case CppTypeKind.Pointer:
                                csType = new CSharpRefType(CSharpRefKind.Out, converter.GetCSharpType(elementType, context));
                                break;
                        }
                    }
                    else
                    {
                        switch (elementType.TypeKind)
                        {
                            case CppTypeKind.Array:
                                break;
                            case CppTypeKind.Reference:
                                break;
                            case CppTypeKind.Qualified:
                                break;
                            case CppTypeKind.Function:
                                break;
                            case CppTypeKind.Typedef:
                                break;
                            case CppTypeKind.StructOrClass:
                                // Is the struct is an opaque definition (which can is transformed into passing the struct directly as 
                                // the struct contains the pointer)
                                if (pointedCSharpType is CSharpStruct csStruct && csStruct.IsOpaque)
                                {
                                    csType = csStruct;
                                }
                                break;
                            case CppTypeKind.Enum:
                                break;
                            case CppTypeKind.TemplateParameterType:
                                break;
                            case CppTypeKind.Unexposed:
                                break;
                        }
                    }

                    // Any pointers that is not decoded to a simpler form is exposed as an IntPtr
                    csType = csType ?? CSharpPrimitiveType.IntPtr;
                }
            }
            else
            {
                switch (cppType.TypeKind)
                {
                    case CppTypeKind.Array:

                        var arrayType = (CppArrayType)cppType;
                        var arrayElementType = arrayType.ElementType;

                        if (arrayType.Size < 0 && arrayElementType.Equals(ConstChar))
                        {
                            // const char[] => string (with marshal)
                            csType = GetStringType(converter);
                        }
                        else
                        {
                            var csArrayElementType = converter.GetCSharpType(arrayElementType, context);
                            csType = new CSharpArrayType(csArrayElementType);
                            var typeWithAttributes = new CSharpTypeWithAttributes(csType);
                            var attr = new CSharpMarshalAttribute(CSharpUnmanagedKind.LPArray);
                            if (csArrayElementType is CSharpTypeWithAttributes csArrayElementTypeWithAttributes)
                            {
                                var marshalAttributeForArrayElementType = GetMarshalAttributeOrNull(csArrayElementTypeWithAttributes.Attributes);
                                attr.ArraySubType = marshalAttributeForArrayElementType.UnmanagedType;
                            }

                            if (arrayType.Size >= 0)
                            {
                                attr.SizeConst = arrayType.Size;
                            }
                            typeWithAttributes.Attributes.Add(attr);
                            csType = typeWithAttributes;
                        }
                        break;

                    case CppTypeKind.Reference:
                        csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(((CppReferenceType)cppType).ElementType, context));
                        break;
                    case CppTypeKind.Qualified:
                        var qualifiedType = (CppQualifiedType) cppType;
                        csType = converter.GetCSharpType(qualifiedType.ElementType, context);
                        // TODO: Handle in parameters
                        break;
                    case CppTypeKind.Function:
                        break;
                    case CppTypeKind.Typedef:
                        break;
                    case CppTypeKind.StructOrClass:
                    case CppTypeKind.Enum:
                        break;
                    case CppTypeKind.TemplateParameterType:
                        break;
                    case CppTypeKind.Unexposed:
                        break;
                }
            }

            return csType;
        }

        private static CSharpMarshalAttribute GetMarshalAttributeOrNull(List<CSharpAttribute> attributes)
        {
            foreach (var cSharpAttribute in attributes)
            {
                if (cSharpAttribute is CSharpMarshalAttribute csMarshalAttribute)
                {
                    return csMarshalAttribute;
                }
            }

            return null;
        }

        public static CSharpType GetBoolType(CSharpConverter converter)
        {
            var boolType = converter.GetTagValueOrDefault<CSharpType>(BoolTypeKey);
            if (boolType == null)
            {
                if (converter.Options.DefaultMarshalForBool == null)
                {
                    boolType = CSharpPrimitiveType.Bool;
                }
                else
                {
                    var boolTypeWithMarshal = new CSharpTypeWithAttributes(CSharpPrimitiveType.Bool);
                    boolTypeWithMarshal.Attributes.Add(converter.Options.DefaultMarshalForBool);
                    boolType = boolTypeWithMarshal;
                }
                converter.Tags[BoolTypeKey] = boolType;
            }
            return boolType;
        }

        public static CSharpType GetStringType(CSharpConverter converter)
        {
            var boolType = converter.GetTagValueOrDefault<CSharpType>(StringTypeKey);
            if (boolType == null)
            {
                if (converter.Options.DefaultMarshalForString == null)
                {
                    boolType = CSharpPrimitiveType.String;
                }
                else
                {
                    var boolTypeWithMarshal = new CSharpTypeWithAttributes(CSharpPrimitiveType.String);
                    boolTypeWithMarshal.Attributes.Add(converter.Options.DefaultMarshalForString);
                    boolType = boolTypeWithMarshal;
                }
                converter.Tags[StringTypeKey] = boolType;
            }
            return boolType;
        }

        /// <summary>
        /// Decode either `const XX*` or `XX*` to return XX, isPointer:true/false, isConst: true/false, with element type `XX`
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isConst"></param>
        /// <param name="isPointer"></param>
        /// <param name="elementType"></param>
        private static void DecodeSimpleType(CppType type, out bool isConst, out bool isPointer, out CppType elementType)
        {
            isConst = false;
            isPointer = false;
            elementType = type;

            if (elementType is CppPointerType pointerType)
            {
                isPointer = true;
                elementType = pointerType.ElementType;
            }

            if (elementType is CppQualifiedType qualifiedType)
            {
                isConst = qualifiedType.Qualifier == CppTypeQualifier.Const;
                elementType = qualifiedType.ElementType;
            }
        }
    }
}