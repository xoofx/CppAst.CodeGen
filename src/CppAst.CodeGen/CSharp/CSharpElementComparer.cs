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
            case CSharpParameter cSharpParameter:
            {
                return Compare(cSharpParameter, (CSharpParameter)right, visitedElement);
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
                if (!CompareReference(cSharpGenericTypeReference.BaseType, ((CSharpGenericTypeReference)right).BaseType, visitedElement))
                {
                    return false;
                }

                if (cSharpGenericTypeReference.TypeArguments.Count != ((CSharpGenericTypeReference)right).TypeArguments.Count)
                {
                    return false;
                }

                for (int i = 0; i < cSharpGenericTypeReference.TypeArguments.Count; i++)
                {
                    if (!Compare(cSharpGenericTypeReference.TypeArguments[i], ((CSharpGenericTypeReference)right).TypeArguments[i], visitedElement))
                    {
                        return false;
                    }
                }

                return true;
            }

            case CSharpGenericParameterType cSharpGenericParameterType:
                {
                    if (cSharpGenericParameterType.Name != ((CSharpGenericParameterType)right).Name)
                    {
                        return false;
                    }

                    if (cSharpGenericParameterType.IsOut != ((CSharpGenericParameterType)right).IsOut)
                    {
                        return false;
                    }

                    if (!CompareWhereClauses(cSharpGenericParameterType.WhereClauses, ((CSharpGenericParameterType)right).WhereClauses, visitedElement))
                    {
                        return false;
                    }

                    return true;
                }

            case CSharpSimpleNameReferenceType cSharpSimpleNameReferenceType:
                return cSharpSimpleNameReferenceType.BaseType.GetType() == ((CSharpSimpleNameReferenceType)right).BaseType.GetType()
                       && cSharpSimpleNameReferenceType.BaseType.Name == ((CSharpSimpleNameReferenceType)right).BaseType.Name;

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

                if (csWithMembers.IsRecord != ((CSharpTypeWithMembers)right).IsRecord)
                {
                    return false;
                }

                if (csWithMembers.ForcePrimaryConstructorParameters != ((CSharpTypeWithMembers)right).ForcePrimaryConstructorParameters)
                {
                    return false;
                }

                if (!CompareGenericParameters(csWithMembers.GenericParameters, ((CSharpTypeWithMembers)right).GenericParameters, visitedElement))
                {
                    return false;
                }

                if (!CompareTypeReferences(csWithMembers.BaseTypes, ((CSharpTypeWithMembers)right).BaseTypes, visitedElement))
                {
                    return false;
                }

                if ((csWithMembers.IsRecord || csWithMembers.ForcePrimaryConstructorParameters) && !CompareParameters(csWithMembers.PrimaryConstructorParameters, ((CSharpTypeWithMembers)right).PrimaryConstructorParameters, visitedElement))
                {
                    return false;
                }

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

    private static bool CompareParameters(IReadOnlyList<CSharpParameter> left, IReadOnlyList<CSharpParameter> right, HashSet<CSharpType>? visitedElement)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!Compare(left[i], right[i], visitedElement))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Compare(CSharpParameter left, CSharpParameter right, HashSet<CSharpType>? visitedElement)
    {
        if (left.Name != right.Name || left.DefaultValue != right.DefaultValue || left.IsParams != right.IsParams || left.IsThis != right.IsThis)
        {
            return false;
        }

        if (left.ParameterType is null || right.ParameterType is null)
        {
            return left.ParameterType is null && right.ParameterType is null;
        }

        return Compare(left.ParameterType, right.ParameterType, visitedElement);
    }

    private static bool CompareGenericParameters(IReadOnlyList<CSharpGenericParameterType> left, IReadOnlyList<CSharpGenericParameterType> right, HashSet<CSharpType>? visitedElement)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!Compare(left[i], right[i], visitedElement))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CompareTypeReferences(IReadOnlyList<CSharpType> left, IReadOnlyList<CSharpType> right, HashSet<CSharpType>? visitedElement)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!CompareReference(left[i], right[i], visitedElement))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CompareReference(CSharpType left, CSharpType right, HashSet<CSharpType>? visitedElement)
    {
        if (left.GetType() != right.GetType())
        {
            return false;
        }

        if (left is CSharpNamedType leftNamedType && right is CSharpNamedType rightNamedType)
        {
            return leftNamedType.Name == rightNamedType.Name;
        }

        if (left is CSharpSimpleNameReferenceType leftSimpleReference && right is CSharpSimpleNameReferenceType rightSimpleReference)
        {
            return leftSimpleReference.BaseType.GetType() == rightSimpleReference.BaseType.GetType()
                   && leftSimpleReference.BaseType.Name == rightSimpleReference.BaseType.Name;
        }

        return Compare(left, right, visitedElement);
    }

    private static bool CompareWhereClauses(IReadOnlyList<CSharpWhereClause> left, IReadOnlyList<CSharpWhereClause> right, HashSet<CSharpType>? visitedElement)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i].Constraint != right[i].Constraint)
            {
                return false;
            }

            var leftType = left[i].Type;
            var rightType = right[i].Type;
            if (leftType is null || rightType is null)
            {
                if (leftType is not null || rightType is not null)
                {
                    return false;
                }

                continue;
            }

            if (!CompareReference(leftType, rightType, visitedElement))
            {
                return false;
            }
        }

        return true;
    }
}
