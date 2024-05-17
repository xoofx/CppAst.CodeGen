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
        private static readonly string CacheContainerKey = typeof(DefaultContainerResolver) + "." + nameof(CacheContainerKey);

        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpContainerResolvers.Add(GetSharpContainer);
        }

        public static ICSharpContainer? GetSharpContainer(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            var cacheContainer = converter.GetTagValueOrDefault<CacheContainer>(CacheContainerKey);

            if (cacheContainer == null)
            {
                cacheContainer = new CacheContainer { DefaultClass = CreateClassLib(converter) };
                converter.Tags[CacheContainerKey] = cacheContainer;
            }

            if (converter.Options.DispatchOutputPerInclude)
            {
                var isFromSystemIncludes = converter.IsFromSystemIncludes(element);

                if (!isFromSystemIncludes)
                {
                    var filePath = element.Span.Start.File;
                    string? filePathRelative = null;
                    if (filePath is not null)
                    {
                        filePath = Path.GetFullPath(filePath);
                        foreach (var folder in converter.Options.IncludeFolders)
                        {
                            var folderPath = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
                            if (filePath.StartsWith(folderPath))
                            {
                                filePathRelative = filePath.Substring(folderPath.Length);
                                break;
                            }
                        }
                    }

                    if (filePathRelative is null)
                    {
                        return cacheContainer.DefaultClass;
                    }
                    else
                    {
                        var dir = Path.GetDirectoryName(filePathRelative);
                        filePathRelative = string.IsNullOrEmpty(dir) ? Path.GetFileNameWithoutExtension(filePathRelative) : Path.Combine(dir, Path.GetFileNameWithoutExtension(filePathRelative));
                    }
                    filePathRelative = filePathRelative.Replace('\\', '/');

                    if (filePathRelative.Contains(":"))
                    {

                    }

                    if (cacheContainer.IncludeToClass.TryGetValue(filePathRelative, out var csClassLib))
                    {
                        return csClassLib;
                    }

                    csClassLib = CreateClassLib(converter, UPath.Combine(UPath.Root, filePathRelative + ".generated.cs"));
                    cacheContainer.IncludeToClass.Add(filePathRelative, csClassLib);
                    return csClassLib;
                }
            }

            return cacheContainer.DefaultClass;
        }

        private static CSharpClass CreateClassLib(CSharpConverter converter, UPath? subFilePathOverride = null)
        {
            var compilation = converter.CurrentCSharpCompilation!;

            var path = converter.Options.DefaultOutputFilePath;

            if (subFilePathOverride != null)
            {
                path = UPath.Combine(converter.Options.DefaultOutputFilePath.GetDirectory(), subFilePathOverride.Value);
            }

            var csFile = new CSharpGeneratedFile(path);
            compilation.Members.Add(csFile);

            var csNamespace = new CSharpNamespace(converter.Options.DefaultNamespace);
            csFile.Members.Add(csNamespace);

            var csClassLib = new CSharpClass(converter.Options.DefaultClassLib);
            csClassLib.Modifiers |= CSharpModifiers.Partial | CSharpModifiers.Static | CSharpModifiers.Unsafe;
            converter.ApplyDefaultVisibility(csClassLib, csNamespace);

            csNamespace.Members.Add(csClassLib);

            converter.CurrentCSharpCompilation!.AllClasses.Add(csClassLib);
            
            return csClassLib;
        }

        private class CacheContainer
        {
            public CSharpClass? DefaultClass { get; set; }

            public Dictionary<string, CSharpClass> IncludeToClass { get; } = new Dictionary<string, CSharpClass>();
        }
    }
}