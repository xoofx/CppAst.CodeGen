// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultTypedefConverter : ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.TypedefConverters.Add(ConvertTypedef);
        }

        private CSharpElement ConvertTypedef(CSharpConverter converter, CppTypedef cppTypedef, CSharpElement context)
        {
            var elementType = cppTypedef.ElementType;

            if (DefaultFunctionTypeConverter.IsFunctionType(elementType, out var cppFunctionType))
            {
                return DefaultFunctionTypeConverter.ConvertNamedFunctionType(converter, cppFunctionType, context, cppTypedef.Name);
            }

            return converter.GetCSharpType(elementType, context);
        }
    }
}