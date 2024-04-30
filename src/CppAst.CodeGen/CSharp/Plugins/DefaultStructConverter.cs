// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultStructConverter : ICSharpConverterPlugin
    {
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.ClassConverters.Add(ConvertClass);
        }

        public static CSharpElement? ConvertClass(CSharpConverter converter, CppClass cppClass, CSharpElement context)
        {
            // This converter supports only plain struct or union
            if (cppClass.ClassKind == CppClassKind.Class && cppClass.Functions.Any(x => (x.Flags & CppFunctionFlags.Virtual) != 0))
            {
                return null;
            }

            // Register the struct as soon as possible
            var csStructName = converter.GetCSharpName(cppClass, context);
            var csStruct = new CSharpStruct(csStructName)
            {
                CppElement = cppClass,
            };

            var container = converter.GetCSharpContainer(cppClass, context);
            converter.ApplyDefaultVisibility(csStruct, container);
            if (container is CSharpInterface)
            {
                container = container.Parent;
            }

            container!.Members.Add(csStruct);

            csStruct.Comment = converter.GetCSharpComment(cppClass, csStruct);

            bool isUnion = cppClass.ClassKind == CppClassKind.Union;

            var structLayout = new CSharpStructLayoutAttribute(isUnion ? LayoutKind.Explicit : LayoutKind.Sequential);
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

            if (cppClass.BaseTypes.Count == 1)
            {
                var csBaseType = converter.GetCSharpType(cppClass.BaseTypes[0].Type, context);
                csStruct.Members.Add(new CSharpField("@base") { FieldType = csBaseType, Visibility = CSharpVisibility.Public });
            }

            // For opaque type we use a standard representation
            if (!cppClass.IsDefinition && cppClass.Fields.Count == 0)
            {
                csStruct.Modifiers |= CSharpModifiers.ReadOnly;
                if (converter.Options.DetectOpaquePointers)
                {
                    csStruct.BaseTypes.Add(new CSharpFreeType($"IEquatable<{csStruct.Name}>"));

                    csStruct.Members.Add(new CSharpLineElement($"public {csStruct.Name}(nint handle) => Handle = handle;"));
                    csStruct.Members.Add(new CSharpLineElement("public nint Handle { get; }"));
                    csStruct.Members.Add(new CSharpLineElement($"public bool Equals({csStruct.Name} other) => Handle.Equals(other.Handle);"));
                    csStruct.Members.Add(new CSharpLineElement($"public override bool Equals(object obj) => obj is {csStruct.Name} other && Equals(other);"));
                    csStruct.Members.Add(new CSharpLineElement("public override int GetHashCode() => Handle.GetHashCode();"));
                    csStruct.Members.Add(new CSharpLineElement("public override string ToString() => \"0x\" + (nint.Size == 8 ? Handle.ToString(\"X16\") : Handle.ToString(\"X8\"));"));
                    csStruct.Members.Add(new CSharpLineElement($"public static bool operator ==({csStruct.Name} left, {csStruct.Name} right) => left.Equals(right);"));
                    csStruct.Members.Add(new CSharpLineElement($"public static bool operator !=({csStruct.Name} left, {csStruct.Name} right) => !left.Equals(right);"));
                }
            }

            // If we have any anonymous structs/unions for a field type
            // try to compute a name for them before processing them
            foreach (var cppField in cppClass.Fields)
            {
                var fieldType = cppField.Type;

                if (fieldType is CppClass cppFieldTypeClass && cppFieldTypeClass.IsAnonymous && string.IsNullOrEmpty(cppFieldTypeClass.Name))
                {
                    var parentName = string.Empty;
                    if (cppFieldTypeClass.Parent is CppClass cppParentClass)
                    {
                        parentName = cppParentClass.Name;
                    }

                    if (cppFieldTypeClass.ClassKind == CppClassKind.Union)
                    {
                        parentName = parentName == string.Empty ? "union" : parentName + "_union";
                    }
                    cppFieldTypeClass.Name = $"{parentName}_{cppField.Name}";
                }
            }

            return csStruct;
        }
    }
}