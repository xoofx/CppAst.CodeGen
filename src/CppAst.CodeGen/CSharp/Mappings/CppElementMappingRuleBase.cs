// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CppElementMappingRuleBase : CppElementMatcher
    {
        private readonly List<CppElementMatcher> _matchers = new List<CppElementMatcher>();

        public IReadOnlyList<CppElementMatcher> Matchers => _matchers;

        public void AddMatchers(IEnumerable<CppElementMatcher> matchers)
        {
            foreach (var cppElementMatcher in matchers)
            {
                AddMatcher(cppElementMatcher);
            }
        }

        public void AddMatcher(CppElementMatcher matcher)
        {
            if (matcher == null) throw new ArgumentNullException(nameof(matcher));
            _matchers.Add(matcher);
        }

        public string? DeclarationFileName { get; set; }

        public int DeclarationLineNumber { get; set; }

        public override bool Match(CppElement cppElement, List<ICppElementMatch> outMatches)
        {
            foreach (var matcher in _matchers)
            {
                if (!matcher.Match(cppElement, outMatches))
                {
                    return false;
                }
            }

            return true;
        }
    }
}