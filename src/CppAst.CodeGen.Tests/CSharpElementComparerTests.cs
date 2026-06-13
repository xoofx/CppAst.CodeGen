// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CppAst.CodeGen.CSharp;
using NUnit.Framework;

namespace CppAst.CodeGen.Tests
{
    public class CSharpElementComparerTests
    {
        [Test]
        public void CompareRecordStructsIncludesPrimaryConstructorParameters()
        {
            var left = CreateRecordStruct("Handle", CSharpPrimitiveType.IntPtr());
            var same = CreateRecordStruct("Handle", CSharpPrimitiveType.IntPtr());
            var differentName = CreateRecordStruct("Value", CSharpPrimitiveType.IntPtr());
            var differentType = CreateRecordStruct("Handle", CSharpPrimitiveType.Int());

            Assert.True(CSharpElementComparer.Compare(left, same));
            Assert.False(CSharpElementComparer.Compare(left, differentName));
            Assert.False(CSharpElementComparer.Compare(left, differentType));
        }

        [Test]
        public void CompareRecordStructsIncludesBaseTypes()
        {
            var left = CreateRecordStruct("Handle", CSharpPrimitiveType.IntPtr());
            left.BaseTypes.Add(new CSharpGenericTypeReference(new CSharpSimpleNameReferenceType(new CSharpInterface("IObjCObject")), new CSharpSimpleNameReferenceType(left)));

            var same = CreateRecordStruct("Handle", CSharpPrimitiveType.IntPtr());
            same.BaseTypes.Add(new CSharpGenericTypeReference(new CSharpSimpleNameReferenceType(new CSharpInterface("IObjCObject")), new CSharpSimpleNameReferenceType(same)));

            var differentBase = CreateRecordStruct("Handle", CSharpPrimitiveType.IntPtr());
            differentBase.BaseTypes.Add(new CSharpGenericTypeReference(new CSharpSimpleNameReferenceType(new CSharpInterface("INSObject")), new CSharpSimpleNameReferenceType(differentBase)));

            Assert.True(CSharpElementComparer.Compare(left, same));
            Assert.False(CSharpElementComparer.Compare(left, differentBase));
        }

        [Test]
        public void CompareIncludesElementNamesForNamedMembersAndTypes()
        {
            Assert.True(CSharpElementComparer.Compare(new CSharpEnumItem("A", "1"), new CSharpEnumItem("A", "1")));
            Assert.False(CSharpElementComparer.Compare(new CSharpEnumItem("A", "1"), new CSharpEnumItem("B", "1")));

            Assert.True(CSharpElementComparer.Compare(CreateMethod("Open"), CreateMethod("Open")));
            Assert.False(CSharpElementComparer.Compare(CreateMethod("Open"), CreateMethod("Close")));

            Assert.True(CSharpElementComparer.Compare(new CSharpStruct("Native"), new CSharpStruct("Native")));
            Assert.False(CSharpElementComparer.Compare(new CSharpStruct("Native"), new CSharpStruct("OtherNative")));

            Assert.True(CSharpElementComparer.Compare(CreateDelegate("Callback"), CreateDelegate("Callback")));
            Assert.False(CSharpElementComparer.Compare(CreateDelegate("Callback"), CreateDelegate("OtherCallback")));
        }

        [Test]
        public void CompareCoversCompositeTypeShapes()
        {
            Assert.True(CSharpElementComparer.Compare(new CSharpNullableType(CSharpPrimitiveType.Int()), new CSharpNullableType(CSharpPrimitiveType.Int())));
            Assert.False(CSharpElementComparer.Compare(new CSharpNullableType(CSharpPrimitiveType.Int()), new CSharpNullableType(CSharpPrimitiveType.Long())));

            Assert.True(CSharpElementComparer.Compare(new CSharpPointerType(CSharpPrimitiveType.Byte()), new CSharpPointerType(CSharpPrimitiveType.Byte())));
            Assert.False(CSharpElementComparer.Compare(new CSharpPointerType(CSharpPrimitiveType.Byte()), new CSharpPointerType(CSharpPrimitiveType.SByte())));

            Assert.True(CSharpElementComparer.Compare(new CSharpArrayType(CSharpPrimitiveType.Int()), new CSharpArrayType(CSharpPrimitiveType.Int())));
            Assert.False(CSharpElementComparer.Compare(new CSharpArrayType(CSharpPrimitiveType.Int()), new CSharpArrayType(CSharpPrimitiveType.UInt())));

            Assert.True(CSharpElementComparer.Compare(new CSharpFixedArrayType(CSharpPrimitiveType.Int(), 4), new CSharpFixedArrayType(CSharpPrimitiveType.Int(), 4)));
            Assert.False(CSharpElementComparer.Compare(new CSharpFixedArrayType(CSharpPrimitiveType.Int(), 4), new CSharpFixedArrayType(CSharpPrimitiveType.Int(), 8)));

            Assert.True(CSharpElementComparer.Compare(new CSharpRefType(CSharpRefKind.In, CSharpPrimitiveType.Int()), new CSharpRefType(CSharpRefKind.In, CSharpPrimitiveType.Int())));
            Assert.False(CSharpElementComparer.Compare(new CSharpRefType(CSharpRefKind.In, CSharpPrimitiveType.Int()), new CSharpRefType(CSharpRefKind.Ref, CSharpPrimitiveType.Int())));

            Assert.True(CSharpElementComparer.Compare(CreateFunctionPointer("Cdecl"), CreateFunctionPointer("Cdecl")));
            Assert.False(CSharpElementComparer.Compare(CreateFunctionPointer("Cdecl"), CreateFunctionPointer("Stdcall")));
        }

        [Test]
        public void CompareCoversGenericParametersAndReferences()
        {
            var leftParameter = new CSharpGenericParameterType("T") { IsOut = true };
            leftParameter.WhereClauses.Add(new CSharpWhereClause("class"));
            var sameParameter = new CSharpGenericParameterType("T") { IsOut = true };
            sameParameter.WhereClauses.Add(new CSharpWhereClause("class"));
            var differentParameter = new CSharpGenericParameterType("T") { IsOut = false };
            differentParameter.WhereClauses.Add(new CSharpWhereClause("class"));

            Assert.True(CSharpElementComparer.Compare(leftParameter, sameParameter));
            Assert.False(CSharpElementComparer.Compare(leftParameter, differentParameter));

            var listOfInt = new CSharpGenericTypeReference(new CSharpFreeType("List"), CSharpPrimitiveType.Int());
            var sameListOfInt = new CSharpGenericTypeReference(new CSharpFreeType("List"), CSharpPrimitiveType.Int());
            var listOfLong = new CSharpGenericTypeReference(new CSharpFreeType("List"), CSharpPrimitiveType.Long());

            Assert.True(CSharpElementComparer.Compare(listOfInt, sameListOfInt));
            Assert.False(CSharpElementComparer.Compare(listOfInt, listOfLong));
        }

        private static CSharpStruct CreateRecordStruct(string parameterName, CSharpType parameterType)
        {
            var cSharpStruct = new CSharpStruct("ObjCObject")
            {
                IsRecord = true,
                Modifiers = CSharpModifiers.ReadOnly | CSharpModifiers.Partial,
            };
            cSharpStruct.PrimaryConstructorParameters.Add(new CSharpParameter(parameterName)
            {
                ParameterType = parameterType,
            });
            return cSharpStruct;
        }

        private static CSharpMethod CreateMethod(string name)
        {
            var method = new CSharpMethod(name)
            {
                ReturnType = CSharpPrimitiveType.Int(),
            };
            method.Parameters.Add(new CSharpParameter(CSharpPrimitiveType.Int(), "value"));
            return method;
        }

        private static CSharpDelegate CreateDelegate(string name)
        {
            var cSharpDelegate = new CSharpDelegate(name)
            {
                ReturnType = CSharpPrimitiveType.Void(),
            };
            cSharpDelegate.Parameters.Add(new CSharpParameter(CSharpPrimitiveType.Int(), "value"));
            return cSharpDelegate;
        }

        private static CSharpFunctionPointer CreateFunctionPointer(string callingConvention)
        {
            var functionPointer = new CSharpFunctionPointer(CSharpPrimitiveType.Void())
            {
                IsUnmanaged = true,
            };
            functionPointer.UnmanagedCallingConvention.Add(callingConvention);
            functionPointer.Parameters.Add(new CSharpParameter(CSharpPrimitiveType.Int(), "value"));
            return functionPointer;
        }
    }
}
