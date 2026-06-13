// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using CppAst.CodeGen.CSharp;

namespace CppAst.CodeGen.Tests
{
    public class ConverterTests
    {
        [Test]
        public void ConvertsAnonymousStructUnionAndFunctionPointer()
        {
            var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
typedef struct { int x; } AnotherStruct;

struct {
    AnotherStruct v;
    AnotherStruct* pv;
    int a;
    int b;
    void (*ptr)(int arg0, int arg1, void (*arg2)(int arg3));

    union
    {
        int c;
        int d;
    } e;
} outer;
            ");

            GeneratedCodeTestHelper.AssertContainsAll(text,
                "public partial struct AnotherStruct",
                "public int x;",
                "public libnative.AnotherStruct v;",
                "public libnative.AnotherStruct* pv;",
                "public delegate*unmanaged[Cdecl]<int, int, delegate*unmanaged[Cdecl]<int, void>, void> ptr;",
                "[global::System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]",
                "[FieldOffset(0)]\n                public int c;",
                "[FieldOffset(0)]\n                public int d;",
                "__union_0 e;");
        }

        [Test]
        public void MapsMacrosToConstantsAndEnumItems()
        {
            var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
#ifdef WIN32
#define EXPORT_API __declspec(dllexport)
#else
#define EXPORT_API __attribute__((visibility(""default"")))
#endif
#define MYNAME_X 1
#define MYNAME_Y 2
#define MYNAME_XYWZ MYNAME_X
#define MYNAME_XY (MYNAME_X|MYNAME_Y)

EXPORT_API void function0(int x);
            ", options =>
            {
                options.MappingRules.Add(e => e.MapMacroToConst("MYNAME_(.*)", "int"));
                options.MappingRules.Add(e => e.MapMacroToEnum("MYNAME_(.*)", "MYNAME_ENUM", @"MYNAME_ENUM_$1"));
                options.MappingRules.Add(e => e.Map<CppParameter>("function0::x").Type("char*"));
                options.MappingRules.Add(e => e.Map<CppFunction>("function0").Visibility(CSharpVisibility.Private));
            });

            GeneratedCodeTestHelper.AssertContainsAll(text,
                "public enum MYNAME_ENUM : int",
                "MYNAME_ENUM_X = unchecked((int)1)",
                "MYNAME_ENUM_Y = unchecked((int)2)",
                "MYNAME_ENUM_XYWZ = unchecked((int)1)",
                "MYNAME_ENUM_XY = unchecked((int)3)",
                "public const int MYNAME_X = 1;",
                "public const int MYNAME_Y = 2;",
                "public const int MYNAME_XYWZ = 1;",
                "public const int MYNAME_XY = 3;",
                "private static partial void function0(byte* x);");
        }

        [Test]
        public void EnumWithTypedefBaseUsesCanonicalBaseType()
        {
            var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
typedef unsigned int EnumBase;

enum class MyEnum : EnumBase
{
    MyEnum_Value = 1,
};
            ");

            GeneratedCodeTestHelper.AssertContainsAll(text,
                "public readonly partial record struct EnumBase(uint Value)",
                "public enum MyEnum : uint",
                "MyEnum_Value = unchecked((uint)1)");
            GeneratedCodeTestHelper.AssertDoesNotContainAny(text, "public enum MyEnum : EnumBase");
        }

        [Test]
        public void MappingRulesCollectionSeparatesStandardRules()
        {
            var rules = new CppMappingRules
            {
                e => e.Map(@"name([a-z]+)::a(\d+)b").Private(),
            };

            Assert.AreEqual(1, rules.StandardRules.Count);
            Assert.AreEqual(0, rules.MacroRules.Count);
            Assert.AreSame(rules.StandardRules[0], ((System.Collections.Generic.IEnumerable<CppElementMappingRuleBase>)rules).Single());
        }

        [Test]
        public void ConvertsEnumsStructsTypedefsCommentsAndFunctionExports()
        {
            var text = GeneratedCodeTestHelper.GenerateSingleFile(@"
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

            // This is a comment
            struct Tata
            {
                int a;

                int b;
                int c;
                char items[4];
                int item2[8];

                const char* d;
            };

            struct git_my_repo;

            typedef int git_my_yoyo;

            typedef const char* git_my_string;

            // This is a comment.
            // This is another comment
            // @param myrepo yoyo
            // @return This is a big list of things to return
            EXPORT_API bool function0(git_my_repo* myrepo, int a, float b, const char* text, const char text2[], bool arg4[], git_my_yoyo arg5, git_my_string arg6);
            ", options =>
            {
                options.GenerateAsInternal = true;
                options.TypedefCodeGenKind = CppTypedefCodeGenKind.Wrap;
                options.TypedefWrapForceList.Add("git_my_string");
            });

            GeneratedCodeTestHelper.AssertContainsAll(text,
                "internal static unsafe partial class libnative",
                "[Flags]\n        public enum Toto : int",
                "TOTO_FLAG = unchecked((int)1)",
                "/// <summary>\n        /// This is a comment\n        /// </summary>",
                "public unsafe partial struct Tata",
                "public fixed byte items[4];",
                "public fixed int item2[8];",
                "public byte* d;",
                "public readonly partial record struct git_my_repo(nint Handle)",
                "public readonly partial record struct git_my_yoyo(int Value)",
                "public readonly partial struct git_my_string : IEquatable<git_my_string>",
                "/// <param name=\"myrepo\">yoyo</param>",
                "/// <returns>This is a big list of things to return</returns>",
                "[global::System.Runtime.InteropServices.LibraryImport(\"libnative\", EntryPoint = \"function0\")]",
                "[return:global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.U1)]",
                "public static partial bool function0(libnative.git_my_repo myrepo, int a, float b, [global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.LPUTF8Str)] string text, byte* text2, bool* arg4, libnative.git_my_yoyo arg5, libnative.git_my_string arg6);");
        }
    }
}
