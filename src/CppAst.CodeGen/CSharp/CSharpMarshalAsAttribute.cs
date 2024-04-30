// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace CppAst.CodeGen.CSharp
{
    public class CSharpMarshalAsAttribute : CSharpAttribute
    {
        public CSharpMarshalAsAttribute(UnmanagedType unmanagedType)
        {
            UnmanagedType = unmanagedType;
        }

        public UnmanagedType UnmanagedType { get; set; }

        public UnmanagedType? ArraySubType { get; set; }

        public int? SizeParamIndex { get; set; }

        public int? SizeConst { get; set; }

        /// <summary>
        /// Gets or sets a custom marshal type as a string
        /// </summary>
        public string? MarshalType { get; set; }

        /// <summary>
        /// Gets or sets a custom marshal type as a System.Type
        /// </summary>
        public string? MarshalTypeRef { get; set; }

        /// <summary>
        /// Gets or sets the custom Marshal cookie.
        /// </summary>
        public string? MarshalCookie { get; set; }

        /// <inheritdoc />
        public override string ToText()
        {
            var builder = new StringBuilder();
            builder.Append("global::System.Runtime.InteropServices.MarshalAs(UnmanagedType.");
            builder.Append(UnmanagedType.ToString());
            if (ArraySubType.HasValue)
            {
                builder.Append(", ArraySubType = UnmanagedType.");
                builder.Append(ArraySubType.Value.ToString());
            }

            if (SizeParamIndex.HasValue)
            {
                builder.Append(", SizeParamIndex = ");
                builder.Append(SizeParamIndex.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (SizeConst.HasValue)
            {
                builder.Append(", SizeConst = ");
                builder.Append(SizeConst.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (MarshalType != null)
            {
                builder.Append(", MarshalType = ").Append(MarshalType);
            }

            if (MarshalTypeRef != null)
            {
                builder.Append(", MarshalTypeRef = ").Append(MarshalTypeRef);
            }

            if (MarshalCookie != null)
            {
                builder.Append(", MarshalCookie = ").Append(MarshalCookie);
            }
            builder.Append(")");
            return builder.ToString();
        }
    }
}