// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace CppAst.CodeGen.CSharp
{
    public static class CppTypeExtensions
    {
        public static bool TryGetElementTypeFromPointerToConst(this CppType cppType, [NotNullWhen(true)] out CppType? elementType)
        {
            if (cppType is CppPointerType type && type.ElementType is CppQualifiedType qualifiedType && qualifiedType.Qualifier == CppTypeQualifier.Const)
            {
                elementType = qualifiedType.ElementType;
                return true;
            }

            elementType = null;
            return false;
        }
    }
}