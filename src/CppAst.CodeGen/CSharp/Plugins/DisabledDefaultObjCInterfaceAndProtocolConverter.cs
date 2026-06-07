// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Diagnostics;
using System.Linq; 

namespace CppAst.CodeGen.CSharp;

public class DisabledDefaultObjCInterfaceAndProtocolConverter : ICSharpConverterPlugin
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
        if (cppClass.Name == "Protocol" && cppClass.ClassKind == CppClassKind.ObjCProtocol)
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
            // Protocol
            // public interface INSObject<out TInstanceType> : ObjCRuntime.IObjCProtocol<TInstanceType> where TInstanceType: INSObject<TInstanceType>;

            csInterface = new CSharpInterface(csStructName)
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
                    csStruct
                }
            });
            container.Members.Add(csStruct);

            csResult = csInterface;
        }
        else if (cppClass.ClassKind == CppClassKind.ObjCInterface)
        {
            // Interface
            // public readonly partial record struct NSObject(nint Handle) : NSObject.IObjCInterface<NSObject>
            // {
            //     public static ObjCClass ClassStatic => __Class.Value;
            // 
            //     private static class __Class
            //     {
            //         public static readonly ObjCClass Value = ObjCRuntime.objc_lookUpClass("NSObject"u8);
            //     }
            // 
            //     public interface IObjCInterface<out TInstanceType> : ObjCRuntime.IObjCInterface<TInstanceType>, INSObject<TInstanceType> where TInstanceType : IObjCInterface<TInstanceType>;
            // }
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
            
            converter.Register(cppClass, csStruct);

            csStruct.Members.Add(new CSharpLineElement("public static ObjCRuntime.ObjCClass ClassStatic => throw new NotImplementedException();")); // TODO: Implement

            converter.ApplyDefaultVisibility(csStruct, container);

            csInterface = new CSharpInterface("IObjCInterface")
            {
                CppElement = cppClass,
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
                        
            // Add all base interfaces
            if (cppClass.BaseTypes.Count == 0)
            {
                // Only NSObject should be in that case
                csInterface.BaseTypes.Add(new CSharpGenericTypeReference(new CSharpFreeType("ObjCRuntime.IObjCInterface"))
                {
                    TypeArguments = { parameterType }
                });
            }
            else
            {
                foreach (var cppBaseType in cppClass.BaseTypes)
                {
                    var baseType = (CSharpStruct)converter.GetCSharpType(cppBaseType.Type, context);
                    // We fetch the interface instead of a struct
                    var baseInterface = baseType.Members.OfType<CSharpInterface>().First();
                    csInterface.BaseTypes.Add(new CSharpGenericTypeReference(baseInterface)
                    {
                        TypeArguments =
                    {
                        parameterType
                    }
                    });
                }
            }

            csStruct.BaseTypes.Add(new CSharpGenericTypeReference(csInterface)
            {
                TypeArguments =
                {
                    csStruct
                }
            });

            csResult = csStruct;
        }
        else
        {
            Debug.Assert(cppClass.ClassKind == CppClassKind.ObjCInterfaceCategory);

            // Interface Category Members Container (No struct/interface generated for it)
            // extension<TInstanceType>(TInstanceType @this) where TInstanceType: NSObject.IObjCInterface<TInstanceType>
            // {
            //     ... members
            // }
            var csStruct = (CSharpStruct)converter.GetCSharpType(cppClass.ObjCCategoryTargetClass!, context);
            csInterface = csStruct.Members.OfType<CSharpInterface>().First();
        }


        // Create the interface for ObjCInterface / ObjCProtocol used by extension methods/properties
        // Protocol Members Container (Put in another top level static class)
        // extension<TInstanceType>(TInstanceType @this) where TInstanceType: INSObject<TInstanceType>
        // {
        //     ... members
        // }

        // Interface Members Container
        // extension<TInstanceType>(TInstanceType @this) where TInstanceType: NSObject.IObjCInterface<TInstanceType>
        // {
        //     ... members
        // }
        var extensionParameterType = new CSharpGenericParameterType("TInstanceType");
        extensionParameterType.WhereClauses.Add(new CSharpWhereClause(new CSharpGenericTypeReference(csInterface)
        {
            TypeArguments =
            {
                extensionParameterType
            }
        }));
        var extension = new CSharpExtension(new CSharpParameter(extensionParameterType, "@this"));
        extension.GenericParameters.Add(extensionParameterType);
        
        if (csResult is null)
        {
            csResult = extension;
        }

        //if (csResult is CSharpTypeWithMembers typeWithMembers)
        //{
        //    typeWithMembers.AssociatedExtension = extension;
        //}

        Debug.Assert(csResult is not null);
        if (csResult != extension)
        {
            container.Members.Add(csResult!);
        }
        container.Members.Add(extension);

        // Add all protocols interfaces that this interface/protocol inherits from
        if (cppClass.ObjCImplementedProtocols.Count == 0)
        {
            if (cppClass.ClassKind == CppClassKind.ObjCProtocol)
            {
                csInterface!.BaseTypes.Add(new CSharpGenericTypeReference("ObjCRuntime.IObjCProtocol")
                {
                    TypeArguments =
                    {
                        csInterface.GenericParameters[0]
                    }
                });
            }
        }
        else
        {
            foreach (var cppBaseType in cppClass.ObjCImplementedProtocols)
            {
                var baseType = converter.GetCSharpType(cppBaseType, context);
                csInterface!.BaseTypes.Add(new CSharpGenericTypeReference(baseType)
                {
                    TypeArguments =
                        {
                        csInterface.GenericParameters[0]
                        }
                });
            }
        }

        //if (cppClass.ClassKind != CppClassKind.ObjCProtocol)
        //{
        //    if (cppClass.ObjCCategoryTargetClass is null || !converter.IsRegistered(cppClass.ObjCCategoryTargetClass))
        //    {
        //        csResult.BaseTypes.Add(new CSharpFreeType("ObjCRuntime.IObjCObject"));

        //        if (cppClass.TemplateParameters.Count > 0)
        //        {
        //            foreach (var cppTemplateParamType in cppClass.TemplateParameters)
        //            {
        //                var csTemplateParamType = (CSharpGenericParameterType)converter.GetCSharpType(cppTemplateParamType, context);

        //                csTemplateParamType.WhereClauses.Add("unmanaged");
        //                //csTemplateParamType.WhereClauses.Add("ObjCRuntime.IObjCObject");
        //                csResult.GenericParameters.Add(csTemplateParamType);
        //            }

        //            // TODO: workaround to create non-generic type, but we are missing helpers to convert with the generic version
        //            var csNonGenericClass = CSharpStruct.MakeObjCObject(csStructName, cppClass);

        //            // Add method As<TObject>() to cast to generic class
        //            var asMethod = new CSharpMethod("As")
        //            {
        //                ReturnType = new CSharpGenericTypeReference(csResult, csResult.GenericParameters.Cast<CSharpType>().ToArray()),
        //                Visibility = CSharpVisibility.Public,
        //            };
        //            asMethod.GenericParameters.AddRange(csResult.GenericParameters);
        //            asMethod.BodyInline = (writer, element) => writer.Write("new (this.Handle)");
        //            csNonGenericClass.Members.Add(asMethod);
                    
        //            container.Members.Add(csNonGenericClass);

        //            // Add implicit converter to non-generic class
        //            // public static implicit operator Foundation.NSHashTable(Foundation.NSHashTable<ObjectType> from) => new Foundation.NSHashTable(from.Handle);
        //            var implicitOperator = new CSharpMethod(string.Empty)
        //            {
        //                Kind = CSharpMethodKind.Operator,
        //                ReturnType = csNonGenericClass,
        //                Modifiers = CSharpModifiers.Implicit | CSharpModifiers.Static,
        //                Visibility = CSharpVisibility.Public,
        //            };

        //            implicitOperator.Parameters.Add(new CSharpParameter("from")
        //            {
        //                ParameterType = new CSharpGenericTypeReference(csResult, csResult.GenericParameters.Cast<CSharpType>().ToArray()),
        //            });

        //            implicitOperator.BodyInline = (writer, element) => writer.Write($"new (from.Handle)");
        //            csResult.Members.Add(implicitOperator);
        //        }

        //        csResult.PrimaryConstructorParameters.Add(new CSharpParameter("Handle")
        //        {
        //            ParameterType = CSharpPrimitiveType.IntPtr(),
        //        });
        //    }
        //    else if (cppClass.ObjCCategoryTargetClass != null)
        //    {
        //        var existingCsType = (CSharpStruct)converter.FindCSharpType(cppClass.ObjCCategoryTargetClass)!;

        //        if (cppClass.TemplateParameters.Count > 0)
        //        {
        //            for (var i = 0; i < cppClass.TemplateParameters.Count; i++)
        //            {
        //                var csTemplateParamType = new CSharpGenericParameterType(existingCsType.GenericParameters[i].Name);
        //                csResult.GenericParameters.Add(csTemplateParamType);
        //            }
        //        }
        //    }
        //}
        
        //csResult.Comment = converter.GetCSharpComment(cppClass, csResult);

        // Add comment about CppClassKind.ObjCInterfaceCategory
        
        return csResult;
    }
}