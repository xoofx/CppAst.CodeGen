// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultTypedefConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.TypedefConverters.Add(ConvertTypedef);
        }

        private CSharpElement ConvertTypedef(CSharpConverter converter, CppTypedef cppTypedef, CSharpElement context)
        {
            var elementType = cppTypedef.ElementType;

            var isFromSystemIncludes = converter.IsFromSystemIncludes(cppTypedef);

            var csElementType = converter.GetCSharpType(elementType, context, true);

            var noWrap = converter.Options.TypedefCodeGenKind == CppTypedefCodeGenKind.NoWrap && !converter.Options.TypedefWrapWhiteList.Contains(cppTypedef.Name);

            // If:
            // - the typedef is from system includes and the underlying type is not a pointer
            // - or the typedef mode is "no-wrap" and is not in the whitelist
            // - or the typedef is a typedef of an opaque struct
            // - or the typedef is a typedef of void
            // then we bypass entirely the typedef and return immediately the element type
            var is_size_t = isFromSystemIncludes && cppTypedef.Name == "size_t";

            var attachedAttributes = string.Empty;
            var csElementTypeName = is_size_t ? "nint" : converter.ConvertTypeReferenceToString(csElementType, out attachedAttributes);

            var csStructName = converter.GetCSharpName(cppTypedef, context);
            var csStruct = new CSharpStruct(csStructName)
            {
                CppElement = cppTypedef,
            };

            if (noWrap || (isFromSystemIncludes && elementType.TypeKind != CppTypeKind.Pointer && !is_size_t) || csStruct.IsOpaque || csElementTypeName == "void")
            {
                return csElementType;
            }

            var container = converter.GetCSharpContainer(cppTypedef, context);
            converter.ApplyDefaultVisibility(csStruct, container);
            container.Members.Add(csStruct);

            csStruct.Comment = converter.GetCSharpComment(cppTypedef, csStruct);

            var structLayout = new CSharpStructLayoutAttribute(LayoutKind.Sequential);
            if (!converter.Options.DisableRuntimeMarshalling)
            {
                structLayout.CharSet = converter.Options.DefaultCharSet;
            }

            // TODO: Add size/pack information

            if (structLayout.CharSet.HasValue || structLayout.Pack.HasValue || structLayout.Size.HasValue || structLayout.LayoutKind != LayoutKind.Sequential)
            {
                csStruct.Attributes.Add(structLayout);

                // Required by StructLayout
                converter.AddUsing(container, "System.Runtime.InteropServices");
            }

            var name = csStruct.Name;
            csStruct.Modifiers |= CSharpModifiers.ReadOnly;
            csStruct.BaseTypes.Add(new CSharpFreeType($"IEquatable<{name}>"));

            // Dump the type name and attached attributes for the element type

            if (csElementType is CSharpPointerType || csElementType is CSharpFunctionPointer)
            {
                csStruct.Members.Add(new CSharpLineElement($"public {name}({csElementTypeName} value) => this.Value = value;"));
                csStruct.Members.Add(new CSharpLineElement($"{attachedAttributes}public {csElementTypeName} Value {{ get; }}"));
                csStruct.Members.Add(new CSharpLineElement($"public bool Equals({name} other) =>  Value == other.Value;"));
                csStruct.Members.Add(new CSharpLineElement($"public override bool Equals(object obj) => obj is {name} other && Equals(other);"));
                csStruct.Members.Add(new CSharpLineElement("public override int GetHashCode() => ((nint)(void*)Value).GetHashCode();"));
                csStruct.Members.Add(new CSharpLineElement("public override string ToString() => ((nint)(void*)Value).ToString();"));
                csStruct.Members.Add(new CSharpLineElement($"public static implicit operator {csElementTypeName}({name} from) => from.Value;"));
                csStruct.Members.Add(new CSharpLineElement($"public static implicit operator {name}({csElementTypeName} from) => new {name}(from);"));
                csStruct.Members.Add(new CSharpLineElement($"public static bool operator ==({name} left, {name} right) => left.Equals(right);"));
                csStruct.Members.Add(new CSharpLineElement($"public static bool operator !=({name} left, {name} right) => !left.Equals(right);"));
            }
            else
            {
                csStruct.Members.Add(new CSharpLineElement($"public {name}({csElementTypeName} value) => this.Value = value;"));
                csStruct.Members.Add(new CSharpLineElement($"{attachedAttributes}public {csElementTypeName} Value {{ get; }}"));
                csStruct.Members.Add(new CSharpLineElement($"public bool Equals({name} other) =>  Value.Equals(other.Value);"));
                csStruct.Members.Add(new CSharpLineElement($"public override bool Equals(object obj) => obj is {name} other && Equals(other);"));
                csStruct.Members.Add(new CSharpLineElement("public override int GetHashCode() => Value.GetHashCode();"));
                csStruct.Members.Add(new CSharpLineElement("public override string ToString() => Value.ToString();"));
                csStruct.Members.Add(new CSharpLineElement($"public static implicit operator {csElementTypeName}({name} from) => from.Value;"));
                csStruct.Members.Add(new CSharpLineElement($"public static implicit operator {name}({csElementTypeName} from) => new {name}(from);"));
                csStruct.Members.Add(new CSharpLineElement($"public static bool operator ==({name} left, {name} right) => left.Equals(right);"));
                csStruct.Members.Add(new CSharpLineElement($"public static bool operator !=({name} left, {name} right) => !left.Equals(right);"));
            }

            return csStruct;
        }
    }
}
