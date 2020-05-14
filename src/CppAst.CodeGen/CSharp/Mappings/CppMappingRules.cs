// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public class CppMappingRules : IEnumerable<CppElementMappingRuleBase>
    {
        public CppMappingRules()
        {
            MacroRules = new List<CppMacroMappingRule>();
            StandardRules = new List<CppElementMappingRule>();
        }

        public List<CppMacroMappingRule> MacroRules { get; }

        public List<CppElementMappingRule> StandardRules { get; }

        public void Add(Func<CppMappingRules, CppMacroMappingRule> e)
        {
            MacroRules.Add(e(this));
        }

        public void Add(Func<CppMappingRules, CppElementMappingRule> e)
        {
            StandardRules.Add(e(this));
        }

        /// <inheritdoc />
        public IEnumerator<CppElementMappingRuleBase> GetEnumerator()
        {
            foreach (var rule in MacroRules)
            {
                yield return rule;
            }

            foreach (var rule in StandardRules)
            {
                yield return rule;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}