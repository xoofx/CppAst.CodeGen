// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class CppMacroToConstMappingRule : CppMacroMappingRule
    {
        public CppMacroToConstMappingRule(CppElementRegexMatcher macroMatch) : base(macroMatch)
        {
        }

        public string ConstFieldTypeName { get; set; }

        public string ConstFieldName { get; set; }

        public bool ExplicitCast { get; set; }
    }
}