// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace CppAst.CodeGen.CSharp
{
    public static class CppMappingRulesExtensions
    {
        public static TMatch FindMatch<TMatch>(this List<ICppElementMatch> matches) where TMatch : class
        {
            foreach (var cppElementMatch in matches)
            {
                if (cppElementMatch is TMatch match)
                {
                    return match;
                }
            }
            return null;
        }

        public static CppElementMappingRule Name(this CppElementMappingRule mappingRule, string replaceName)
        {
            mappingRule.CppElementActions.Add((converter, element, context, matches) =>
            {
                if (!(element is ICppMember cppMember)) return;

                var match = matches.FindMatch<CppElementRegexMatch>();
                if (match?.RegexInput != null)
                {
                    cppMember.Name = Regex.Replace(match.RegexInput, match.RegexPattern, replaceName);
                }
                else
                {
                    cppMember.Name = replaceName;
                }
            });
            return mappingRule;
        }

        public static CppElementMappingRule Type(this CppElementMappingRule mappingRule, string type, int? arraySize = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            mappingRule.TypeRemap = type;
            mappingRule.TypeRemapArraySize = arraySize;

            mappingRule.CppElementActions.Add((converter, element, context, matches) =>
            {
                var remapType = DefaultMappingRulesConverter.GetCppTypeRemap(converter, mappingRule.TypeRemap, mappingRule.TypeRemapArraySize);
                if (remapType == null) return;

                if (element is CppField cppField)
                {
                    cppField.Type = remapType;
                }

                if (element is CppParameter cppParameter)
                {
                    cppParameter.Type = remapType;
                }
            });

            return mappingRule;
        }

        public static CppElementMappingRule Visibility(this CppElementMappingRule mappingRule, CSharpVisibility visibility)
        {
            mappingRule.CSharpElementActions.Add((converter, csElement, match) =>
            {
                if (!(csElement is ICSharpElementWithVisibility csElementWithVisibility)) return;

                csElementWithVisibility.Visibility = visibility;
            });
            return mappingRule;
        }

        public static CppElementMappingRule Public(this CppElementMappingRule mappingRule)
        {
            return Visibility(mappingRule, CSharpVisibility.Public);
        }

        public static CppElementMappingRule Private(this CppElementMappingRule mappingRule)
        {
            return Visibility(mappingRule, CSharpVisibility.Private);
        }

        public static CppElementMappingRule Protected(this CppElementMappingRule mappingRule)
        {
            return Visibility(mappingRule, CSharpVisibility.Protected);
        }

        public static CppElementMappingRule Internal(this CppElementMappingRule mappingRule)
        {
            return Visibility(mappingRule, CSharpVisibility.Internal);
        }

        public static CppElementMappingRule CppAction(this CppElementMappingRule mappingRule, Action<CSharpConverter, CppElement> action)
        {
            mappingRule.CppElementActions.Add((converter, element, context, match) =>
            {
                action(converter, element);
            });
            return mappingRule;
        }

        public static CppElementMappingRule CSharpAction(this CppElementMappingRule mappingRule, Action<CSharpConverter, CSharpElement> action)
        {
            mappingRule.CSharpElementActions.Add((converter, csElement, match) =>
            {
                action(converter, csElement);
            });
            return mappingRule;
        }

        public static CppElementMappingRule Map(this CppMappingRules dispatcher, string cppRegexName, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            return new CppElementMappingRule(new CppElementRegexMatcher(cppRegexName))
            {
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber
            };
        }

        public static CppElementMappingRule MapAll<TCppElement>(this CppMappingRules dispatcher, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0) where TCppElement : CppElement
        {
            return new CppElementMappingRule(new CppElementTypeMatcher<TCppElement>())
            {
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber
            };
        }

        public static CppElementMappingRule Map<TCppElement>(this CppMappingRules dispatcher, string cppRegexName, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0) where TCppElement : CppElement
        {
            return new CppElementMappingRule(new CppElementTypeMatcher<TCppElement>(), new CppElementRegexMatcher(cppRegexName))
            {
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber
            };
        }

        public static CppElementMappingRule Map(this CppMappingRules dispatcher, Func<CppElement, bool> filter, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            throw new NotImplementedException();
        }

        public static CppElementMappingRule Map(this CppMappingRules dispatcher, Func<CppElement, bool> filter, List<Action<CppMappingRules>> subMappingRules, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            throw new NotImplementedException();
        }

        public static CppMacroToConstMappingRule MapMacroToConst(this CppMappingRules dispatcher, string cppRegexMatchMacroName, string cppType, string enumItemName = null, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            return new CppMacroToConstMappingRule(new CppElementRegexMatcher(cppRegexMatchMacroName))
            {
                ConstFieldTypeName = cppType,
                ConstFieldName = enumItemName,
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber
            };
        }

        public static CppMacroToEnumMappingRule MapMacroToEnum(this CppMappingRules dispatcher, string cppRegexName, string cppEnumTypeName, string cppEnumItemName = null, string integerType = "int", [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            var rule = new CppMacroToEnumMappingRule(new CppElementRegexMatcher(cppRegexName))
            {
                CppEnumTypeName = cppEnumTypeName,
                CppEnumItemName = cppEnumItemName,
                CppIntegerTypeName = integerType,
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber
            };
            return rule;
        }

        public static CppElementMappingRule MapType(this CppMappingRules dispatcher, string cppType, string csType, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            throw new NotImplementedException();
        }

        public static CppElementMappingRule MapArrayType(this CppMappingRules dispatcher, string cppElementType, int arraySize, string csType, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            throw new NotImplementedException();
        }
    }
}