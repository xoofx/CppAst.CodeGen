// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace CppAst.CodeGen.CSharp
{
    public class CSharpStruct : CSharpTypeWithMembers
    {
        public CSharpStruct(string name) : base(name)
        {
            Modifiers = CSharpModifiers.Partial;
        }

        /// <inheritdoc />
        protected override string DeclarationKind => "struct";

        public CSharpStructMarshallingUsage MarshallingUsage { get; set; }


        public static CSharpStruct MakeObjCObject(string name, CppElement element)
        {
            var csStruct = new CSharpStruct(name)
            {
                CppElement = element,
                IsRecord = true,
                Modifiers = CSharpModifiers.ReadOnly | CSharpModifiers.Partial
            };
            csStruct.BaseTypes.Add(new CSharpFreeType("ObjCRuntime.IObjCObject"));
            csStruct.PrimaryConstructorParameters.Add(new CSharpParameter("Handle")
            {
                ParameterType = CSharpPrimitiveType.IntPtr(),
            });
            return csStruct;
        }
    }
}
