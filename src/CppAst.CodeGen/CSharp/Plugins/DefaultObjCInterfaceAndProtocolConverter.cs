// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq; 

namespace CppAst.CodeGen.CSharp;

public class DefaultObjCInterfaceAndProtocolConverter : ICSharpConverterPlugin
{
    public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
    {
        pipeline.ClassConverters.Add(ConvertClass);
    }

    public static CSharpElement? ConvertClass(CSharpConverter converter, CppClass cppClass, CSharpElement context)
    {
        // This converter supports only plain struct or union
        if (!(cppClass.ClassKind == CppClassKind.ObjCInterface ||
            cppClass.ClassKind == CppClassKind.ObjCInterfaceCategory ||
            cppClass.ClassKind == CppClassKind.ObjCProtocol)
            )
        {
            return null;
        }

        // We don't generate any code but use default ObjCRuntime.ObjCProtocol
        if (cppClass.Name == "Protocol" && cppClass.ClassKind == CppClassKind.ObjCInterface)
        {
            return new CSharpFreeType("ObjCRuntime.ObjCProtocol")
            {
                CppElement = cppClass,
            };
        }

        // Register the struct as soon as possible
        var csStructName = converter.GetCSharpName(cppClass, context);

        CSharpElement? csResult = null;
        CSharpInterface? csInterface = null;
        var container = converter.GetCSharpContainer(cppClass, context);

        if (cppClass.ClassKind == CppClassKind.ObjCProtocol)
        {
            csInterface = new CSharpInterface($"I{csStructName}")
            {
                CppElement = cppClass,
            };
            converter.Register(cppClass, csInterface);

            var parameterType = new CSharpGenericParameterType("TInstanceType")
            {
                IsOut = true
            };
            parameterType.WhereClauses.Add(new CSharpWhereClause(new CSharpGenericTypeReference(new CSharpSimpleNameReferenceType(csInterface))
            {
                TypeArguments =
                {
                    parameterType
                }
            }));

            csInterface.GenericParameters.Add(parameterType);
            converter.ApplyDefaultVisibility(csInterface, container);

            var csStruct = new CSharpStruct($"ObjCObject_{csStructName}")
            {
                CppElement = cppClass,
                IsRecord = true,
                Modifiers = CSharpModifiers.ReadOnly | CSharpModifiers.Partial
            };
            csStruct.BaseTypes.Add(new CSharpFreeType("ObjCRuntime.IObjCObject"));
            csStruct.PrimaryConstructorParameters.Add(new CSharpParameter("Handle")
            {
                ParameterType = CSharpPrimitiveType.IntPtr(),
            });
            csStruct.Comment = converter.GetCSharpComment(cppClass, csStruct);
            csStruct.BaseTypes.Add(new CSharpGenericTypeReference(new CSharpSimpleNameReferenceType(csInterface))
            {
                TypeArguments =
                {
                    new CSharpSimpleNameReferenceType(csStruct)
                }
            });
            container.Members.Add(csStruct);
            csInterface.ObjCProtocolDefaultImpl = csStruct;

            csResult = csInterface;
        }
        else if (cppClass.ClassKind == CppClassKind.ObjCInterface)
        {
            var csStruct = new CSharpStruct(csStructName)
            {
                CppElement = cppClass,
                IsRecord = true,
                Modifiers = CSharpModifiers.ReadOnly | CSharpModifiers.Partial,
            };

            csStruct.PrimaryConstructorParameters.Add(new CSharpParameter("Handle")
            {
                ParameterType = CSharpPrimitiveType.IntPtr(),
            });

            foreach (var cppTemplateParamType in cppClass.TemplateParameters)
            {
                var csTemplateParamType = (CSharpGenericParameterType)converter.GetCSharpType(cppTemplateParamType, context);

                if (cppClass.ClassKind == CppClassKind.ObjCInterface)
                {
                    csTemplateParamType.WhereClauses.Add(new("unmanaged"));
                    csTemplateParamType.WhereClauses.Add(new(new CSharpFreeType("ObjCRuntime.IObjCObject")));
                }

                csStruct.GenericParameters.Add(csTemplateParamType);
            }
            
            var csStructRef = new CSharpGenericTypeReference(csStruct);
            csStructRef.TypeArguments.AddRange(csStruct.GenericParameters.Select(x => new CSharpGenericParameterType(x.Name)));
            
            converter.Register(cppClass, csStruct);

            if (cppClass.ClassKind == CppClassKind.ObjCInterface)
            {
                csStruct.Members.Add(new CSharpLineElement("public static ObjCRuntime.ObjCClass ClassStatic => throw new NotImplementedException();")); // TODO: Implement
            }

            converter.ApplyDefaultVisibility(csStruct, container);

            csInterface = new CSharpInterface($"ObjCInterface")
            {
                CppElement = cppClass,
                Modifiers = CSharpModifiers.Partial,
                Visibility = CSharpVisibility.Public,
            };
            csStruct.Members.Add(csInterface);

            var parameterType = new CSharpGenericParameterType("TInstanceType")
            {
                IsOut = true
            };
            parameterType.WhereClauses.Add(new CSharpWhereClause(new CSharpGenericTypeReference(new CSharpSimpleNameReferenceType(csInterface))
            {
                TypeArguments =
                {
                    parameterType
                }
            }));
            csInterface.GenericParameters.Add(parameterType);

            if (cppClass.BaseTypes.Count == 0)
            {
                csInterface.BaseTypes.Add(new CSharpFreeType("ObjCRuntime.IObjCInterface"));
            }
            else
            {
                foreach (var cppBaseType in cppClass.BaseTypes)
                {
                    var baseType = converter.GetCSharpType(cppBaseType.Type, context);
                    if (baseType is CSharpStruct cSharpStruct)
                    {
                        var baseInterface = cSharpStruct.Members.OfType<CSharpInterface>().First();

                        csInterface.BaseTypes.Add(new CSharpGenericTypeReference(baseInterface)
                        {
                            TypeArguments =
                            {
                                parameterType
                            }
                        });
                    }
                    else if (baseType is CSharpGenericTypeReference typeRef)
                    {
                        var csStructBaseType = (CSharpStruct)typeRef.BaseType;
                        var csInterfaceBaseType = csStructBaseType.Members.OfType<CSharpInterface>().First();

                        var genericTypeRef = new CSharpGenericTypeReference(csInterfaceBaseType);
                        foreach(var typeArg in typeRef.TypeArguments)
                        {
                            genericTypeRef.TypeArguments.Add(typeArg);
                        }
                        csInterface.BaseTypes.Add(genericTypeRef);
                    }
                    else
                    {
                        Debug.Assert(false, $"Unsupported base type {baseType} for {cppClass.Name}");
                    }
                }
            }

            csStruct.BaseTypes.Add(new CSharpGenericTypeReference(csInterface)
            {
                TypeArguments =
                {
                    new CSharpSimpleNameReferenceType(csStruct)
                }
            });

            csResult = csStruct;
        }
        else
        {
            var csStruct = (CSharpStruct)converter.GetCSharpType(cppClass.ObjCCategoryTargetClass!, context);
            csInterface = csStruct.Members.OfType<CSharpInterface>().First();
        }

        // If an ObjC interface has template parameters, extension members don't support multiple generic parameters
        // So we need to create a specialized extension for these generic ObjC types
        // Issue: https://github.com/dotnet/roslyn/issues/78472
        CSharpExtension extension;
        var targetCppClass = cppClass.ClassKind == CppClassKind.ObjCInterfaceCategory ? cppClass.ObjCCategoryTargetClass : cppClass;
        if (targetCppClass.ClassKind == CppClassKind.ObjCInterface && targetCppClass.TemplateParameters.Count > 0)
        {
            var targetType = (CSharpStruct?)csResult;
            if (cppClass.ClassKind == CppClassKind.ObjCInterfaceCategory)
            {
                targetType = (CSharpStruct)converter.GetCSharpType(cppClass.ObjCCategoryTargetClass!, context)!;
            }

            var csStructGeneric = new CSharpGenericTypeReference(targetType!);
            extension = new CSharpExtension(new CSharpParameter(csStructGeneric, "@this"));
            foreach (var cppTemplateParamType in targetCppClass.TemplateParameters)
            {
                var csTemplateParamType = (CSharpGenericParameterType)converter.GetCSharpType(cppTemplateParamType, context);

                //if (cppClass.ClassKind == CppClassKind.ObjCInterface)
                //{
                //    csTemplateParamType.WhereClauses.Add(new("unmanaged"));
                //    csTemplateParamType.WhereClauses.Add(new(new CSharpFreeType("ObjCRuntime.IObjCObject")));
                //}

                csStructGeneric.TypeArguments.Add(new CSharpGenericParameterType(csTemplateParamType.Name));
                extension.GenericParameters.Add(csTemplateParamType);
            }
        }
        else
        {
            var extensionParameterType = new CSharpGenericParameterType("TInstanceType");
            extensionParameterType.WhereClauses.Add(new CSharpWhereClause(new CSharpGenericTypeReference(csInterface)
            {
                TypeArguments =
            {
                extensionParameterType
            }
            }));
            extension = new CSharpExtension(new CSharpParameter(extensionParameterType, "@this"));
            extension.GenericParameters.Add(extensionParameterType);
        }

        if (csResult is null)
        {
            csResult = extension;
        }
        else
        {
            container.Members.Add(extension);
            ((CSharpTypeWithMembers)csResult).LinkedExtension = extension;
        }
        container.Members.Add(csResult!);

        // Add all protocols interfaces that this interface/protocol inherits from
        if (cppClass.ObjCImplementedProtocols.Count == 0)
        {
            if (cppClass.ClassKind == CppClassKind.ObjCProtocol)
            {
                csInterface.BaseTypes.Add(new CSharpFreeType("ObjCRuntime.IObjCProtocol"));
            }
        }
        else
        {
            foreach (var cppBaseType in cppClass.ObjCImplementedProtocols)
            {
                var baseType = converter.GetCSharpType(cppBaseType, context);
                csInterface.BaseTypes.Add(new CSharpGenericTypeReference(baseType)
                {
                    TypeArguments =
                    {
                        csInterface.GenericParameters[0]
                    }
                });
            }
        }

        return csResult;
    }
}