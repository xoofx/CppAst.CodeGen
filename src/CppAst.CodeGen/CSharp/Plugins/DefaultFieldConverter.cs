// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultFieldConverter: ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.FieldConverters.Add(ConvertField);
        }
        
        public static CSharpElement ConvertField(CSharpConverter converter, CppField cppField, CSharpElement context)
        {
            var csContainer = ((ICSharpContainer)(context as CSharpStruct) ?? context as CSharpClass);
            if (csContainer == null)
            {
                csContainer = converter.GetCSharpContainer(cppField, context);
            }
            
            var isUnion = ((cppField.Parent as CppClass)?.ClassKind ?? CppClassKind.Struct) == CppClassKind.Union;

            var csFieldName = converter.GetCSharpName(cppField, (CSharpElement)csContainer);
            var csField = new CSharpField(csFieldName) { CppElement = cppField };
            converter.ApplyDefaultVisibility(csField, csContainer);

            bool isConst = cppField.Type is CppQualifiedType qualifiedType && qualifiedType.Qualifier == CppTypeQualifier.Const;
            if (isConst)
            {
                csField.Modifiers |= CSharpModifiers.Const;
            }

            csContainer.Members.Add(csField);

            csField.Comment = converter.GetCSharpComment(cppField, csField);

            if (isUnion)
            {
                csField.Attributes.Add(new CSharpFreeAttribute("FieldOffset(0)"));
                var container = converter.GetCSharpContainer(cppField, context);
                converter.AddUsing(container, "System.Runtime.InteropServices");
            }
            csField.FieldType = converter.GetCSharpType(cppField.Type, csField);

            if (cppField.InitExpression != null)
            {
                csField.InitValue = converter.ConvertExpression(cppField.InitExpression, context, csField.FieldType);
            }

            return csField;
        }
    }
}