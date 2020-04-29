// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.
namespace CppAst.CodeGen.CSharp
{
    public class DefaultEnumItemConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.EnumItemConverters.Add(ConvertEnumItem);
        }

        public static CSharpElement ConvertEnumItem(CSharpConverter converter, CppEnumItem cppEnumItem, CSharpElement context)
        {
            // If the context is not an enum, we don't support this scenario.
            if (!(context is CSharpEnum csEnum)) return null;

            var enumItemName = converter.GetCSharpName(cppEnumItem, context);
            var csEnumItem = new CSharpEnumItem(enumItemName)
            {
                CppElement = cppEnumItem
            };
            csEnum.Members.Add(csEnumItem);
            csEnumItem.Comment = converter.GetCSharpComment(cppEnumItem, context);

            // Process any enum item value expression (e.g ENUM_ITEM = 1 << 2)
            if (cppEnumItem.ValueExpression != null)
            {
                var integerValue = converter.ConvertExpression(cppEnumItem.ValueExpression, csEnumItem, csEnum.IntegerBaseType);

                csEnumItem.Value = $"unchecked(({csEnum.IntegerBaseType}){(string.IsNullOrEmpty(integerValue) ? cppEnumItem.Value + "" : integerValue)})";

                // Tag the enum has flags
                if (!csEnum.IsFlags && csEnumItem.Value.Contains("<<"))
                {
                    csEnum.IsFlags = true;
                }

                if (csEnum.IsFlags)
                {
                    csEnumItem.Value = csEnumItem.Value.Replace("<<", $" << ({csEnum.IntegerBaseType})");
                }
            }

            if (converter.Options.GenerateEnumItemAsFields && context.Parent is CSharpClass csClass)
            {
                var csEnumItemAsField = new CSharpField(enumItemName)
                {
                    Modifiers = CSharpModifiers.Const,
                    FieldType = csEnum,
                    Comment = csEnumItem.Comment,
                    InitValue = $"{csEnum.Name}.{csEnumItem.Name}"
                };
                converter.ApplyDefaultVisibility(csEnumItemAsField, csClass);

                csClass.Members.Add(csEnumItemAsField);
            }

            return csEnumItem;
        }
    }
}
