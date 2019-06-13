// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace CppAst.CodeGen.CSharp
{
    public delegate void CppElementModifierDelegate(CSharpConverter converter, CppElement cppElement, CSharpElement context, List<ICppElementMatch> matches);

    public delegate void CSharpElementModifierDelegate(CSharpConverter converter, CSharpElement csElement, List<ICppElementMatch> matches);

    public class CppElementMappingRule : CppElementMappingRuleBase
    {
        public CppElementMappingRule(params CppElementMatcher[] matchers)
        {
            if (matchers == null) throw new ArgumentNullException(nameof(matchers));
            AddMatchers(matchers);
            CppElementActions = new List<CppElementModifierDelegate>();
            CSharpElementActions = new List<CSharpElementModifierDelegate>();
        }

        public string TypeRemap { get; set; }

        public int? TypeRemapArraySize { get; set; }

        public List<CppElementModifierDelegate> CppElementActions { get; }

        public List<CSharpElementModifierDelegate> CSharpElementActions { get; }
    }
}