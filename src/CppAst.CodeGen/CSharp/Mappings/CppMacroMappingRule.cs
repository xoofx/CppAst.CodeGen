// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace CppAst.CodeGen.CSharp
{
    public abstract class CppMacroMappingRule : CppElementMappingRuleBase
    {
        protected CppMacroMappingRule(CppElementRegexMatcher macroMatch)
        {
            if (macroMatch == null) throw new ArgumentNullException(nameof(macroMatch));
            AddMatcher(macroMatch);
        }
    }
}