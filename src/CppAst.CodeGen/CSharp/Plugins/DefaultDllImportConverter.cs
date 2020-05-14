// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultDllImportConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.Converted.Add(AddDefaultDllImport);
        }

        public static void AddDefaultDllImport(CSharpConverter converter, CSharpElement element, CSharpElement context)
        {
            if (!(element is CSharpMethod method) ||
                (method.Modifiers & CSharpModifiers.Extern) == 0 ||
                method.Attributes.OfType<CSharpDllImportAttribute>().Any())
            {
                return;
            }

            var callingConvention = (method.CppElement as CppFunction)?.CallingConvention ?? CppCallingConvention.Default;
            var csCallingConvention = callingConvention.GetCSharpCallingConvention();
            var name = converter.Options.DefaultDllImportNameAndArguments ?? "LibNativeName";
            method.Attributes.Add(new CSharpDllImportAttribute(name) { CallingConvention = csCallingConvention });
        }
    }
}