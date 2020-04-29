// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultFunctionConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.FunctionConverters.Add(ConvertFunction);
        }

        public static CSharpElement ConvertFunction(CSharpConverter converter, CppFunction cppFunction, CSharpElement context)
        {
            // We process only public export functions
            if (!cppFunction.IsPublicExport() || ((cppFunction.Flags & (CppFunctionFlags.Inline | CppFunctionFlags.Method | CppFunctionFlags.Constructor | CppFunctionFlags.Destructor)) != 0 && (cppFunction.Flags & CppFunctionFlags.Virtual) == 0))
            {
                return null;
            }

            // Register the struct as soon as possible
            var csFunction = new CSharpMethod() { CppElement = cppFunction };

            var container = converter.GetCSharpContainer(cppFunction, context);

            converter.ApplyDefaultVisibility(csFunction, container);
            container.Members.Add(csFunction);

            converter.AddUsing(container, "System.Runtime.InteropServices");

            if ((cppFunction.Flags & CppFunctionFlags.Virtual) == 0)
            {
                csFunction.Modifiers |= CSharpModifiers.Static | CSharpModifiers.Extern;
            }
            else
            {
                csFunction.Visibility = CSharpVisibility.None;
            }
            csFunction.Name = converter.GetCSharpName(cppFunction, csFunction);
            csFunction.Comment = converter.GetCSharpComment(cppFunction, csFunction);
            csFunction.ReturnType = converter.GetCSharpType(cppFunction.ReturnType, csFunction);

            return csFunction;
        }
    }
}