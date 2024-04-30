// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;

namespace CppAst.CodeGen.CSharp;

/// <summary>
/// Defines how a struct is used for marshalling between managed/native.
/// </summary>
[Flags]
public enum CSharpStructMarshallingUsage
{
    /// <summary>
    /// The struct doesn't appear to be used as between managed/native transition.
    /// </summary>
    None = 0,

    /// <summary>
    /// The struct is used to pass data from managed to native.
    /// </summary> 
    ManagedToNative = 1 << 0,

    /// <summary>
    /// The struct is used to pass data from native to managed.
    /// </summary>
    NativeToManaged = 1 << 1,

    /// <summary>
    /// The struct is used as an input/output parameter directly or indirectly from/to native.
    /// </summary>
    Both = ManagedToNative | NativeToManaged
}