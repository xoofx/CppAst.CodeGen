// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ClangSharp;
using ClangSharp.Interop;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultTypeConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpTypeResolvers.Add(GetCSharpType);
        }

        public static CSharpType? GetCSharpType(CSharpConverter converter, CppType cppType, CSharpElement context, bool nested)
        {
            // Early exit for primitive types
            if (cppType is CppPrimitiveType cppPrimitiveType)
            {
                return GetCSharpPrimitiveType(converter, cppPrimitiveType);
            }

            // Check if a particular CppType has been already converted
            var csType = converter.FindCSharpType(cppType);
            if (csType != null)
            {
                return csType;
            }

            DecodeSimpleType(cppType, out var isConst, out var isPointer, out var isOpaqueStructElementType, out var elementType);

            var isParamFromFunctionOrFunctionPointer = !nested && context is CSharpParameter;
            var isReturnFromFunctionOrFunctionPointer = !nested && (context is CSharpMethod || context is CSharpFunctionPointer);
            var isParam = !nested && context is CSharpParameter csParam && csParam.Parent is CSharpMethod;
            var isReturn = !nested && context is CSharpMethod;
            var isConstField = context is CSharpField ctxCsField && (ctxCsField.Modifiers & CSharpModifiers.Const) != 0;

            if (isConst && isPointer && !nested && elementType.Equals(CppPrimitiveType.Char) && (isParam || isReturn || isConstField))
            {
                // const char* => string (with marshal)
                csType = GetStringType(converter, isReturn ? CppStringUsage.Return : isConstField ? CppStringUsage.Const : CppStringUsage.Parameter );
            }

            if (csType == null)
            {
                switch (cppType.TypeKind)
                {
                    case CppTypeKind.Pointer:
                        var csElementType = converter.GetCSharpType(((CppPointerType)cppType).ElementType, context, true)!;
                        if ((converter.Options.DetectOpaquePointers && isOpaqueStructElementType) || csElementType is CSharpFunctionPointer)
                        {
                            csType = csElementType;
                        }
                        else
                        {
                            var manualRefKind = converter.CurrentParameterRefKind;
                            // Handle auto-ref or manual ref kind
                            if (isParam && (converter.Options.EnableAutoByRef || manualRefKind.HasValue))
                            {
                                // Reset the state
                                converter.CurrentParameterRefKind = null;

                                if (manualRefKind.HasValue)
                                {
                                    if (manualRefKind.Value != CSharpRefKind.None)
                                    {
                                        csType = new CSharpRefType(manualRefKind.Value, csElementType);
                                    }
                                }
                                else
                                {
                                    if (isConst && elementType.TypeKind == CppTypeKind.StructOrClass)
                                    {
                                        csType = new CSharpRefType(CSharpRefKind.In, csElementType);
                                    }
                                    else if (csElementType is CSharpStruct ||
                                             csElementType is CSharpPointerType ||
                                             csElementType is CSharpEnum ||
                                             (csElementType is CSharpPrimitiveType primitiveType && (primitiveType.Kind != CSharpPrimitiveKind.Void && primitiveType.Kind != CSharpPrimitiveKind.Byte))
                                            )
                                    {
                                        csType = new CSharpRefType(CSharpRefKind.Ref, csElementType);
                                    }
                                }
                            }

                            csType ??= new CSharpPointerType(csElementType);
                        }
                        break;
                    case CppTypeKind.Reference:
                        csType = new CSharpRefType(CSharpRefKind.Ref, converter.GetCSharpType(((CppReferenceType)cppType).ElementType, context, true)!);
                        break;
                    case CppTypeKind.Array:
                        var arrayType = (CppArrayType)cppType;
                        var arrayElementType = arrayType.ElementType;
                        var canonicalElementType = arrayElementType.GetCanonicalType();
                        var isPointerElementType = IsPointerType(canonicalElementType);
                        if (converter.Options.AllowFixedSizeBuffers && 
                            context is CSharpField csField && 
                            ((canonicalElementType is CppPrimitiveType cppPrimitive && 
                              cppPrimitive.Kind != CppPrimitiveKind.Bool && 
                              ((cppPrimitive.Kind != CppPrimitiveKind.Long && cppPrimitive.Kind != CppPrimitiveKind.UnsignedLong)))
                            )
                            )
                        {
                            var csParent = (CSharpTypeWithMembers)csField.Parent!;
                            csParent.Modifiers |= CSharpModifiers.Unsafe;

                            var csArrayElementType = converter.GetCSharpType(cppPrimitive, context, true)!;
                            var size = arrayType.Size;
                            if (size <= 0)
                            {
                                size = 1;
                            }

                            csType = new CSharpFixedArrayType(csArrayElementType, size);
                        }
                        else
                        {
                            if (arrayType.Size > 0)
                            {
                                var csArrayElementType = isPointerElementType ? CSharpPrimitiveType.IntPtr() : converter.GetCSharpType(arrayElementType, context, true)!;
                                csType = new CSharpGenericTypeReference($"{converter.Options.FixedArrayPrefix}{arrayType.Size}", [csArrayElementType]);
                            }
                            else
                            {
                                var csArrayElementType = converter.GetCSharpType(arrayElementType, context, true)!;
                                csType = new CSharpPointerType(csArrayElementType);
                            }
                        }
                        break;
                    case CppTypeKind.Qualified:
                        var qualifiedType = (CppQualifiedType)cppType;
                        csType = converter.GetCSharpType(qualifiedType.ElementType, context, true)!;
                        break;
                    case CppTypeKind.Function:
                    case CppTypeKind.Typedef:
                    case CppTypeKind.StructOrClass:
                    case CppTypeKind.Enum:
                        csType = converter.ConvertType(cppType, context);
                        break;
                    case CppTypeKind.TemplateParameterType:
                        break;
                    case CppTypeKind.TemplateParameterNonType:
                        break;
                    case CppTypeKind.TemplateArgumentType:
                        break;
                    case CppTypeKind.Unexposed:
                        break;
                }
            }

            return csType;
        }
        
        public static CSharpType GetCSharpPrimitiveType(CSharpConverter converter, CppPrimitiveType cppPrimitiveType)
        {
            CSharpType? csType = null;

            if (!converter.Options.DisableRuntimeMarshalling && converter.Options.DefaultMarshalForBool != null && cppPrimitiveType.Kind == CppPrimitiveKind.Bool)
            {
                csType = CSharpPrimitiveType.Bool();
                var boolTypeWithMarshal = new CSharpTypeWithAttributes(csType);
                boolTypeWithMarshal.Attributes.Add(converter.Options.DefaultMarshalForBool.Clone());
                csType = boolTypeWithMarshal;
            }
            else if (converter.Options.CharAsByte && cppPrimitiveType.Kind == CppPrimitiveKind.Char)
            {
                csType = CSharpPrimitiveType.Byte();
            }

            csType ??= CSharpHelper.GetCSharpPrimitive(converter, cppPrimitiveType);
            return csType;
        }

        private static CSharpType? GetStringType(CSharpConverter converter, CppStringUsage usage)
        {
            if (!converter.Options.AllowMarshalForString && usage != CppStringUsage.Const)
            {
                return null;
            }

            CSharpType strType = usage == CppStringUsage.Parameter && converter.Options.ManagedToUnmanagedStringTypeForParameter != null ? (CSharpType)new CSharpFreeType(converter.Options.ManagedToUnmanagedStringTypeForParameter) : CSharpPrimitiveType.String();
            if (usage != CppStringUsage.Const && converter.Options.DefaultMarshalForString != null)
            {
                var strTypeWithMarshal = new CSharpTypeWithAttributes(strType);
                var attr = converter.Options.DefaultMarshalForString.Clone();
                if (usage == CppStringUsage.Return)
                {
                    attr.Scope = CSharpAttributeScope.Return;
                }
                strTypeWithMarshal.Attributes.Add(attr);
                strType = strTypeWithMarshal;
            }
            return strType;
        }

        enum CppStringUsage
        {
            Field,
            Parameter,
            Return,
            Const
        }

        /// <summary>
        /// Decode either `const XX*` or `XX*` to return XX, isPointer:true/false, isConst: true/false, with element type `XX`
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isConst"></param>
        /// <param name="isPointer"></param>
        /// <param name="isOpaqueElementType"></param>
        /// <param name="elementType"></param>
        public static void DecodeSimpleType(CppType type, out bool isConst, out bool isPointer, out bool isOpaqueElementType, out CppType elementType)
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

            var canonicalElementType = elementType.GetCanonicalType();
            isOpaqueElementType = isPointer && canonicalElementType is CppClass cppClass && cppClass.ClassKind == CppClassKind.Struct && !cppClass.IsDefinition && cppClass.Fields.Count == 0;
        }

        private static bool IsPointerType(CppType type)
        {
            return type is CppPointerType || (type is CppQualifiedType qualifiedType && qualifiedType.ElementType is CppPointerType);
        }
    }
}

