// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class DefaultContainerResolver: ICSharpConverterPlugin
    {
        private static readonly string DefaultClassLibKey = typeof(DefaultContainerResolver) + "." + nameof(DefaultClassLibKey);

        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpContainerResolvers.Add(GetSharpContainer);
        }

        public static ICSharpContainer GetSharpContainer(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            var compilation = converter.CurrentCSharpCompilation;

            if (converter.Tags.TryGetValue(DefaultClassLibKey, out var container))
            {
                return (ICSharpContainer) container;
            }
            
            var csFile = new CSharpGeneratedFile(converter.Options.DefaultOutputFilePath);
            compilation.Members.Add(csFile);

            var csNamespace = new CSharpNamespace(converter.Options.DefaultNamespace);
            csFile.Members.Add(csNamespace);

            var csClassLib = new CSharpClass(converter.Options.DefaultClassLib);
            csClassLib.Modifiers |= CSharpModifiers.Partial | CSharpModifiers.Static;
            converter.ApplyDefaultVisibility(csClassLib, csNamespace);

            csNamespace.Members.Add(csClassLib);

            converter.Tags[DefaultClassLibKey] = csClassLib;

            return csClassLib;
        }
    }
}