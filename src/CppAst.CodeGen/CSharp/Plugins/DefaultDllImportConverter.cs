// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.ComponentModel;
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
                (method.Modifiers & (CSharpModifiers.Extern | CSharpModifiers.Partial)) == 0 ||
                (converter.Options.UseLibraryImport && method.Attributes.OfType<CSharpLibraryImportAttribute>().Any()) || method.Attributes.OfType<CSharpDllImportAttribute>().Any())
            {
                return;
            }

            CallingConvention csCallingConvention;

            if (converter.Options.DefaultCallingConvention.HasValue)
            {
                csCallingConvention = converter.Options.DefaultCallingConvention.Value;
            }
            else
            {
                var callingConvention = (method.CppElement as CppFunction)?.CallingConvention ?? CppCallingConvention.Default;
                csCallingConvention = callingConvention.GetCSharpCallingConvention();
            }
            
            var name = converter.Options.DefaultDllImportNameAndArguments ?? "LibNativeName";

            if (converter.Options.UseLibraryImport)
            {
                method.Modifiers &= ~CSharpModifiers.Extern;
                method.Modifiers |= CSharpModifiers.Partial;
                method.Attributes.Add(new CSharpLibraryImportAttribute(name) { EntryPoint = $"\"{method.Name}\"" });
                method.Attributes.Add(new CSharpUnmanagedCallingConventionAttribute(csCallingConvention));

                var container = converter.GetCSharpContainer(element)!;

                // Required by StructLayout
                converter.AddUsing(container, "System.Runtime.CompilerServices");
            }
            else
            {
                method.Attributes.Add(new CSharpDllImportAttribute(name) { CallingConvention = csCallingConvention });
            }
        }
    }
}