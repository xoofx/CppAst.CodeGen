// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class CppMacroToEnumMappingRule : CppMacroMappingRule
    {
        public CppMacroToEnumMappingRule(CppElementRegexMatcher macroMatch) : base(macroMatch)
        {
        }

        public string CppEnumTypeName { get; set; }

        public string CppEnumItemName { get; set; }

        public string CppIntegerTypeName { get; set; }

        public bool ExplicitCast { get; set; }
    }
}