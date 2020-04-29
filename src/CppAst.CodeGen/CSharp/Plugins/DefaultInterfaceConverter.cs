// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;

namespace CppAst.CodeGen.CSharp
{
    [StructLayout(LayoutKind.Explicit)]
    public class DefaultInterfaceConverter : ICSharpConverterPlugin
    {
        /// <inheritdoc />
        public void Register(CSharpConverter converter, CSharpConverterPipeline pipeline)
        {
            pipeline.ClassConverters.Add(ConvertClass);
        }

        public static CSharpElement ConvertClass(CSharpConverter converter, CppClass cppClass, CSharpElement context)
        {
            if (cppClass.ClassKind != CppClassKind.Class)
            {
                return null;
            }
            if (cppClass.Functions.All(x => (x.Flags & CppFunctionFlags.Virtual) == 0))
            {
                return null;
            }

            // Register the struct as soon as possible
            var csInterfaceName = converter.GetCSharpName(cppClass, context);
            var csInterface = new CSharpInterface(csInterfaceName)
            {
                CppElement = cppClass,
            };

            var container = converter.GetCSharpContainer(cppClass, context);
            converter.ApplyDefaultVisibility(csInterface, container);
            container.Members.Add(csInterface);

            if (cppClass.BaseTypes.Count > 0)
            {
                var csType = converter.GetCSharpType(cppClass.BaseTypes[0].Type, context);
                csInterface.BaseTypes.Add(csType);
            }

            csInterface.Comment = converter.GetCSharpComment(cppClass, csInterface);

            return csInterface;
        }
    }
}