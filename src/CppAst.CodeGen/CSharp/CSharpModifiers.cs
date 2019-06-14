// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace CppAst.CodeGen.CSharp
{
    [Flags]
    public enum CSharpModifiers
    {
        None = 0,
        Static = 1 << 0,
        Extern = 1 << 1,
        Partial = 1 << 2,
        Abstract = 1 << 3,
        Const = 1 << 4,
        ReadOnly = 1 << 5,
        Unsafe = 1 << 6,
    }
}