// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.Common
{
    /// <summary>
    /// Mode for <see cref="CodeWriter"/>
    /// </summary>
    public enum CodeWriterMode
    {
        /// <summary>
        /// Generates full details.
        /// </summary>
        Full,

        /// <summary>
        /// Generates simple details generally used for ToString()
        /// </summary>
        Simple,
    }
}