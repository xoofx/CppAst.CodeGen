// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public delegate void GlobalProcessingDelegate(CSharpConverter converter);

    public delegate ICSharpContainer GetCSharpContainerDelegate(CSharpConverter converter, CppElement element, CSharpElement context);

    public delegate string GetCSharpNameDelegate(CSharpConverter converter, CppElement element, CSharpElement context);

    public delegate CSharpType GetCSharpTypeDelegate(CSharpConverter converter, CppType cppType, CSharpElement context);

    public delegate CSharpCompilation ConvertCompilationDelegate(CSharpConverter converter, CppCompilation cppCompilation, CSharpElement context);

    public delegate void AfterPreprocessingDelegate(CSharpConverter converter, CppCompilation cppCompilation, StringBuilder additionalHeaders);

    public delegate CSharpElement ConvertEnumDelegate(CSharpConverter converter, CppEnum cppEnum, CSharpElement context);

    public delegate CSharpElement ConvertEnumItemDelegate(CSharpConverter converter, CppEnumItem cppEnumItem, CSharpElement context);

    public delegate CSharpElement ConvertClassDelegate(CSharpConverter converter, CppClass cppClass, CSharpElement context);

    public delegate CSharpElement ConvertFieldDelegate(CSharpConverter converter, CppField cppField, CSharpElement context);

    public delegate CSharpElement ConvertFunctionDelegate(CSharpConverter converter, CppFunction cppFunction, CSharpElement context);

    public delegate CSharpElement ConvertFunctionTypeDelegate(CSharpConverter converter, CppFunctionType cppFunctionType, CSharpElement context);

    public delegate CSharpElement ConvertParameterDelegate(CSharpConverter converter, CppParameter cppParameter, int index, CSharpElement context);

    public delegate CSharpElement ConvertTypedefDelegate(CSharpConverter converter, CppTypedef cppTypedef, CSharpElement context);

    public delegate CSharpElement ConvertNamespaceDelegate(CSharpConverter converter, CppNamespace cppNamespace, CSharpElement context);

    public delegate void ProcessBeforeConvertDelegate(CSharpConverter converter, CppElement element, CSharpElement context);

    public delegate void ProcessAfterConvertDelegate(CSharpConverter converter, CSharpElement element, CSharpElement context);
   
    public sealed class CSharpConverterPipeline
    {
        public CSharpConverterPipeline(CSharpConverterOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            ConvertBegin = new List<GlobalProcessingDelegate>();
            GetCSharpNameResolvers = new List<GetCSharpNameDelegate>();
            GetCSharpTypeResolvers = new List<GetCSharpTypeDelegate>();
            GetCSharpContainerResolvers = new List<GetCSharpContainerDelegate>();
            AfterPreprocessing = new List<AfterPreprocessingDelegate>();
            CompilationConverters = new List<ConvertCompilationDelegate>();
            EnumConverters = new List<ConvertEnumDelegate>();
            EnumItemConverters = new List<ConvertEnumItemDelegate>();
            ClassConverters = new List<ConvertClassDelegate>();
            FieldConverters = new List<ConvertFieldDelegate>();
            FunctionConverters = new List<ConvertFunctionDelegate>();
            FunctionTypeConverters = new List<ConvertFunctionTypeDelegate>();
            ParameterConverters = new List<ConvertParameterDelegate>();
            TypedefConverters = new List<ConvertTypedefDelegate>();
            Converting = new List<ProcessBeforeConvertDelegate>();
            Converted = new List<ProcessAfterConvertDelegate>();
            ConvertEnd = new List<GlobalProcessingDelegate>();
            RegisteredPlugins = new List<ICSharpConverterPlugin>();
        }

        public CSharpConverterOptions Options { get; }

        public List<AfterPreprocessingDelegate> AfterPreprocessing { get; }

        public List<GlobalProcessingDelegate> ConvertBegin { get; }

        public List<GetCSharpNameDelegate> GetCSharpNameResolvers { get; }

        public List<GetCSharpTypeDelegate> GetCSharpTypeResolvers { get; }

        public List<GetCSharpContainerDelegate> GetCSharpContainerResolvers { get; }

        public List<ConvertCompilationDelegate> CompilationConverters { get; }

        public List<ConvertEnumDelegate> EnumConverters { get; }

        public List<ConvertEnumItemDelegate> EnumItemConverters { get; }

        public List<ConvertClassDelegate> ClassConverters { get; }

        public List<ConvertFieldDelegate> FieldConverters { get; }

        public List<ConvertFunctionDelegate> FunctionConverters { get; }

        public List<ConvertFunctionTypeDelegate> FunctionTypeConverters { get; }

        public List<ConvertParameterDelegate> ParameterConverters { get; }

        public List<ConvertTypedefDelegate> TypedefConverters { get; }

        public List<ProcessBeforeConvertDelegate> Converting { get; }

        public List<ProcessAfterConvertDelegate> Converted { get; }

        public List<GlobalProcessingDelegate> ConvertEnd { get; }

        public List<ICSharpConverterPlugin> RegisteredPlugins { get; }
        
        public void RegisterPlugins(CSharpConverter converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            ConvertBegin.Clear();
            AfterPreprocessing.Clear();
            GetCSharpNameResolvers.Clear();
            GetCSharpTypeResolvers.Clear();
            GetCSharpContainerResolvers.Clear();
            CompilationConverters.Clear();
            EnumConverters.Clear();
            EnumItemConverters.Clear();
            ClassConverters.Clear();
            FieldConverters.Clear();
            FunctionConverters.Clear();
            FunctionTypeConverters.Clear();
            ParameterConverters.Clear();
            TypedefConverters.Clear();
            Converting.Clear();
            Converted.Clear();
            ConvertEnd.Clear();
            RegisteredPlugins.Clear();

            for (var index = Options.Plugins.Count - 1; index >= 0; index--)
            {
                var plugin = Options.Plugins[index];
                if (RegisteredPlugins.Contains(plugin))
                {
                    continue;
                }

                RegisteredPlugins.Add(plugin);
                plugin.Register(converter, this);
            }
        }
    }
}