// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultTypedefConverter : ICSharpConverterPlugin
    {
        public DefaultTypedefConverter()
        {
            StandardCTypes = new Dictionary<string, Func<CSharpType>>()
            {
                {"int8_t", CSharpPrimitiveType.SByte},
                {"uint8_t", CSharpPrimitiveType.Byte},
                {"int16_t", CSharpPrimitiveType.Short},
                {"uint16_t", CSharpPrimitiveType.UShort},
                {"int32_t", CSharpPrimitiveType.Int},
                {"uint32_t", CSharpPrimitiveType.UInt},
                {"int64_t", CSharpPrimitiveType.Long},
                {"uint64_t", CSharpPrimitiveType.ULong},
                {"size_t", CSharpPrimitiveType.UIntPtr},
                {"ssize_t", CSharpPrimitiveType.IntPtr},
                {"ptrdiff_t", CSharpPrimitiveType.IntPtr},
                {"intptr_t", CSharpPrimitiveType.IntPtr},
                {"uintptr_t", CSharpPrimitiveType.UIntPtr},
                {"char16_t", CSharpPrimitiveType.Char},
                {"char32_t", () => new CSharpFreeType("global::System.Text.Rune")},
            };
        }

        public Dictionary<string, Func<CSharpType>> StandardCTypes { get; }
        
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.TypedefConverters.Add(ConvertTypedef);
        }

        public CSharpElement ConvertTypedef(CSharpConverter converter, CppTypedef cppTypedef, CSharpElement context)
        {
            var elementType = cppTypedef.ElementType;

            if (converter.Options.AutoConvertStandardCTypes && StandardCTypes.TryGetValue(cppTypedef.Name, out var funcStandardType))
            {
                return funcStandardType();
            }

            var csElementType = converter.GetCSharpType(elementType, context, true);

            var noWrap = converter.Options.TypedefCodeGenKind == CppTypedefCodeGenKind.NoWrap && !converter.Options.TypedefWrapForceList.Contains(cppTypedef.Name);

            var csElementTypeName = converter.ConvertTypeReferenceToString(csElementType, out var attachedAttributes);

            var csStructName = converter.GetCSharpName(cppTypedef, context);
            var csStruct = new CSharpStruct(csStructName)
            {
                CppElement = cppTypedef,
            };

            if (noWrap || csStruct.IsOpaque || csElementTypeName == "void")
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
