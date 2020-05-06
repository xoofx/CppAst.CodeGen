// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultMappingRulesConverter : ICSharpConverterPlugin
    {
        private const string CachedRulesKey = nameof(DefaultMappingRulesConverter) + "_" + nameof(CachedRulesKey);

        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            var cachedRules = GetCachedRules(converter);
            // Register to the pipeline only we have anything to process
            if (!cachedRules.IsEmpty)
            {
                if (cachedRules.MacroRules.Count > 0 || cachedRules.TypesToCompile.Count > 0)
                {
                    pipeline.AfterPreprocessing.Add(AfterPreprocessing);
                }

                pipeline.ConvertBegin.Add(ProcessTypedefs);
                pipeline.Converting.Add(ProcessCppElementMappingRules);
                pipeline.Converted.Add(ProcessCSharpElementMappingRules);
            }
        }

        private static void ProcessTypedefs(CSharpConverter converter)
        {
            var cachedRules = GetCachedRules(converter);

            if (cachedRules.TypesToCompile.Count > 0)
            {
                var matchTypeDef = new Regex($@"^{cachedRules.Prefix}(\d+)_typedef$");

                var typedefs = converter.CurrentCppCompilation.Typedefs;
                for (var i = typedefs.Count - 1; i >= 0; i--)
                {
                    var cppTypedef = typedefs[i];
                    var result = matchTypeDef.Match(cppTypedef.Name);

                    if (result.Success)
                    {
                        var typeIndex = int.Parse(result.Groups[1].Value);

                        var typeToCompile = cachedRules.TypesToCompile[typeIndex];
                        cachedRules.TypesCompiled[new TypeRemapKey(typeToCompile.TypeRemap, typeToCompile.TypeRemapArraySize)] = cppTypedef.ElementType;
                        typedefs.RemoveAt(i);
                    }
                }
            }
        }

        private void AfterPreprocessing(CSharpConverter converter, CppCompilation cppCompilation, StringBuilder additionalHeaders)
        {
            var cachedRules = GetCachedRules(converter);
            var macroRules = cachedRules.MacroRules;

            if (macroRules.Count > 0)
            {
                var matches = new List<ICppElementMatch>();
                var enumToMacros = new Dictionary<CppMacroToEnumMappingRule, StringBuilder>();

                foreach (var cppMacro in cppCompilation.Macros)
                {
                    if (cppMacro.Parameters != null)
                    {
                        continue;
                    }

                    foreach (var cppMacroRule in macroRules)
                    {
                        matches.Clear();

                        if (cppMacroRule.Match(cppMacro, matches))
                        {
                            var regexMatch = matches.FindMatch<CppElementRegexMatch>();

                            switch (cppMacroRule)
                            {
                                case CppMacroToConstMappingRule macroToConst:
                                    {
                                        AppendPragmaLine(cppMacroRule, additionalHeaders);

                                        var macroName = cppMacro.Name;
                                        if (regexMatch != null && macroToConst.ConstFieldName != null)
                                        {
                                            macroName = Regex.Replace(regexMatch.RegexInput, regexMatch.RegexPattern, macroToConst.ConstFieldName);
                                        }

                                        foreach (var token in cppMacro.Tokens)
                                        {
                                            if (token.Kind == CppTokenKind.Comment && !string.IsNullOrEmpty(token.Text))
                                            {
                                                additionalHeaders.AppendLine(token.Text);
                                            }
                                        }

                                        additionalHeaders.AppendLine(macroToConst.ExplicitCast
                                            ? $"const {macroToConst.ConstFieldTypeName} {cachedRules.Prefix}{macroName} = ({macroToConst.ConstFieldTypeName}){cppMacro.Value};"
                                            : $"const {macroToConst.ConstFieldTypeName} {cachedRules.Prefix}{macroName} = {cppMacro.Value};");
                                        break;
                                    }
                                case CppMacroToEnumMappingRule macroToEnum:
                                    {
                                        if (!enumToMacros.TryGetValue(macroToEnum, out var macrosAsEnumText))
                                        {
                                            macrosAsEnumText = new StringBuilder();

                                            var enumTypeName = macroToEnum.CppEnumTypeName;

                                            AppendPragmaLine(cppMacroRule, macrosAsEnumText);
                                            macrosAsEnumText.Append($"enum {enumTypeName}");
                                            if (macroToEnum.CppIntegerTypeName != "int")
                                            {
                                                macrosAsEnumText.Append(" : ").Append(macroToEnum.CppIntegerTypeName);
                                            }

                                            macrosAsEnumText.AppendLine();
                                            macrosAsEnumText.AppendLine("{");

                                            enumToMacros.Add(macroToEnum, macrosAsEnumText);
                                        }

                                        var enumItemName = macroToEnum.CppEnumItemName;
                                        if (regexMatch != null)
                                        {
                                            enumItemName = Regex.Replace(regexMatch.RegexInput, regexMatch.RegexPattern, enumItemName);
                                        }

                                        AppendPragmaLine(cppMacroRule, macrosAsEnumText);
                                        macrosAsEnumText.AppendLine(macroToEnum.ExplicitCast
                                            ? $"    {cachedRules.Prefix}{enumItemName} = ({macroToEnum.CppIntegerTypeName}){cppMacro.Value},"
                                            : $"    {cachedRules.Prefix}{enumItemName} = {cppMacro.Value},");

                                        break;
                                    }
                            }
                        }
                    }
                }

                foreach (var enumToMacroPair in enumToMacros)
                {
                    var enumDeclarationText = enumToMacroPair.Value.AppendLine("};");
                    additionalHeaders.AppendLine(enumDeclarationText.ToString());
                }
            }

            if (cachedRules.TypesToCompile.Count > 0)
            {
                for (var i = 0; i < cachedRules.TypesToCompile.Count; i++)
                {
                    var rule = cachedRules.TypesToCompile[i];
                    AppendPragmaLine(rule, additionalHeaders);

                    if (rule.TypeRemapArraySize.HasValue)
                    {
                        var value = rule.TypeRemapArraySize.Value;
                        additionalHeaders.AppendLine($"typedef {rule.TypeRemap} {cachedRules.Prefix}{i}_typedef[{(value < 0 ? string.Empty : value.ToString(CultureInfo.InvariantCulture))}];");
                    }
                    else
                    {
                        additionalHeaders.AppendLine($"typedef {rule.TypeRemap} {cachedRules.Prefix}{i}_typedef;");
                    }
                }
            }
        }

        private static void AppendPragmaLine(CppElementMappingRuleBase rule, StringBuilder builder)
        {
            builder.AppendLine($"#line {rule.DeclarationLineNumber} \"{rule.DeclarationFileName.Replace(@"\", @"\\")}\"");
        }

        private static void ProcessCppElementMappingRules(CSharpConverter converter, CppElement cppElement, CSharpElement context)
        {
            var cachedRules = GetCachedRules(converter);

            var rules = cachedRules.StandardRules;

            // If a CppElement starts with a Prefix, it was generated by AfterPreprocessing and need to be removed
            // entirely
            if (cppElement is ICppMember member && member.Name.StartsWith(cachedRules.Prefix))
            {
                member.Name = member.Name.Substring(cachedRules.Prefix.Length);
            }

            var matches = new List<ICppElementMatch>();
            foreach (var rule in rules)
            {
                matches.Clear();
                if (rule.Match(cppElement, matches))
                {
                    if (rule.CSharpElementActions.Count > 0)
                    {
                        // save the match for later
                        if (!cachedRules.ElementToMatches.TryGetValue(cppElement, out var listCppElementMatch))
                        {
                            listCppElementMatch = new List<CppElementMatch>();
                            cachedRules.ElementToMatches.Add(cppElement, listCppElementMatch);
                        }
                        listCppElementMatch.Add(new CppElementMatch(rule, new List<ICppElementMatch>(matches)));
                    }

                    foreach (var action in rule.CppElementActions)
                    {
                        action(converter, cppElement, context, matches);
                    }
                }
            }
        }

        internal static CppType GetCppTypeRemap(CSharpConverter converter, string typeName, int? typeRemapArraySize = null)
        {
            var cachedRules = GetCachedRules(converter);

            if (cachedRules.TypesCompiled.TryGetValue(new TypeRemapKey(typeName, typeRemapArraySize), out var cppType))
            {
                return cppType;
            }

            return null;
        }

        private static void ProcessCSharpElementMappingRules(CSharpConverter converter, CSharpElement element, CSharpElement context)
        {
            if (element.CppElement == null) return;

            var cachedRules = GetCachedRules(converter);
            if (cachedRules.ElementToMatches.TryGetValue(element.CppElement, out var rules))
            {
                foreach (var rule in rules)
                {
                    foreach (var action in rule.Rule.CSharpElementActions)
                    {
                        action(converter, element, rule.Matches);
                    }
                }

                // TODO: better try to cache the underlying object
                cachedRules.ElementToMatches.Remove(element.CppElement);
            }
        }

        private static CachedRules GetCachedRules(CSharpConverter converter)
        {
            var cachedRules = converter.GetTagValueOrDefault<CachedRules>(CachedRulesKey);
            if (cachedRules == null)
            {
                cachedRules = new CachedRules();

                foreach (var mappingRule in converter.Options.MappingRules.MacroRules)
                {
                    cachedRules.MacroRules.Add(mappingRule);
                }

                var tempTypedefRules = new Dictionary<TypeRemapKey, CppElementMappingRule>();
                foreach (var mappingRule in converter.Options.MappingRules.StandardRules)
                {
                    cachedRules.StandardRules.Add(mappingRule);
                    if (mappingRule.TypeRemap != null)
                    {
                        tempTypedefRules[new TypeRemapKey(mappingRule.TypeRemap, mappingRule.TypeRemapArraySize)] = mappingRule;
                    }
                }

                foreach (var cppElementMappingRule in tempTypedefRules)
                {
                    cachedRules.TypesToCompile.Add(cppElementMappingRule.Value);
                }

                converter.Tags.Add(CachedRulesKey, cachedRules);
            }

            return cachedRules;
        }

        private class CachedRules
        {
            public CachedRules()
            {
                MacroRules = new List<CppMacroMappingRule>();
                StandardRules = new List<CppElementMappingRule>();
                ElementToMatches = new Dictionary<CppElement, List<CppElementMatch>>(CppElementReferenceEqualityComparer.Default);
                TypesToCompile = new List<CppElementMappingRule>();
                TypesCompiled = new Dictionary<TypeRemapKey, CppType>();
                Prefix = "cppast_" + Guid.NewGuid().ToString("N") + "_";
            }

            public string Prefix { get; }

            public List<CppMacroMappingRule> MacroRules { get; }

            public List<CppElementMappingRule> StandardRules { get; }

            public List<CppElementMappingRule> TypesToCompile { get; }

            public Dictionary<TypeRemapKey, CppType> TypesCompiled { get; }

            public Dictionary<CppElement, List<CppElementMatch>> ElementToMatches { get; }

            public bool IsEmpty => MacroRules.Count == 0 && StandardRules.Count == 0;
        }

        private readonly struct TypeRemapKey
        {
            public TypeRemapKey(string typeRemap, int? typeRemapArraySize)
            {
                TypeRemap = typeRemap;
                TypeRemapArraySize = typeRemapArraySize;
            }

            public readonly string TypeRemap;

            public readonly int? TypeRemapArraySize;

            public bool Equals(TypeRemapKey other)
            {
                return string.Equals(TypeRemap, other.TypeRemap) && TypeRemapArraySize == other.TypeRemapArraySize;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return obj is TypeRemapKey other && Equals(other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((TypeRemap != null ? TypeRemap.GetHashCode() : 0) * 397) ^ TypeRemapArraySize.GetHashCode();
                }
            }
        }

        private readonly struct CppElementMatch
        {
            public CppElementMatch(CppElementMappingRule rule, List<ICppElementMatch> matches)
            {
                Rule = rule;
                Matches = matches;
            }

            public CppElementMappingRule Rule { get; }

            public List<ICppElementMatch> Matches { get; }
        }
    }
}