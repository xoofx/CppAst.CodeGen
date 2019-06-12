﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using CppAst.CodeGen.Common;
using CppAst.CodeGen.CSharp;
using NUnit.Framework;
using Zio;
using Zio.FileSystems;

namespace CppAst.CodeGen.Tests
{
    /// <summary>
    /// TEMP
    /// </summary>
    public class ConverterTests
    {
        [Test]
        public void TestMacroToConst()
        {
            var options = new CSharpConverterOptions()
            {
                MappingRules =
                {
                    e => e.MapMacroToConst("MYNAME_(.*)", "int", @"YOYO_$1"),
                    e => e.MapMacroToEnum("MYNAME_(.*)", "MYNAME_ENUM", @"MYNAME_ENUM_$1"),
                    e => e.Map<CppParameter>("function0::x").Type("bool"),
                }
            };

            var csCompilation = CSharpConverter.Convert(@"
#ifdef WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API __attribute__((visibility(""default"")))
#endif
#define MYNAME_X 1
#define MYNAME_Y 2
#define MYNAME_XYWZ 3

EXPORT_API void function0(int x);
            ", options);

            Assert.False(csCompilation.HasErrors);
            
            var fs = new MemoryFileSystem();
            var codeWriter = new CodeWriter(new CodeWriterOptions(fs));
            csCompilation.DumpTo(codeWriter);

            var text = fs.ReadAllText(options.DefaultOutputFilePath);
            Console.WriteLine(text);
        }

        [Test]
        public void TestMappingRules()
        {
            var rules = new CppMappingRules()
            {
                e => e.Map(@"name([a-z]+)::a(\d+)b").Private(),
            };
        }


        [Test]
        public void CheckFunction()
        {
            var options = new CSharpConverterOptions()
            {
                GenerateAsInternal = true,
                TypedefCodeGenKind =  CppTypedefCodeGenKind.Wrap,
                TypedefWrapWhiteList =
                {
                    "git_my_string"
                }
            };

            var csCompilation = CSharpConverter.Convert(@"
            #ifdef WIN32
            #define EXPORT_API __declspec(dllexport)
            #else
            #define EXPORT_API __attribute__((visibility(""default"")))
            #endif

            enum Toto
            {
                TOTO = 0,
                TOTO_FLAG = 1 << 0,
            };

            struct Tata
            {
                int a;

                int b;
                int c;

                const char* d;
            };

            struct git_my_repo;

            typedef int git_my_yoyo;

            typedef const char* git_my_string;

            EXPORT_API bool function0(git_my_repo* myrepo, int a, float b, const char* text, const char text2[], bool arg4[], git_my_yoyo arg5, git_my_string arg6);
            ", options);

            Assert.False(csCompilation.HasErrors);

            var fs = new MemoryFileSystem();
            var codeWriter = new CodeWriter(new CodeWriterOptions(fs));
            csCompilation.DumpTo(codeWriter);

            var text = fs.ReadAllText(options.DefaultOutputFilePath);
            Console.WriteLine(text);
        }
    }

}