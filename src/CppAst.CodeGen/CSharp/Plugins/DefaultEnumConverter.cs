// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultEnumConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.EnumConverters.Add(ConvertEnum);
        }

        public static CSharpElement? ConvertEnum(CSharpConverter converter, CppEnum cppEnum, CSharpElement context)
        {
            // We don't support generate anonymous enums
            if (cppEnum.IsAnonymous && context is CSharpCompilation)
            {
                return null;
            }

            var enumName = converter.GetCSharpName(cppEnum, context);

            var csEnum = new CSharpEnum(enumName)
            {
                CppElement = cppEnum
            };

            var container = converter.GetCSharpContainer(cppEnum, context);
            container.Members.Add(csEnum);

            converter.ApplyDefaultVisibility(csEnum, container);

            csEnum.Comment = converter.GetCSharpComment(cppEnum, csEnum);

            // We can only reason with a canonical type in C#
            // while in C++ you could use a typedef or a using alias
            var canonicalType = GetCanonicalIntegerBaseType(converter, cppEnum);
            csEnum.BaseTypes.Add(converter.GetCSharpType(canonicalType, csEnum));

            return csEnum;
        }

        private static CppType GetCanonicalIntegerBaseType(CSharpConverter converter, CppEnum cppEnum)
        {
            var canonicalType = cppEnum.IntegerType;
            var visitedTypes = new HashSet<CppType>();
            while (visitedTypes.Add(canonicalType))
            {
                var nextType = canonicalType.GetCanonicalType();
                if (!ReferenceEquals(nextType, canonicalType))
                {
                    canonicalType = nextType;
                    continue;
                }

                if (canonicalType is CppTypedef typedef)
                {
                    canonicalType = typedef.ElementType;
                    continue;
                }

                var resolvedTypedef = canonicalType is CppUnexposedType unexposedType ? FindTypedef(converter, cppEnum, unexposedType.Name) : null;
                if (resolvedTypedef is not null)
                {
                    canonicalType = resolvedTypedef.ElementType;
                    continue;
                }

                break;
            }

            return canonicalType;
        }

        private static CppTypedef? FindTypedef(CSharpConverter converter, CppEnum cppEnum, string typeName)
        {
            if (converter.CurrentCppCompilation is { } compilation && typeName.Contains("::"))
            {
                var typedef = compilation.FindByFullName(typeName) as CppTypedef;
                if (typedef is not null)
                {
                    return typedef;
                }
            }

            var container = cppEnum.Parent;
            while (container is not null)
            {
                if (container is ICppDeclarationContainer declarationContainer)
                {
                    foreach (var candidate in declarationContainer.Typedefs)
                    {
                        if (candidate.Name == typeName)
                        {
                            return candidate;
                        }
                    }
                }

                container = (container as CppElement)?.Parent;
            }

            return null;
        }
    }
}
