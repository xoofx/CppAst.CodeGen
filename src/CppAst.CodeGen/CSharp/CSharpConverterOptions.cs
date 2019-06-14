// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Zio;

namespace CppAst.CodeGen.CSharp
{
    public enum CppTypedefCodeGenKind
    {
        Wrap,

        NoWrap,
    }
    
    public class CSharpConverterOptions : CppParserOptions
    {
        public CSharpConverterOptions()
        {
            Plugins = new List<ICSharpConverterPlugin>()
            {
                new DefaultCommentConverter(),
                new DefaultGetCSharpNamePlugin(),
                new DefaultContainerResolver(),
                new DefaultTypedefConverter(),
                new DefaultEnumConverter(),
                new DefaultEnumItemConverter(),
                new DefaultFunctionConverter(),
                new DefaultParameterConverter(),
                new DefaultStructConverter(),
                new DefaultFieldConverter(),
                new DefaultFunctionTypeConverter(),
                new DefaultTypeConverter(),
                new DefaultDllImportConverter(),
                new DefaultMappingRulesConverter(),
            };

            MappingRules = new CppMappingRules();
            DefaultNamespace = "LibNative";
            DefaultOutputFilePath = "/LibNative.generated.cs";
            DefaultClassLib = "libnative";
            DefaultDllImportNameAndArguments = "\"libnative\"";
            GenerateAsInternal = false;
            GenerateEnumItemAsFields = true;
            TypedefCodeGenKind = CppTypedefCodeGenKind.Wrap;
            TypedefWrapWhiteList = new HashSet<string>();
            Tags = new Dictionary<string, object>();
            DefaultCharSet = CharSet.Ansi;
            AllowFixedSizeBuffers = true;
            DefaultMarshalForString = new CSharpMarshalAttribute(CSharpUnmanagedKind.LPStr);
            DefaultMarshalForBool = new CSharpMarshalAttribute(CSharpUnmanagedKind.U1);
        }

        public string DefaultNamespace { get; set; }

        public UPath DefaultOutputFilePath { get; set; }

        public string DefaultClassLib { get; set; }
        
        public bool GenerateAsInternal { get; set; }

        public string DefaultDllImportNameAndArguments { get; set; }

        public bool AllowFixedSizeBuffers { get; set; }

        public CharSet DefaultCharSet { get; set; }

        public bool DispatchOutputPerInclude { get; set; }

        public CSharpMarshalAttribute DefaultMarshalForString { get; set; }

        public CSharpMarshalAttribute DefaultMarshalForBool { get; set; }

        public bool GenerateEnumItemAsFields { get; set; }

        public CppTypedefCodeGenKind TypedefCodeGenKind { get; set; }
        
        public HashSet<string> TypedefWrapWhiteList { get; }

        public Dictionary<string, object> Tags { get; private set; }

        public CppMappingRules MappingRules { get; private set; }
        
        public List<ICSharpConverterPlugin> Plugins { get; private set; }

        public object this[string tagName]
        {
            get
            {
                Tags.TryGetValue(tagName, out var obj);
                return obj;
            }
            set
            {
                Tags[tagName] = value;
            }
        }

        public override CppParserOptions Clone()
        {
            var csConverterOptions = (CSharpConverterOptions)base.Clone();

            csConverterOptions.MappingRules = new CppMappingRules();
            csConverterOptions.MappingRules.MacroRules.AddRange(MappingRules.MacroRules);
            csConverterOptions.MappingRules.StandardRules.AddRange(MappingRules.StandardRules);

            csConverterOptions.Plugins = new List<ICSharpConverterPlugin>(Plugins);

            // TODO: value behind tags are not cloned
            csConverterOptions.Tags = new Dictionary<string, object>(Tags);

            csConverterOptions.DefaultMarshalForString = (CSharpMarshalAttribute)DefaultMarshalForString?.Clone();
            csConverterOptions.DefaultMarshalForBool = (CSharpMarshalAttribute)DefaultMarshalForBool?.Clone();

            return csConverterOptions;
        }
    }
}