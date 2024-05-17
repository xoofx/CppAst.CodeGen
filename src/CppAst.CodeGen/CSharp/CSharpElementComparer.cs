// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.CSharp;
using System.Collections.Generic;
using System.Linq;

namespace CppAst.CodeGen.CSharp;

public class CSharpElementComparer
{
    public static bool Compare(CSharpElement left, CSharpElement right)
    {
        return Compare(left, right, null);
    }

    private static bool Compare(CSharpElement left, CSharpElement right, HashSet<CSharpType>? visitedElement)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        switch (left)
        {
            case CSharpEnumItem cSharpEnumItem:
            {
                return cSharpEnumItem.Value == ((CSharpEnumItem)right).Value;
            }

            case CSharpField cSharpField:
            {
                if (!Compare(cSharpField.FieldType!, ((CSharpField)right).FieldType!, visitedElement))
                {
                    return false;
                }

                if (!string.Equals(cSharpField.InitValue, ((CSharpField)right).InitValue, StringComparison.Ordinal))
                {
                    return false;
                }

                return cSharpField.Name == ((CSharpField)right).Name;
            }

            case CSharpProperty cSharpProperty:
            {
                if (!Compare(cSharpProperty.ReturnType!, ((CSharpProperty)right).ReturnType!, visitedElement))
                {
                    return false;
                }

                return cSharpProperty.Name == ((CSharpProperty)right).Name;
            }

            case CSharpMethod csSharpMethod:
            {
                if (csSharpMethod.Parameters.Count != ((CSharpMethod)right).Parameters.Count)
                {
                    return false;
                }

                if (!Compare(csSharpMethod.ReturnType!, ((CSharpMethod)right).ReturnType!, visitedElement))
                {
                    return false;
                }

                for (int i = 0; i < csSharpMethod.Parameters.Count; i++)
                {
                    if (csSharpMethod.Parameters[i].Name != ((CSharpMethod)right).Parameters[i].Name)
                    {
                        return false;
                    }

                    if (!Compare(csSharpMethod.Parameters[i].ParameterType!, ((CSharpMethod)right).Parameters[i].ParameterType!, visitedElement))
                    {
                        return false;
                    }
                }

                return true;
            }
            case CSharpType csType:
            {
                return Compare(csType, (CSharpType)right, visitedElement);
            }
        }

        throw new NotImplementedException($"{left.GetType()} comparison is not implemented");
    }

    private static bool Compare(CSharpType left, CSharpType right, HashSet<CSharpType>? visitedElement)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        
        if (left.GetType() != right.GetType())
        {
            return false;
        }

        // Don't continue visiting an element that we already visited
        if (left is CSharpStruct && visitedElement != null)
        {
            if (!visitedElement.Add(left))
            {
                return true;
            }
        }

        switch (left)
        {
            case CSharpGenericTypeReference cSharpGenericTypeReference:
            {
                if (cSharpGenericTypeReference.Name != ((CSharpGenericTypeReference)right).Name)
                {
                    return false;
                }

                if (cSharpGenericTypeReference.TypeArguments.Length != ((CSharpGenericTypeReference)right).TypeArguments.Length)
                {
                    return false;
                }

                for (int i = 0; i < cSharpGenericTypeReference.TypeArguments.Length; i++)
                {
                    if (!Compare(cSharpGenericTypeReference.TypeArguments[i], ((CSharpGenericTypeReference)right).TypeArguments[i], visitedElement))
                    {
                        return false;
                    }
                }

                return true;
            }

            case CSharpFreeType cSharpFreeType:
                return cSharpFreeType.Text == ((CSharpFreeType)right).Text;

            case CSharpEnum csEnum:
            {
                var leftItems = csEnum.Members.OfType<CSharpEnumItem>().ToDictionary(p => p.Name, p => p);
                var rightItems = ((CSharpEnum)right).Members.OfType<CSharpEnumItem>().ToDictionary(p => p.Name, p => p);

                if (leftItems.Count != rightItems.Count)
                {
                    return false;
                }

                foreach (var key in leftItems.Keys)
                {
                    if (!rightItems.TryGetValue(key, out var rightItem))
                    {
                        return false;
                    }

                    if (!Compare(leftItems[key], rightItem, visitedElement))
                    {
                        return false;
                    }
                }

                return true;
            }

            case CSharpTypeWithMembers csWithMembers:
            {
                visitedElement ??= new HashSet<CSharpType>();

                // Process fields
                var leftAllFields = csWithMembers.Members.OfType<CSharpField>().ToList();
                var rightAllFields = ((CSharpTypeWithMembers)right).Members.OfType<CSharpField>().ToList();

                // Process instance fields
                var leftInstanceFields = leftAllFields.Where(x => (x.Modifiers & (CSharpModifiers.Const | CSharpModifiers.Static)) == 0).ToList();
                var rightInstanceFields = rightAllFields.Where(x => (x.Modifiers & (CSharpModifiers.Const | CSharpModifiers.Static)) == 0).ToList();
                if (leftInstanceFields.Count != rightInstanceFields.Count)
                {
                    return false;
                }

                for (int i = 0; i < leftInstanceFields.Count; i++)
                {
                    if (!Compare(leftInstanceFields[i], rightInstanceFields[i], visitedElement))
                    {
                        return false;
                    }
                }
                
                // Process properties
                var leftProperties = csWithMembers.Members.OfType<CSharpProperty>().ToDictionary(p => p.Name, p => p);
                var rightProperties = ((CSharpTypeWithMembers)right).Members.OfType<CSharpProperty>().ToDictionary(p => p.Name, p => p);

                if (leftProperties.Count != rightProperties.Count)
                {
                    return false;
                }

                foreach (var key in leftProperties.Keys)
                {
                    if (!rightProperties.TryGetValue(key, out var rightProperty))
                    {
                        return false;
                    }

                    if (!Compare(leftProperties[key], rightProperty, visitedElement))
                    {
                        return false;
                    }
                }
                
                return true;
            }

            case CSharpDelegate cSharpDelegate:
            {
                if (cSharpDelegate.Parameters.Count != ((CSharpDelegate)right).Parameters.Count)
                {
                    return false;
                }

                if (!Compare(cSharpDelegate.ReturnType!, ((CSharpDelegate)right).ReturnType!, visitedElement))
                {
                    return false;
                }

                for (int i = 0; i < cSharpDelegate.Parameters.Count; i++)
                {
                    if (!Compare(cSharpDelegate.Parameters[i].ParameterType!, ((CSharpDelegate)right).Parameters[i].ParameterType!, visitedElement))
                    {
                        return false;
                    }
                }

                return true;
            }
            case CSharpFixedArrayType cSharpFixedArrayType:
            {
                if (cSharpFixedArrayType.Size != ((CSharpFixedArrayType)right).Size)
                {
                    return false;
                }

                return Compare(cSharpFixedArrayType.ElementType, ((CSharpFixedArrayType)right).ElementType, visitedElement);
            }
            case CSharpPrimitiveType primitiveType:
                return primitiveType.Kind == ((CSharpPrimitiveType)right).Kind;
            case CSharpRefType cSharpRefType:
            {
                if (cSharpRefType.Kind != ((CSharpRefType)right).Kind)
                {
                    return false;
                }

                return Compare(cSharpRefType.ElementType, ((CSharpRefType)right).ElementType, visitedElement);
            }
            case CSharpTypeWithAttributes cSharpTypeWithAttributes:
            {
                //if (cSharpTypeWithAttributes.Attributes.Count != ((CSharpTypeWithAttributes)right).Attributes.Count)
                //{
                //    return false;
                //}

                //for (int i = 0; i < cSharpTypeWithAttributes.Attributes.Count; i++)
                //{
                //    if (!Compare(cSharpTypeWithAttributes.Attributes[i], ((CSharpTypeWithAttributes)right).Attributes[i], visitedElement))
                //    {
                //        return false;
                //    }
                //}

                return Compare(cSharpTypeWithAttributes.ElementType, ((CSharpTypeWithAttributes)right).ElementType, visitedElement);
            }
            case CSharpPointerType pointerType:
                return Compare(pointerType.ElementType, ((CSharpPointerType)right).ElementType, visitedElement);
            case CSharpArrayType arrayType:
                return Compare(arrayType.ElementType, ((CSharpArrayType)right).ElementType, visitedElement);
            case CSharpFunctionPointer functionPointerType:
            {
                if (functionPointerType.Parameters.Count != ((CSharpFunctionPointer)right).Parameters.Count)
                {
                    return false;
                }

                if (!Compare(functionPointerType.ReturnType, ((CSharpFunctionPointer)right).ReturnType, visitedElement))
                {
                    return false;
                }
                
                for (int i = 0; i < functionPointerType.Parameters.Count; i++)
                {
                    if (!Compare(functionPointerType.Parameters[i].ParameterType!, ((CSharpFunctionPointer)right).Parameters[i].ParameterType!, visitedElement))
                    {
                        return false;
                    }
                }

                return true;
            }
            case CSharpNullableType cSharpNullableType:
            {
                return Compare(cSharpNullableType.ElementType, ((CSharpNullableType)right).ElementType, visitedElement);
            }
        }

        throw new NotImplementedException($"{left.GetType()} comparison is not implemented");
    }
}
