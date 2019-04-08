﻿//-----------------------------------------------------------------------------
// FILE:	    RoundTripJsonInputFormatter.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

using Neon.Common;
using Neon.Data;
using Newtonsoft.Json.Linq;

namespace Neon.Web
{
    /// <summary>
    /// <para>
    /// Handles deserialization of JSON objects for noSQL scenarios that supports round 
    /// trips without any property loss, even if one side of the transaction is out 
    /// of data and is not aware of all of the possible JSON properties.
    /// </para>
    /// <para>
    /// This class is designed to support classes generated by the <b>Neon.CodeGen</b>
    /// assembly that implement <see cref=" IGeneratedType"/>.
    /// </para>
    /// </summary>
    public sealed class RoundTripJsonInputFormatter : TextInputFormatter
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RoundTripJsonInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedEncodings.Add(Encoding.UTF8);
        }

        /// <inheritdoc/>
        protected override bool CanReadType(Type type)
        {
            if (WebHelper.IsRoundTripType(type))
            {
                return true;
            }

            if (type.Implements<IGeneratedType>())
            {
                WebHelper.RegisterRoundTripType(type);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var request = context.HttpContext.Request;

            if (request.Body == null)
            {
                return await InputFormatterResult.SuccessAsync(null);
            }
            else
            {
                var result = await GeneratedTypeFactory.TryCreateFromAsync(context.ModelType, request.Body, Encoding.UTF8);

                if (result.Item1)
                {
                    return await InputFormatterResult.SuccessAsync(result.Item2);
                }
                else
                {
                    return await InputFormatterResult.SuccessAsync(NeonHelper.JsonDeserialize(context.ModelType, Encoding.UTF8.GetString(await request.Body.ReadToEndAsync())));
                }
            }
        }
    }
}
