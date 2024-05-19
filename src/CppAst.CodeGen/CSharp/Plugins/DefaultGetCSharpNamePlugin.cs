// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.IO;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultGetCSharpNamePlugin : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpNameResolvers.Add(DefaultGetCSharpName);
        }

        public static string DefaultGetCSharpName(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            var name = string.Empty;

            // Try to get the name directly from the CppElement
            if (element is ICppMember member)
            {
                name = member.Name;
            }

            // If it is null, try to get a contextual name from the context
            if (string.IsNullOrEmpty(name))
            {
                if (context is ICSharpMember csMember)
                {
                    name = csMember.Name;
                }

                if (!string.IsNullOrEmpty(name))
                {
                    // Handle the case for union types where the field name is auto-generated from the union name
                    // and could have a conflict with it. Add the index of the field to the name to avoid conflicts
                    if (context.CppElement is ICppDeclarationContainer cppContainer)
                    {
                        int indexOfElement;
                        string? kind = "";
                        if (cppContainer is CppClass cppClass)
                        {
                            kind = $"{cppClass.ClassKind.ToString().ToLowerInvariant()}_";
                        }

                        if (element is CppField cppField)
                        {
                            indexOfElement = cppContainer.Fields.IndexOf(cppField);
                        }
                        else if (element is CppClass subclass)
                        {
                            indexOfElement = cppContainer.Classes.IndexOf(subclass);
                        }
                        else if (element is CppFunction cppFunction)
                        {
                            indexOfElement = cppContainer.Functions.IndexOf(cppFunction);
                        }
                        else if (element is CppEnum cppEnum)
                        {
                            indexOfElement = cppContainer.Enums.IndexOf(cppEnum);
                        }
                        else if (element is CppTypedef cppTypedef)
                        {
                            indexOfElement = cppContainer.Typedefs.IndexOf(cppTypedef);
                        }
                        else
                        {
                            indexOfElement = -1;
                        }

                        if (indexOfElement >= 0)
                        {
                            name = $"{name}__{kind}{indexOfElement}";
                        }
                    }
                }
            }

            // If the name is null, we create an anonymous type name that includes the type, file name, and file offset
            if (string.IsNullOrEmpty(name))
            {
                var fileName = Path.GetFileNameWithoutExtension(element.Span.Start.File);
                name = $"__Anonymous{element.GetType().Name}_{fileName}_{element.Span.Start.Offset}";
            }

            return name;
        }
    }
}