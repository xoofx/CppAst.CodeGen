// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultFunctionTypeConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.FunctionTypeConverters.Add(ConvertFunctionType);
        }

        public static CSharpType ConvertFunctionType(CSharpConverter converter, CppFunctionType cppFunctionType, CSharpElement context)
        {
            if (cppFunctionType == null) throw new ArgumentNullException(nameof(cppFunctionType));

            var returnType = converter.GetCSharpType(cppFunctionType.ReturnType, context);
            
            var csFunctionPointer = new CSharpFunctionPointer(returnType)
            {
                CppElement = cppFunctionType,
                IsUnmanaged = true,
            };

            // Add calling convention
            CallingConvention csCallingConvention = converter.Options.DefaultCallingConvention ?? cppFunctionType.CallingConvention.GetCSharpCallingConvention();

            csFunctionPointer.UnmanagedCallingConvention.Add(csCallingConvention.GetUnmanagedCallConvType());
            
            ICSharpContainer container = converter.GetCSharpContainer(cppFunctionType, context)!;

            converter.AddUsing(container, "System.Runtime.InteropServices");

            return csFunctionPointer;
        }
    }
}