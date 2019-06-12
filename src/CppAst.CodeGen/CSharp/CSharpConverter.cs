// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CppAst.CodeGen.Common;

namespace CppAst.CodeGen.CSharp
{
    public sealed class CSharpConverter
    {
        private CppCompilation _cppCompilation;
        private CSharpCompilation _csCompilation;
        private CodeWriter _csTempWriter;
        private readonly Dictionary<CppElement, CSharpElement> _mapCppToCSharp;

        private readonly CSharpConverterPipeline _pipeline;

        private CSharpConverter(CSharpConverterOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _mapCppToCSharp = new Dictionary<CppElement, CSharpElement>(CppElementReferenceEqualityComparer.Default);
            _csTempWriter = new CodeWriter(new CodeWriterOptions(null));
            Tags = new Dictionary<string, object>();

            _pipeline = new CSharpConverterPipeline(options);
            _pipeline.RegisterPlugins(this);
        }

        public CppCompilation CurrentCppCompilation => _cppCompilation;

        public CSharpCompilation CurrentCSharpCompilation => _csCompilation;

        public CSharpConverterOptions Options { get; }

        public Dictionary<string, object> Tags { get; }

        public static CSharpCompilation Convert(List<string> files, CSharpConverterOptions options)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var converter = new CSharpConverter(options);
            return converter.Run(parserOptions => CppParser.ParseFiles(files, parserOptions));
        }

        public static CSharpCompilation Convert(string text, CSharpConverterOptions options)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var converter = new CSharpConverter(options);
            return converter.Run(parserOptions => CppParser.Parse(text, parserOptions));
        }

        private CSharpCompilation Run(Func<CppParserOptions, CppCompilation> parse)
        {
            var additionalHeaders = new StringBuilder();

            if (_pipeline.AfterPreprocessing.Count > 0)
            {
                var preprocessingOptions = this.Options.Clone();
                preprocessingOptions.AdditionalArguments.Add("--preprocess");
                preprocessingOptions.ParseMacros = true;

                var cppCompilationPreprocessed = parse(preprocessingOptions);

                // Early exit if we have compilation errors
                if (!cppCompilationPreprocessed.HasErrors)
                {
                    _cppCompilation = cppCompilationPreprocessed;

                    // Let preprocessing working
                    for (var i = _pipeline.AfterPreprocessing.Count - 1; i >= 0; i--)
                    {
                        var preprocess = _pipeline.AfterPreprocessing[i];
                        preprocess(this, cppCompilationPreprocessed, additionalHeaders);
                    }
                }
            }

            var cppOptions = this.Options.Clone();

            if (additionalHeaders.Length > 0)
            {
                cppOptions.PostHeaderText = (cppOptions.PostHeaderText ?? string.Empty) + "\n" + additionalHeaders + "\n";
            }

            var cppCompilation = parse(cppOptions);

            return Convert(cppCompilation);
        }

        public object GetTagValueOrNull(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            Tags.TryGetValue(name, out var obj);
            return obj;
        }

        public T GetTagValueOrDefault<T>(string name)
        {
            var obj = GetTagValueOrNull(name);
            return obj == null ? default : obj is T objT ? objT : default;
        }

        public string ConvertTypeReferenceToString(CSharpType csType, out string attachedAttributes)
        {
            var strWriter = (StringWriter) _csTempWriter.CurrentWriter;
            strWriter.GetStringBuilder().Length = 0;
            csType.DumpReferenceTo(_csTempWriter);
            var typeReferenceName = strWriter.ToString();
            
            strWriter.GetStringBuilder().Length = 0;
            csType.DumpContextualAttributesTo(_csTempWriter);
            attachedAttributes = strWriter.ToString();

            return typeReferenceName;
        }

        private CSharpCompilation Convert(CppCompilation cppCompilation)
        {
            if (cppCompilation == null) throw new ArgumentNullException(nameof(cppCompilation));
            return (CSharpCompilation)Convert(cppCompilation, 0,  null);
        }

        private CSharpElement Convert(CppElement cppElement, CSharpElement context)
        {
            if (cppElement == null) throw new ArgumentNullException(nameof(cppElement));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Convert(cppElement, 0, context);
        }

        private CSharpElement Convert(CppElement cppElement, int index, CSharpElement context)
        {
            if (_mapCppToCSharp.TryGetValue(cppElement, out var csElement)) return csElement;

            try
            {
                // Gives a chance to modify the element before processing it
                ProcessConverting(cppElement, context);

                switch (cppElement)
                {
                    case CppCompilation cppCompilation:
                        _cppCompilation = cppCompilation;
                        _csCompilation = ConvertCompilation(cppCompilation, context);
                        _cppCompilation.Diagnostics.CopyTo(_csCompilation.Diagnostics);
                        csElement = _csCompilation;
                        break;
                    case CppEnum cppEnum:
                        csElement = TryConvertEnum(cppEnum, context);
                        break;
                    case CppEnumItem cppEnumItem:
                        csElement = TryConvertEnumItem(cppEnumItem, context);
                        break;
                    case CppClass cppClass:
                        csElement = TryConvertClass(cppClass, context);
                        break;
                    case CppField cppField:
                        csElement = TryConvertField(cppField, context);
                        break;
                    case CppFunction cppFunction:
                        csElement = TryConvertFunction(cppFunction, context);
                        break;
                    case CppFunctionType cppFunctionType:
                        csElement = TryConvertFunctionType(cppFunctionType, context);
                        break;
                    case CppParameter cppParameter:
                        csElement = TryConvertParameter(cppParameter, index, context);
                        break;
                    case CppTypedef cppTypedef:
                        csElement = TryConvertTypedef(cppTypedef, context);
                        break;
                }

                if (csElement != null)
                {
                    Register(cppElement, csElement);
                }

                if (cppElement is ICppContainer container)
                {
                    var subContext = csElement ?? context;
                    foreach (var nestedCppElement in container.Children())
                    {
                        Convert((CppElement)nestedCppElement, 0, subContext);
                    }
                }

                if (csElement != null)
                {
                    ProcessConverted(csElement, context);
                }
            }
            finally
            {
                if (cppElement is CppCompilation)
                {
                    // Reset current CppCompilation and current CSharpCompilation
                    _cppCompilation = null;
                    _csCompilation = null;
                }
            }

            return csElement;
        }
        
        public CSharpComment GetCSharpComment(CppElement element, CSharpElement context)
        {
            return null;
        }

        public string GetCSharpName(CppElement member, CSharpElement context, string defaultName = null)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            if (context == null) throw new ArgumentNullException(nameof(context));
            string name = null;
            for (var i = _pipeline.GetCSharpNameResolvers.Count - 1; i >= 0; i--)
            {
                var del = _pipeline.GetCSharpNameResolvers[i];
                name = del(this, member, context);
                if (name != null)
                {
                    break;
                }
            }

            name = name ?? (member as ICppMember)?.Name ?? (member as CppParameter)?.Name ?? defaultName ?? $"unsupported_name /* {member} */";
            return CSharpHelper.EscapeName(name);
        }

        public CSharpType GetCSharpType(CppType cppType, CSharpElement context)
        {
            if (cppType == null) throw new ArgumentNullException(nameof(cppType));
            if (context == null) throw new ArgumentNullException(nameof(context));

            CSharpType csType;
            for (var i = _pipeline.GetCSharpTypeResolvers.Count - 1; i >= 0; i--)
            {
                var getCSharpTypeDelegate = _pipeline.GetCSharpTypeResolvers[i];
                csType = getCSharpTypeDelegate(this, cppType, context);
                if (csType != null)
                {
                    return csType;
                }
            }

            csType = new CSharpFreeType($"unsupported_type /* {cppType} */");
            return csType;
        }

        //public CSharpVisibility GetCurrentCSharpVisibility(CSharpElement context)
        //{
        //    if (context == null) throw new ArgumentNullException(nameof(context));

        //    var container = GetCSharpClosestContainer(context);
        //    if (container is CSharpTypeWithMembers typeWithMembers)
        //    {
        //        return typeWithMembers.Visibility;
        //    }

        //    return CSharpVisibility.None;
        //}

        public void ApplyDefaultVisibility(ICSharpElementWithVisibility element, ICSharpContainer container)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (container == null) throw new ArgumentNullException(nameof(container));
            // By default, make element public if parent is internal
            var elementVisibility = container is CSharpTypeWithMembers typeWithMembers ? typeWithMembers.Visibility : CSharpVisibility.None;
            if (elementVisibility == CSharpVisibility.Internal)
            {
                elementVisibility = CSharpVisibility.Public;
            }
            else if (elementVisibility == CSharpVisibility.None)
            {
                elementVisibility = Options.GenerateAsInternal ? CSharpVisibility.Internal : CSharpVisibility.Public;
            }
            element.Visibility = elementVisibility;
        }

        private void ProcessConverting(CppElement cppElement, CSharpElement context)
        {
            for (var i = _pipeline.Converting.Count - 1; i >= 0; i--)
            {
                var converting = _pipeline.Converting[i];
                converting(this, cppElement, context);
            }
        }

        private void ProcessConverted(CSharpElement element, CSharpElement context)
        {
            for (var i = _pipeline.Converted.Count - 1; i >= 0; i--)
            {
                var converted = _pipeline.Converted[i];
                converted(this, element, context);
            }
        }

        private CSharpCompilation ConvertCompilation(CppCompilation item, CSharpElement context)
        {
            for (var i = _pipeline.CompilationConverters.Count - 1; i >= 0; i--)
            {
                var tryProcessCompilation = _pipeline.CompilationConverters[i];
                var element = tryProcessCompilation(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return new CSharpCompilation();
        }
        
        private CSharpElement TryConvertEnum(CppEnum item, CSharpElement context)
        {
            for (var i = _pipeline.EnumConverters.Count - 1; i >= 0; i--)
            {
                var tryEnum = _pipeline.EnumConverters[i];
                var element = tryEnum(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertEnumItem(CppEnumItem item, CSharpElement context)
        {
            for (var i = _pipeline.EnumItemConverters.Count - 1; i >= 0; i--)
            {
                var tryEnumItem = _pipeline.EnumItemConverters[i];
                var element = tryEnumItem(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertClass(CppClass item, CSharpElement context)
        {
            for (var i = _pipeline.ClassConverters.Count - 1; i >= 0; i--)
            {
                var tryProcessClass = _pipeline.ClassConverters[i];
                var element = tryProcessClass(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertField(CppField item, CSharpElement context)
        {
            for (var i = _pipeline.FieldConverters.Count - 1; i >= 0; i--)
            {
                var tryField = _pipeline.FieldConverters[i];
                var element = tryField(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertFunction(CppFunction item, CSharpElement context)
        {
            for (var i = _pipeline.FunctionConverters.Count - 1; i >= 0; i--)
            {
                var tryProcessFunction = _pipeline.FunctionConverters[i];
                var element = tryProcessFunction(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertFunctionType(CppFunctionType item, CSharpElement context)
        {
            for (var i = _pipeline.FunctionTypeConverters.Count - 1; i >= 0; i--)
            {
                var tryProcessFunctionType = _pipeline.FunctionTypeConverters[i];
                var element = tryProcessFunctionType(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertParameter(CppParameter item, int index, CSharpElement context)
        {
            for (var i = _pipeline.ParameterConverters.Count - 1; i >= 0; i--)
            {
                var tryProcessParameter = _pipeline.ParameterConverters[i];
                var element = tryProcessParameter(this, item, index, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

        private CSharpElement TryConvertTypedef(CppTypedef item, CSharpElement context)
        {
            for (var i = _pipeline.TypedefConverters.Count - 1; i >= 0; i--)
            {
                var tryProcessTypedef = _pipeline.TypedefConverters[i];
                var element = tryProcessTypedef(this, item, context);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }
        
        public void AddUsing(ICSharpContainer container, string referenceName, string aliasName = null, bool isStatic = false)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (referenceName == null) throw new ArgumentNullException(nameof(referenceName));

            var usingCompatibleContainer = container;

            while (usingCompatibleContainer != null)
            {
                if (usingCompatibleContainer is CSharpNamespace || usingCompatibleContainer is CSharpGeneratedFile)
                {
                    break;
                }
                usingCompatibleContainer = usingCompatibleContainer.Parent;
            }

            if (usingCompatibleContainer == null)
            {
                throw new InvalidOperationException($"Unable to add `using {referenceName}. No enclosing {nameof(CSharpNamespace)} or {nameof(CSharpGeneratedFile)} found.");
            }

            var members = usingCompatibleContainer.Members;
            int insertIndex = 0;
            for (int i = 0; i < members.Count; i++, insertIndex++)
            {
                var member = members[i];
                if (member is CSharpUsingDeclaration csUsingDecl)
                {
                    if (csUsingDecl.Reference == referenceName && csUsingDecl.Alias == aliasName && csUsingDecl.IsStatic == isStatic)
                    {
                        return;
                    }
                }
                else if (member is CSharpNamespace || member is CSharpType)
                {
                    break;
                }
            }

            members.Insert(insertIndex, new CSharpUsingDeclaration(referenceName) { Alias = aliasName, IsStatic =  isStatic });
        }

        private void Register(CppElement cppElement, CSharpElement element)
        {
            if (cppElement == null) throw new ArgumentNullException(nameof(cppElement));
            if (element == null) throw new ArgumentNullException(nameof(element));

            // Verify that a type map to a type
            if (cppElement is CppType && !(element is CSharpType))
            {
                throw new InvalidOperationException($"The {nameof(CppType)} element `{cppElement}` is converted to an element of type `{element.GetType()}` while it should inherit from `{nameof(CSharpType)}`");
            }

            CSharpElement csElement;
            if (_mapCppToCSharp.TryGetValue(cppElement, out csElement))
            {
                throw new InvalidOperationException($"The element `{cppElement}` is already registered to `{csElement}`");
            }
            element.CppElement = cppElement;
            _mapCppToCSharp[cppElement] = element;
        }

        public CSharpElement FindCSharpElement(CppElement cppElement)
        {
            if (cppElement == null) throw new ArgumentNullException(nameof(cppElement));
            _mapCppToCSharp.TryGetValue(cppElement, out var csElement);
            return csElement;
        }

        public CSharpType FindCSharpType(CppType cppType)
        {
            if (cppType == null) throw new ArgumentNullException(nameof(cppType));
            return (CSharpType) FindCSharpElement(cppType);
        }

        private ICSharpContainer GetCSharpClosestContainer(CSharpElement context)
        {
            // Default implementation, returns the current context
            var nextContext = context;
            while (nextContext != null)
            {
                if (nextContext is ICSharpContainer container) return container;
                nextContext = context.Parent;
            }
            return _csCompilation;
        }
        
        public ICSharpContainer GetCSharpContainer(CppElement element, CSharpElement context)
        {
            for (var i = _pipeline.GetCSharpContainerResolvers.Count - 1; i >= 0; i--)
            {
                var getCSharpContainer = _pipeline.GetCSharpContainerResolvers[i];
                var container = getCSharpContainer(this, element, context);
                if (container != null)
                {
                    return container;
                }
            }

            throw new InvalidOperationException($"Unable to find or create a CSharp container for the element `{element}`");
        }

        public string ConvertExpression(CppExpression expression, CSharpElement context, CSharpType expressionType)
        {
            return expression.ToString();
        }
    }

    internal sealed class CppElementReferenceEqualityComparer : IEqualityComparer<CppElement>
    {
        public static readonly CppElementReferenceEqualityComparer Default = new CppElementReferenceEqualityComparer();

        public bool Equals(CppElement x, CppElement y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(CppElement obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}