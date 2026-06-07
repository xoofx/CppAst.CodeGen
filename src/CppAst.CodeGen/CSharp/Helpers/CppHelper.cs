// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp;

public static class CppHelper
{
    public static bool IsStructCanBeRecordWithElementType(CppType elementType)
    {
        // We can use a record if the element type of typedef is the following:
        return elementType is CppPrimitiveType || elementType is CppTypedef || elementType is CppClass || elementType is CppEnum;
    }

    public static bool IsObjCFunction(CppFunction cppFunction)
    {
        var cppParent = cppFunction.Parent as CppClass;
        bool isObjCFunction = cppParent != null &&
            (cppParent.ClassKind == CppClassKind.ObjCInterface ||
            cppParent.ClassKind == CppClassKind.ObjCInterfaceCategory ||
            cppParent.ClassKind == CppClassKind.ObjCProtocol) &&
            ((cppFunction.Flags & (CppFunctionFlags.Method | CppFunctionFlags.ClassMethod)) != 0);

        return isObjCFunction;
    }
}