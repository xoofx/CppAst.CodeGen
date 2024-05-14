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
                new DefaultInterfaceConverter(),
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
            TypedefWrapForceList = new HashSet<string>();
            Tags = new Dictionary<string, object?>();
            DefaultCharSet = CharSet.Ansi;
            AllowFixedSizeBuffers = true;
            DefaultMarshalForString = new CSharpMarshalAsAttribute(UnmanagedType.LPUTF8Str);
            DefaultMarshalForBool = new CSharpMarshalAsAttribute(UnmanagedType.U1);
            UseLibraryImport = true;
            DisableRuntimeMarshalling = false;
            CharAsByte = true;
            DetectOpaquePointers = true;
            AllowMarshalForString = true;
            ManagedToUnmanagedStringTypeForParameter = null;
            EnableAutoByRef = true;
            AutoConvertStandardCTypes = true;
            MapCLongToIntPtr = false;
        }

        public string DefaultNamespace { get; set; }

        public UPath DefaultOutputFilePath { get; set; }

        public string DefaultClassLib { get; set; }

        public bool GenerateAsInternal { get; set; }

        public string DefaultDllImportNameAndArguments { get; set; }

        public bool AllowFixedSizeBuffers { get; set; }

        public bool DetectOpaquePointers { get; set; }

        public bool CharAsByte { get; set; }

        public CharSet DefaultCharSet { get; set; }

        public bool DispatchOutputPerInclude { get; set; }

        public bool AllowMarshalForString { get; set; }

        public CSharpAttribute? DefaultMarshalForString { get; set; }

        public CSharpAttribute? DefaultMarshalForBool { get; set; }

        public bool GenerateEnumItemAsFields { get; set; }

        public CppTypedefCodeGenKind TypedefCodeGenKind { get; set; }

        public bool UseLibraryImport { get; set; }

        public bool DisableRuntimeMarshalling { get; set; }

        public HashSet<string> TypedefWrapForceList { get; }

        public Dictionary<string, object?> Tags { get; private set; }

        public CppMappingRules MappingRules { get; private set; }

        public List<ICSharpConverterPlugin> Plugins { get; private set; }

        public string? ManagedToUnmanagedStringTypeForParameter { get; set; }

        public bool AutoConvertStandardCTypes { get; set; }

        public bool EnableAutoByRef { get; set; }

        public string FixedArrayPrefix { get; set; } = "FixedArray";

        public bool MapCLongToIntPtr { get; set; }

        public object? this[string tagName]
        {
            get
            {
                Tags.TryGetValue(tagName, out var obj);
                return obj;
            }
            set => Tags[tagName] = value;
        }

        public override CppParserOptions Clone()
        {
            var csConverterOptions = (CSharpConverterOptions)base.Clone();

            csConverterOptions.MappingRules = new CppMappingRules();
            csConverterOptions.MappingRules.MacroRules.AddRange(MappingRules.MacroRules);
            csConverterOptions.MappingRules.StandardRules.AddRange(MappingRules.StandardRules);

            csConverterOptions.Plugins = new List<ICSharpConverterPlugin>(Plugins);

            // TODO: value behind tags are not cloned
            csConverterOptions.Tags = new Dictionary<string, object?>(Tags);

            csConverterOptions.DefaultMarshalForString = (CSharpAttribute?)DefaultMarshalForString?.Clone();
            csConverterOptions.DefaultMarshalForBool = (CSharpAttribute?)DefaultMarshalForBool?.Clone();

            return csConverterOptions;
        }
    }
}