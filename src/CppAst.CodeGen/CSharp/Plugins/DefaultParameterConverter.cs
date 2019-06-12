// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultParameterConverter: ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.ParameterConverters.Add(ConvertParameter);
        }
        
        public static CSharpElement ConvertParameter(CSharpConverter converter, CppParameter cppParam, int index, CSharpElement context)
        {
            var parameters = (context as CSharpMethod)?.Parameters ?? (context as CSharpDelegate)?.Parameters;
            if (parameters == null)
            {
                return null;
            }

            var csParamName = converter.GetCSharpName(cppParam, context);
            var csParam = new CSharpParameter(csParamName) {CppElement = cppParam};
            parameters.Add(csParam);

            var csParamType = converter.GetCSharpType(cppParam.Type, context);
            csParam.Index = index;
            csParam.ParameterType = csParamType;

            return csParam;
        }
    }
}