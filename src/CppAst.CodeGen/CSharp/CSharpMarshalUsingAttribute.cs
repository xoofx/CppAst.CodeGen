// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpMarshalUsingAttribute : CSharpAttribute
    {
        public CSharpMarshalUsingAttribute(string nativeType)
        {
            NativeType = nativeType;
        }

        /// <summary>Gets the marshaller type used to convert the attributed type from managed to native code.</summary>
        public string? NativeType { get; }

        /// <summary>Gets or sets the name of the parameter that will provide the size of the collection when marshalling from unmanaged to managed, or <see cref="F:System.Runtime.InteropServices.Marshalling.MarshalUsingAttribute.ReturnsCountValue" /> if the return value provides the size.</summary>
        public string? CountElementName { get; set; }

        /// <summary>If a collection is constant size, gets or sets the size of the collection when marshalling from unmanaged to managed.</summary>
        public int? ConstantElementCount { get; set; }

        /// <summary>Gets or sets the indirection depth this marshalling info is provided for.</summary>
        public int? ElementIndirectionDepth { get; set; }


        /// <inheritdoc />
        public override string ToText()
        {
            var builder = new StringBuilder();
            builder.Append("global::System.Runtime.InteropServices.Marshalling.MarshalUsing(");
            builder.Append(NativeType);
            if (CountElementName != null)
            {
                builder.Append($", CountElementName = {CountElementName}");
            }

            if (ConstantElementCount.HasValue)
            {
                builder.Append(", ConstantElementCount = ");
                builder.Append(ConstantElementCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (ElementIndirectionDepth.HasValue)
            {
                builder.Append(", ElementIndirectionDepth = ");
                builder.Append(ElementIndirectionDepth.Value.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(")");
            return builder.ToString();
        }
    }
}