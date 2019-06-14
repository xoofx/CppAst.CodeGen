// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public enum CSharpCasingKind
    {
        Undefined,

        /// <summary>
        /// 'camelCase'
        /// </summary>
        Lower,

        /// <summary>
        /// 'camelCase'
        /// </summary>
        Camel,

        /// <summary>
        /// 'PascalCase'
        /// </summary>
        Pascal,

        /// <summary>
        /// 'SCREAMING'
        /// </summary>
        Screaming,

        // Only snake case after this, CamelCase must be the first

        /// <summary>
        /// 'camel_Case'
        /// </summary>
        CamelSnake,

        /// <summary>
        /// 'lower_case'
        /// </summary>
        LowerSnake,

        /// <summary>
        /// 'Pascal_Case'
        /// </summary>
        PascalSnake,

        /// <summary>
        /// 'SCREAMING_CASE'
        /// </summary>
        ScreamingSnake,
    }
}