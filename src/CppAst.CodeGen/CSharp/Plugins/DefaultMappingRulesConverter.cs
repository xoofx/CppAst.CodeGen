﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public partial class DefaultMappingRulesConverter : ICSharpConverterPlugin
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

                var typedefs = converter.CurrentCppCompilation!.Typedefs;
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
                var macrosByName = new Dictionary<string, CppMacro>();
                foreach (var cppMacro in macrosByName.Values)
                {
                    macrosByName[cppMacro.Name] = cppMacro;
                }
                var ruleNameByMacro = new Dictionary<CppMacro, Dictionary<CppMacroMappingRule, string>>();

                // Generate names from rules
                foreach (var cppMacro in cppCompilation.Macros)
                {
                    if (cppMacro.Parameters != null)
                    {
                        continue;
                    }

                    ruleNameByMacro.Add(cppMacro, new Dictionary<CppMacroMappingRule, string>());

                    foreach (var cppMacroRule in macroRules)
                    {
                        matches.Clear();

                        if (cppMacroRule.Match(cppMacro, matches))
                        {
                            var regexMatch = matches.FindMatch<CppElementRegexMatch>();

                            var name = cppMacroRule switch
                            {
                                CppMacroToConstMappingRule macroToConst => macroToConst.ConstFieldName,
                                CppMacroToEnumMappingRule macroToEnum => macroToEnum.CppEnumItemName,
                                _ => null
                            };

                            if (regexMatch != null && name != null)
                            {
                                ruleNameByMacro[cppMacro][cppMacroRule] = Regex.Replace(regexMatch.RegexInput, regexMatch.RegexPattern, name);
                            }
                            else
                            {
                                ruleNameByMacro[cppMacro][cppMacroRule] = cppMacro.Name;
                            }

                            if (!cachedRules.MatchMacros.Contains(cppMacro))
                            {
                                cachedRules.MatchMacros.Add(cppMacro);
                            }
                        }
                    }
                }

                // Write matched macros
                foreach (var macroToRulePair in ruleNameByMacro)
                {
                    var cppMacro = macroToRulePair.Key;

                    foreach (var cppMacroRuleToNamePair in macroToRulePair.Value)
                    {
                        var cppMacroRule = cppMacroRuleToNamePair.Key;
                        var name = cppMacroRuleToNamePair.Value;

                        StringBuilder? stringBuilder = null;
                        bool explicitCast = false;
                        string? typeName = null;
                        string? rulePrefix = null;
                        string? ruleSuffix = null;

                        switch (cppMacroRule)
                        {
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

                                stringBuilder = macrosAsEnumText;
                                explicitCast = macroToEnum.ExplicitCast;
                                typeName = macroToEnum.CppIntegerTypeName;
                                rulePrefix = $"    {cachedRules.GetPrefix(cppMacro)}";
                                ruleSuffix = ",";
                                break;
                            }
                            case CppMacroToConstMappingRule macroToConst:
                                
                                stringBuilder = additionalHeaders;
                                explicitCast = macroToConst.ExplicitCast;
                                typeName = macroToConst.ConstFieldTypeName;
                                rulePrefix = $"const {macroToConst.ConstFieldTypeName} {cachedRules.GetPrefix(cppMacro)}";
                                ruleSuffix = ";";
                                break;
                        }

                        if (stringBuilder != null)
                        {
                            AppendPragmaLine(cppMacroRule, stringBuilder);

                            foreach (var token in cppMacro.Tokens)
                            {
                                if (token.Kind == CppTokenKind.Comment && !string.IsNullOrEmpty(token.Text))
                                {
                                    stringBuilder.AppendLine(token.Text);
                                }

                                if (token.Kind == CppTokenKind.Identifier &&
                                    macrosByName.TryGetValue(token.Text, out var tokenMacro) &&
                                    ruleNameByMacro.TryGetValue(tokenMacro, out var namesDictionary) &&
                                    namesDictionary.Count > 0)
                                {
                                    var tokenName = namesDictionary
                                        .OrderByDescending(kvp => kvp.Key is CppMacroToConstMappingRule)
                                        .ThenByDescending(kvp => kvp.Key == cppMacroRule)
                                        .First().Value;

                                    token.Text = $"{cachedRules.GetPrefix(cppMacro)}{tokenName}";
                                }
                            }

                            cppMacro.UpdateValueFromTokens();

                            var macroValue = cppMacroRule.OverrideValue ?? cppMacro.Value;
                            stringBuilder.AppendLine(explicitCast
                                ? $"{rulePrefix}{name} = ({typeName}){macroValue}{ruleSuffix}"
                                : $"{rulePrefix}{name} = {macroValue}{ruleSuffix}");
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
            builder.AppendLine($"#line {rule.DeclarationLineNumber} \"{rule.DeclarationFileName?.Replace(@"\", @"\\")}\"");
        }

        /// <summary>
        /// Macro used to match an index of CppMacro and the name of the macro
        /// </summary>
        [GeneratedRegex(@"^(\d+)_(.*)")]
        private static partial Regex RegexMatchMacro();

        private static void ProcessCppElementMappingRules(CSharpConverter converter, CppElement cppElement, CSharpElement context)
        {
            var cachedRules = GetCachedRules(converter);

            var rules = cachedRules.StandardRules;

            // If a CppElement starts with a Prefix, it was generated by AfterPreprocessing and need to be removed
            // entirely
            if (cppElement is ICppMember member && member.Name.StartsWith(cachedRules.Prefix))
            {
                string RemoveRulePrefix(string str)
                {
                    return str.Substring(cachedRules.Prefix.Length);
                }
                
                var result = RegexMatchMacro().Match(RemoveRulePrefix(member.Name));
                
                // Recover the macro index to recover the original SourceSpan to dispatch later to the correct output files
                var macroIndex = int.Parse(result.Groups[1].Value);
                var name = result.Groups[2].Value;
                
                member.Name = name;
                cppElement.Span = cachedRules.MatchMacros[macroIndex].Span;

                // Check field and parameter expressions' arguments for the prefix as well
                var expression = cppElement switch
                {
                    CppField cppField => cppField.InitExpression,
                    CppParameter cppParameter => cppParameter.InitExpression,
                    CppEnumItem cppEnumItem => cppEnumItem.ValueExpression,
                    _ => throw new InvalidOperationException($"Unexpected type {cppElement.GetType()}")
                };

                if (cppElement.Parent is CppEnum cppEnum)
                {
                    if (cppEnum.Span.Start.File != cppElement.Span.Start.File && !string.IsNullOrEmpty(cppElement.Span.Start.File))
                    {
                        cppEnum.Span = cppElement.Span;
                    }
                }

                if (expression != null)
                {
                    bool expressionWasNotRecovered = false;
                    var expressions = new Stack<CppExpression>();
                    expressions.Push(expression);

                    while (expressions.Count > 0)
                    {
                        expression = expressions.Pop();
                        switch (expression)
                        {
                            case CppRawExpression rawExpression:
                            {
                                foreach (var token in rawExpression.Tokens)
                                {
                                    if (token.Text.StartsWith(cachedRules.Prefix))
                                    {
                                        token.Text = RemoveRulePrefix(token.Text);
                                    }
                                }
                                rawExpression.UpdateTextFromTokens();
                                break;
                            }
                            case CppLiteralExpression literalExpression:
                            {
                                if (literalExpression.Value != null && literalExpression.Value.StartsWith(cachedRules.Prefix))
                                {
                                    literalExpression.Value = RemoveRulePrefix(literalExpression.Value);
                                }
                                else if (string.IsNullOrEmpty(literalExpression.Value))
                                {
                                    expressionWasNotRecovered = true;
                                }
                                break;
                            }
                        }

                        if (expression.Arguments != null)
                        {
                            foreach (var argument in expression.Arguments)
                            {
                                expressions.Push(argument);
                            }
                        }
                    }

                    if (expressionWasNotRecovered)
                    {
                        switch (cppElement)
                        {
                            case CppField cppField:
                                cppField.InitExpression = null;
                                break;

                            case CppParameter cppParameter:
                                cppParameter.InitExpression = null;
                                break;

                            case CppEnumItem cppEnumItem:
                                cppEnumItem.ValueExpression = null;
                                break;
                            default:
                                throw new InvalidOperationException($"Unexpected type {cppElement.GetType()}");
                        }
                    }
                }
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

        internal static CppType? GetCppTypeRemap(CSharpConverter converter, string typeName, int? typeRemapArraySize = null)
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
                MatchMacros = new List<CppMacro>();
            }

            public string Prefix { get; }

            public List<CppMacroMappingRule> MacroRules { get; }

            public List<CppElementMappingRule> StandardRules { get; }

            public List<CppElementMappingRule> TypesToCompile { get; }

            public Dictionary<TypeRemapKey, CppType> TypesCompiled { get; }

            public Dictionary<CppElement, List<CppElementMatch>> ElementToMatches { get; }

            public List<CppMacro> MatchMacros { get; }

            public bool IsEmpty => MacroRules.Count == 0 && StandardRules.Count == 0;

            public string GetPrefix(CppMacro macro)
            {
                var index = MatchMacros.IndexOf(macro);
                if (index < 0) throw new InvalidOperationException($"Cannot find the macro {macro.Name} in the list of matched macros");
                return $"{Prefix}{index:0}_";
            }
        }

        private readonly struct TypeRemapKey
        {
            public TypeRemapKey(string? typeRemap, int? typeRemapArraySize)
            {
                TypeRemap = typeRemap;
                TypeRemapArraySize = typeRemapArraySize;
            }

            public readonly string? TypeRemap;

            public readonly int? TypeRemapArraySize;

            public bool Equals(TypeRemapKey other)
            {
                return string.Equals(TypeRemap, other.TypeRemap) && TypeRemapArraySize == other.TypeRemapArraySize;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
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
