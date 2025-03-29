// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultTypedefConverter : ICSharpConverterPlugin
    {
        private DefaultTypeConverter? _defaultTypeConverter;

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
            _defaultTypeConverter = converter.Options.Plugins.OfType<DefaultTypeConverter>().FirstOrDefault();
        }

        public CSharpElement ConvertTypedef(CSharpConverter converter, CppTypedef cppTypedef, CSharpElement context)
        {
            var elementType = cppTypedef.ElementType;

            // Use the default type converter if available
            if (_defaultTypeConverter is not null && _defaultTypeConverter.MapCppToCSharpType.TryGetValue(cppTypedef.Name, out var csType))
            {
                return csType;
            }

            if (converter.Options.AutoConvertStandardCTypes && StandardCTypes.TryGetValue(cppTypedef.Name, out var funcStandardType))
            {
                return funcStandardType();
            }

            // For typedef we need to resolve it from the container perspective, not from its usage (e.g field)
            if (!(context is ICSharpContainer))
            {
                context = converter.CurrentCSharpCompilation!;
            }
            
            var csElementType = converter.GetCSharpType(elementType, context, true);

            var noWrap = converter.Options.TypedefCodeGenKind == CppTypedefCodeGenKind.NoWrap && !converter.Options.TypedefWrapForceList.Contains(cppTypedef.Name);

            var csStructName = converter.GetCSharpName(cppTypedef, context);
            var csStruct = new CSharpStruct(csStructName)
            {
                CppElement = cppTypedef,
            };

            if (noWrap || csStruct.IsOpaque || (csElementType is CSharpPrimitiveType csPrimitive && csPrimitive.Kind == CSharpPrimitiveKind.Void))
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
            csStruct.BaseTypes.Add(new CSharpGenericTypeReference($"IEquatable", [csStruct]));

            csStruct.Members.Add(new CSharpMethod(name)
            {
                Kind = CSharpMethodKind.Constructor,
                Parameters =
                {
                    new CSharpParameter("value")
                    {
                        ParameterType = csElementType
                    }
                },
                Visibility = CSharpVisibility.Public,
                BodyInline = ((writer, _) => writer.Write("this.Value = value"))
            });
            csStruct.Members.Add(new CSharpProperty("Value")
            {
                ReturnType = csElementType,
                Visibility = CSharpVisibility.Public,
                GetterOnly = true
            });
            csStruct.Members.Add(new CSharpLineElement(() => $"public override bool Equals(object obj) => obj is {name} other && Equals(other);"));
            if (csElementType is CSharpPointerType || csElementType is CSharpFunctionPointer)
            {
                csStruct.Members.Add(new CSharpLineElement(() => $"public bool Equals({name} other) => Value == other.Value;"));
                csStruct.Members.Add(new CSharpLineElement("public override int GetHashCode() => ((nint)(void*)Value).GetHashCode();"));
                csStruct.Members.Add(new CSharpLineElement("public override string ToString() => ((nint)(void*)Value).ToString();"));
            }
            else
            {
                csStruct.Members.Add(new CSharpLineElement(() => $"public bool Equals({name} other) => Value.Equals(other.Value);"));
                csStruct.Members.Add(new CSharpLineElement("public override int GetHashCode() => Value.GetHashCode();"));
                csStruct.Members.Add(new CSharpLineElement("public override string ToString() => Value.ToString();"));
            }

            csStruct.Members.Add(new CSharpMethod(string.Empty)
            {
                Kind = CSharpMethodKind.Operator,
                ReturnType = csElementType,
                Modifiers = CSharpModifiers.Static | CSharpModifiers.Implicit,
                Parameters =
                {
                    new CSharpParameter("from") {ParameterType = csStruct},
                },
                BodyInline = ((writer, _) => writer.Write("from.Value")),
                Visibility = CSharpVisibility.Public
            });
            csStruct.Members.Add(new CSharpMethod(string.Empty)
            {
                Kind = CSharpMethodKind.Operator,
                ReturnType = csStruct,
                Modifiers = CSharpModifiers.Static | CSharpModifiers.Implicit,
                Parameters =
                {
                    new CSharpParameter("from") {ParameterType = csElementType},
                },
                BodyInline = (writer, element) =>
                {
                    writer.Write("new ");
                    csStruct.DumpReferenceTo(writer);
                    writer.Write("(");
                    writer.Write("from");
                    writer.Write(")");
                },
                Visibility = CSharpVisibility.Public
            });
            csStruct.Members.Add(new CSharpLineElement(() => $"public static bool operator ==({name} left, {name} right) => left.Equals(right);"));
            csStruct.Members.Add(new CSharpLineElement(() => $"public static bool operator !=({name} left, {name} right) => !left.Equals(right);"));


            return csStruct;
        }
    }
}
