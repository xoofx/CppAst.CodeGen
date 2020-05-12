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
                var contextName = string.Empty;
                if (context is ICSharpMember csMember)
                {
                    contextName = csMember.Name;
                }

                if (!string.IsNullOrEmpty(contextName))
                {
                    name = contextName;
                }
            }

            // If the name is null, we create an anonymous type name that includes the type, file name, and file offset
            if (string.IsNullOrEmpty(name))
            {
                var fileName = Path.GetFileNameWithoutExtension(element.Span.Start.File);
                name = $"__Anonymous{element.GetType().Name}_{fileName}_{element.Span.Start.Offset}";
            }
            else if (element is CppType cppType && (!(element is ICppMember cppMember) || string.IsNullOrEmpty(cppMember.Name)))
            {
                switch (cppType)
                {
                    case CppClass cppClass:
                        name = CSharpHelper.AppendWithCasing(name, CSharpHelper.GetCSharpCasingKind(name), cppClass.ClassKind.ToString().ToLowerInvariant(), CSharpCasingKind.Lower);
                        break;
                    case CppFunctionType cppFunctionType:
                        name = CSharpHelper.AppendWithCasing(name, CSharpHelper.GetCSharpCasingKind(name), "delegate", CSharpCasingKind.Lower);
                        break;
                }
            }

            return name;
        }
    }
}