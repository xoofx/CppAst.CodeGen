// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultFunctionTypeConverter : ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.FunctionTypeConverters.Add(ConvertAnonymousFunctionType);
        }

        public static bool IsFunctionType(CppType type, out CppFunctionType cppFunctionType)
        {
            type = type.GetCanonicalType();
            cppFunctionType = type as CppFunctionType;
            if (cppFunctionType == null)
            {
                if (type is CppPointerType ptrType && (ptrType.ElementType is CppFunctionType cppFunctionType2))
                {
                    cppFunctionType = cppFunctionType2;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static CSharpElement ConvertAnonymousFunctionType(CSharpConverter converter, CppFunctionType cppfunctiontype, CSharpElement context)
        {
            return ConvertNamedFunctionType(converter, cppfunctiontype, context, null);
        }

        public static CSharpType ConvertNamedFunctionType(CSharpConverter converter, CppFunctionType cppType, CSharpElement context, string name)
        {
            if (cppType == null) throw new ArgumentNullException(nameof(cppType));

            if (name == null)
            {
                // Create a contextual name
                var cppElement = context.CppElement;
                while (cppElement != null)
                {
                    name = converter.GetCSharpName(cppElement, context, string.Empty);
                    if (!string.IsNullOrEmpty(name))
                    {
                        break;
                    }
                    cppElement = (CppElement)cppElement.Parent;
                }

                name = name != null ? name + "Delegate" : "Delegate";
            }

            var csDelegate = new CSharpDelegate(name) { CppElement = cppType };

            var cppFunctionType = (CppFunctionType)cppType;

            // Add calling convention
            var csCallingConvention = cppFunctionType.CallingConvention.GetCSharpCallingConvention();
            csDelegate.Attributes.Add(new CSharpFreeAttribute($"UnmanagedFunctionPointer(CallingConvention.{csCallingConvention})"));

            var container = converter.GetCSharpContainer(cppFunctionType, context);

            converter.ApplyDefaultVisibility(csDelegate, container);
            container.Members.Add(csDelegate);

            csDelegate.Comment = converter.GetCSharpComment(cppFunctionType, csDelegate);
            csDelegate.ReturnType = converter.GetCSharpType(cppFunctionType.ReturnType, csDelegate);

            return csDelegate;
        }
    }
}