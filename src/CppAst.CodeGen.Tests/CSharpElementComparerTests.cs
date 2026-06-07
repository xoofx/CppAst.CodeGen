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
    }
}
