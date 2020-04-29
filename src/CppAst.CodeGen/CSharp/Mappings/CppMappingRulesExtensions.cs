// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
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

        public static CppElementMappingRule Discard(this CppElementMappingRule mappingRule)
        {
            mappingRule.CppElementActions.Add((converter, element, context, matches) =>
            {
                converter.Discard(element);
            });
            return mappingRule;
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
            mappingRule.TypeRemap = type ?? throw new ArgumentNullException(nameof(type));
            mappingRule.TypeRemapArraySize = arraySize;

            mappingRule.CppElementActions.Add((converter, element, context, matches) =>
            {
                var remapType = DefaultMappingRulesConverter.GetCppTypeRemap(converter, mappingRule.TypeRemap, mappingRule.TypeRemapArraySize);
                if (remapType == null) return;

                switch (element)
                {
                    case CppField cppField:
                        cppField.Type = remapType;
                        break;
                    case CppParameter cppParameter:
                        cppParameter.Type = remapType;
                        break;
                    case CppFunction cppFunction:
                        cppFunction.ReturnType = remapType;
                        break;
                }
            });

            return mappingRule;
        }

        public static CppElementMappingRule DllImportLibrary(this CppElementMappingRule mappingRule, string dllImportName, string headerFileName)
        {
            bool isHeaderMatch = false;
            mappingRule.CppElementActions.Add(CppRule);
            mappingRule.CSharpElementActions.Add(CsRule);

            void CppRule(CSharpConverter converter, CppElement cppElement, CSharpElement context, List<ICppElementMatch> matches)
            {
                if (!(context is CSharpMethod)) { return; }

                var fileName = Path.GetFileName(cppElement.SourceFile);

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                isHeaderMatch = fileName == headerFileName;
            }

            void CsRule(CSharpConverter converter, CSharpElement csElement, List<ICppElementMatch> matches)
            {
                if (!(csElement is CSharpMethod csMethod) || !isHeaderMatch) { return; }

                foreach (var attribute in csMethod.Attributes)
                {
                    if (attribute is CSharpDllImportAttribute dllImportAttribute)
                    {
                        dllImportAttribute.DllName = dllImportName;
                        break;
                    }
                }
            }

            return mappingRule;
        }

        public static CppElementMappingRule InitValue(this CppElementMappingRule mappingRule, string value)
        {
            mappingRule.CSharpElementActions.Add((converter, element, matches) =>
            {
                if (element is CSharpField csField)
                {
                    csField.InitValue = value;
                }

                if (element is CSharpParameter csParam)
                {
                    csParam.DefaultValue = value;
                }
            });

            return mappingRule;
        }

        public static CppElementMappingRule MarshalAs(this CppElementMappingRule mappingRule, CSharpUnmanagedKind unmanagedKind)
        {
            return MarshalAs(mappingRule, new CSharpMarshalAttribute(unmanagedKind));
        }

        public static CppElementMappingRule MarshalAs(this CppElementMappingRule mappingRule, CSharpMarshalAttribute marshalAttribute, bool cloneAttribute = true)
        {
            if (marshalAttribute == null) throw new ArgumentNullException(nameof(marshalAttribute));

            var clonedAttribute = cloneAttribute ? marshalAttribute.Clone() : marshalAttribute;

            mappingRule.CSharpElementActions.Add((converter, element, matches) =>
            {
                var csField = element as CSharpField;
                var csParam = element as CSharpParameter;
                var csMethod = element as CSharpMethod;
                if (csField == null && csParam == null && csMethod == null) return;

                var type = csField?.FieldType ?? csParam?.ParameterType ?? csMethod?.ReturnType;
                // Should not happen, but in case 
                if (type == null) return;

                if (type is CSharpTypeWithAttributes cppTypeWithAttributes)
                {
                    for (var i = cppTypeWithAttributes.Attributes.Count - 1; i >= 0; i--)
                    {
                        var attr = cppTypeWithAttributes.Attributes[i];
                        if (attr is CSharpMarshalAttribute)
                        {
                            cppTypeWithAttributes.Attributes.RemoveAt(i);
                            cppTypeWithAttributes.Attributes.Insert(i, clonedAttribute);
                            return;
                        }
                    }
                    cppTypeWithAttributes.Attributes.Add(clonedAttribute);
                }
                else
                {
                    var typeWithAttributes = new CSharpTypeWithAttributes(type);
                    typeWithAttributes.Attributes.Add(clonedAttribute);
                    if (csField != null) csField.FieldType = typeWithAttributes;
                    else if (csParam != null) csParam.ParameterType = typeWithAttributes;
                    else csMethod.ReturnType = typeWithAttributes;
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

        public static CppMacroToConstMappingRule MapMacroToConst(this CppMappingRules dispatcher, string cppRegexMatchMacroName, string cppType, bool explicitCast = false, string enumItemName = null, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            return new CppMacroToConstMappingRule(new CppElementRegexMatcher(cppRegexMatchMacroName))
            {
                ConstFieldTypeName = cppType,
                ConstFieldName = enumItemName,
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber,
                ExplicitCast = explicitCast,
            };
        }

        public static CppMacroToEnumMappingRule MapMacroToEnum(this CppMappingRules dispatcher, string cppRegexName, string cppEnumTypeName, string cppEnumItemName = null, string integerType = "int", bool explicitCast = false, [CallerFilePath] string mapOriginFilePath = null, [CallerLineNumber] int mapLineNumber = 0)
        {
            return new CppMacroToEnumMappingRule(new CppElementRegexMatcher(cppRegexName))
            {
                CppEnumTypeName = cppEnumTypeName,
                CppEnumItemName = cppEnumItemName,
                CppIntegerTypeName = integerType,
                DeclarationFileName = mapOriginFilePath,
                DeclarationLineNumber = mapLineNumber,
                ExplicitCast = explicitCast,
            };
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