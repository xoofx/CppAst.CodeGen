// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class DefaultGetCSharpNamePlugin : ICSharpConverterPlugin
    {

        private readonly HashSet<CppClass> _tempOtherClasses;

        public DefaultGetCSharpNamePlugin()
        {
            _tempOtherClasses = new HashSet<CppClass>();
        }

        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.GetCSharpNameResolvers.Add(GetCSharpName);
        }

        protected virtual string GetCSharpName(CSharpConverter converter, CppElement element, CSharpElement context)
        {
            var name = string.Empty;

            // Try to get the name directly from the CppElement
            if (element is ICppMember member)
            {
                name = member.Name;
            }

            if (element is CppFunction cppFunction && cppFunction.Parent is CppClass cppClass && 
                (cppClass.ClassKind == CppClassKind.ObjCInterface ||
                 cppClass.ClassKind == CppClassKind.ObjCInterfaceCategory ||
                 cppClass.ClassKind == CppClassKind.ObjCProtocol))
            {
                // For ObjC interface, we keep only the name before the first :
                var nameBuilder = new StringBuilder();
                var nameParts = name.Split(':', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < nameParts.Length; i++)
                {
                    var namePart = nameParts[i];
                    if (nameBuilder.Length > 0)
                    {
                        nameBuilder.Append(namePart[0].ToString().ToUpperInvariant());
                        nameBuilder.Append(namePart.Substring(1));
                    }
                    else
                    {
                        nameBuilder.Append(namePart);
                    }
                }

                name = nameBuilder.ToString();

                // Some ObjC methods can have a static and non static version with the same name, so we append "Static" on the static if required.
                if ((cppFunction.Flags & CppFunctionFlags.ClassMethod) != 0)
                {
                    _tempOtherClasses.Clear();
                    _tempOtherClasses.Add(cppClass);

                    // Wwe need to visit also categories that can add methods that can conflict with a method defined in another class
                    if (cppClass.ObjCCategoryTargetClass != null)
                    {
                        _tempOtherClasses.Add(cppClass.ObjCCategoryTargetClass);

                        foreach (var otherCppClass in cppClass.ObjCCategoryTargetClass.ObjCCategories)
                        {
                            _tempOtherClasses.Add(otherCppClass);
                        }
                    }

                    foreach (var otherClass in _tempOtherClasses)
                    {
                        if (otherClass.Functions.Any(x => x.Name == cppFunction.Name && (x.Flags & CppFunctionFlags.ClassMethod) == 0))
                        {
                            name = $"{name}Static";
                            break;
                        }
                    }
                }
            }

            // If it is null, try to get a contextual name from the context
            if (string.IsNullOrEmpty(name))
            {
                if (context is ICSharpMember csMember)
                {
                    name = csMember.Name;
                }

                if (!string.IsNullOrEmpty(name))
                {
                    // Handle the case for union types where the field name is auto-generated from the union name
                    // and could have a conflict with it. Add the index of the field to the name to avoid conflicts
                    if (context.CppElement is ICppDeclarationContainer cppContainer)
                    {
                        int indexOfElement;
                        string? kind = "";

                        if (element is CppField cppField)
                        {
                            indexOfElement = cppContainer.Fields.IndexOf(cppField);
                            kind = "field_";
                        }
                        else if (element is CppClass subclass)
                        {
                            indexOfElement = cppContainer.Classes.IndexOf(subclass);
                            kind = $"{subclass.ClassKind.ToString().ToLowerInvariant()}_";
                        }
                        else if (element is CppFunction cppFunction2)
                        {
                            indexOfElement = cppContainer.Functions.IndexOf(cppFunction2);
                            kind = "func_";
                        }
                        else if (element is CppEnum cppEnum)
                        {
                            indexOfElement = cppContainer.Enums.IndexOf(cppEnum);
                            kind = "enum_";
                        }
                        else if (element is CppTypedef cppTypedef)
                        {
                            indexOfElement = cppContainer.Typedefs.IndexOf(cppTypedef);
                            kind = "typedef_";
                        }
                        else
                        {
                            indexOfElement = -1;
                        }

                        if (indexOfElement >= 0)
                        {
                            name = $"{name}__{kind}{indexOfElement}";
                        }
                    }
                }
            }

            // If the name is null, we create an anonymous type name that includes the type, file name, and file offset
            if (string.IsNullOrEmpty(name))
            {
                var fileName = Path.GetFileNameWithoutExtension(element.Span.Start.File);
                name = $"__Anonymous{element.GetType().Name}_{fileName}_{element.Span.Start.Offset}";
            }

            return name;
        }
    }
}