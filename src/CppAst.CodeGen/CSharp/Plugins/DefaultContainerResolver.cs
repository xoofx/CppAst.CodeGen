// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Zio;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultContainerResolver : ICSharpConverterPlugin
    {
        private static readonly string CacheContainerKey = $"{typeof(DefaultContainerResolver)}.{nameof(CacheContainerKey)}";

        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpContainerResolvers.Add(GetSharpContainer);
        }

        public static ICSharpContainer GetSharpContainer(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            var cacheContainer = converter.GetTagValueOrDefault<CacheContainer>(CacheContainerKey);

            if (cacheContainer == null)
            {
                cacheContainer = new CacheContainer { DefaultContainer = CreateContainer(converter, element) };
                converter.Tags[CacheContainerKey] = cacheContainer;
            }

            if (converter.Options.DispatchOutputPerInclude &&
                !converter.IsFromSystemIncludes(element))
            {
                var fileName = Path.GetFileNameWithoutExtension(element.Span.Start.File);

                if (fileName != null)
                {
                    if (cacheContainer.IncludeToContainer.TryGetValue(fileName, out var cSharpContainer))
                    {
                        return cSharpContainer;
                    }

                    cSharpContainer = CreateContainer(converter, element, UPath.Combine(UPath.Root, $"{CSharpHelper.ToPascal(fileName)}.generated.cs"), fileName);
                    cacheContainer.IncludeToContainer.Add(fileName, cSharpContainer);
                    return cSharpContainer;
                }
            }

            return cacheContainer.DefaultContainer;
        }

        private static ICSharpContainer CreateContainer(CSharpConverter converter, CppElement element, UPath? subFilePathOverride = null, string nameOverride = "")
        {
            var path = converter.Options.DefaultOutputFilePath;
            var compilation = converter.CurrentCSharpCompilation;

            if (subFilePathOverride != null)
            {
                path = UPath.Combine(converter.Options.DefaultOutputFilePath.GetDirectory(), subFilePathOverride.Value);
            }

            var csFile = new CSharpGeneratedFile(path);
            compilation.Members.Add(csFile);

            var csNamespace = new CSharpNamespace(converter.Options.DefaultNamespace);
            csFile.Members.Add(csNamespace);

            var csClassName = string.IsNullOrWhiteSpace(nameOverride)
                ? converter.Options.DefaultClassLib
                : CSharpHelper.ToPascal(nameOverride);
            CSharpTypeWithMembers container = new CSharpClass(csClassName);
            container.Modifiers |= CSharpModifiers.Partial | CSharpModifiers.Static;
            converter.ApplyDefaultVisibility(container, csNamespace);

            csNamespace.Members.Add(container);
            return container;
        }

        private class CacheContainer
        {
            public CacheContainer()
            {
                IncludeToContainer = new Dictionary<string, ICSharpContainer>();
            }

            public ICSharpContainer DefaultContainer { get; set; }

            public Dictionary<string, ICSharpContainer> IncludeToContainer { get; }
        }
    }
}