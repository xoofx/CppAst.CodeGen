// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.IO;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultTypedefConverter : ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.TypedefConverters.Add(ConvertTypedef);
        }

        private CSharpElement ConvertTypedef(CSharpConverter converter, CppTypedef cppTypedef, CSharpElement context)
        {
            var elementType = cppTypedef.ElementType;

            if (DefaultFunctionTypeConverter.IsFunctionType(elementType, out var cppFunctionType))
            {
                return DefaultFunctionTypeConverter.ConvertNamedFunctionType(converter, cppFunctionType, context, cppTypedef.Name);
            }

            var isFromSystemIncludes = IsFromSystemIncludes(converter.CurrentCppCompilation, cppTypedef);

            var csElementType = converter.GetCSharpType(elementType, context, true);

            var noWrap = converter.Options.TypedefCodeGenKind == CppTypedefCodeGenKind.NoWrap && !converter.Options.TypedefWrapWhiteList.Contains(cppTypedef.Name);

            // If:
            // - the typedef is from system includes and the underlying type is not a pointer
            // - or the typedef mode is "no-wrap" and is not in the whitelist
            // then we bypass entirely the typedef and return immediately the element type
            if (noWrap || (isFromSystemIncludes && elementType.TypeKind != CppTypeKind.Pointer))
            {
                return csElementType;
            }

            // Otherwise we generate a small wrapper struct
            var csStructName = converter.GetCSharpName(cppTypedef, context);
            var csStruct = new CSharpStruct(csStructName)
            {
                CppElement = cppTypedef,
            };

            var container = converter.GetCSharpContainer(cppTypedef, context);
            converter.ApplyDefaultVisibility(csStruct, container);
            container.Members.Add(csStruct);

            csStruct.Comment = converter.GetCSharpComment(cppTypedef, csStruct);

            // Requires System.Runtime.InteropServices
            csStruct.Attributes.Add(new CSharpFreeAttribute("StructLayout(LayoutKind.Sequential)"));

            // Required by StructLayout
            converter.AddUsing(container, "System.Runtime.InteropServices");

            var name = csStruct.Name;
            csStruct.Modifiers |= CSharpModifiers.ReadOnly;
            csStruct.BaseTypes.Add(new CSharpFreeType($"IEquatable<{name}>"));

            // Dump the type name and attached attributes for the element type
            var csElementTypeName = converter.ConvertTypeReferenceToString(csElementType, out var attachedAttributes);

            csStruct.Members.Add(new CSharpLineElement($"public {name}({csElementTypeName} value) => this.Value = value;"));
            csStruct.Members.Add(new CSharpLineElement($"{attachedAttributes}public readonly {csElementTypeName} Value;"));
            csStruct.Members.Add(new CSharpLineElement($"public bool Equals({name} other) =>  Value.Equals(other.Value);"));
            csStruct.Members.Add(new CSharpLineElement($"public override bool Equals(object obj) => obj is {name} other && Equals(other);"));
            csStruct.Members.Add(new CSharpLineElement($"public override int GetHashCode() => Value.GetHashCode();"));
            csStruct.Members.Add(new CSharpLineElement($"public override string ToString() => Value.ToString();"));
            csStruct.Members.Add(new CSharpLineElement($"public static implicit operator {csElementTypeName}({name} from) => from.Value;"));
            csStruct.Members.Add(new CSharpLineElement($"public static implicit operator {name}({csElementTypeName} from) => new {name}(from);"));

            return csStruct;

        }
        
        private static bool IsFromSystemIncludes(CppCompilation cppCompilation, CppElement cppElement)
        {
            while (cppElement != null)
            {
                if (cppElement == cppCompilation.System)
                {
                    return true;
                }
                cppElement = cppElement.Parent as CppElement;
            }
            return false;
        }
    }
}