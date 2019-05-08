﻿//-----------------------------------------------------------------------------
// FILE:	    ProxyReply.cs
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
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Neon.Common;

namespace Neon.Cadence.Internal
{
    /// <summary>
    /// Base class for all proxy requests.
    /// </summary>
    [ProxyMessage(MessageTypes.Unspecified)]
    internal class ProxyReply : ProxyMessage
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ProxyReply()
        {
        }

        /// <summary>
        /// Uniquely identifies the request this reply answers.
        /// </summary>
        public long RequestId
        {
            get => GetLongProperty("RequestId");
            set => SetLongProperty("RequestId", value);
        }

        /// <summary>
        /// Optionally indicates the error type.
        /// </summary>
        public CadenceErrorTypes ErrorType
        {
            get
            {
                switch (GetStringProperty("ErrorType"))
                {
                    case null:          return CadenceErrorTypes.None;
                    case "cancelled":   return CadenceErrorTypes.Cancelled;
                    case "custom":      return CadenceErrorTypes.Custom;
                    case "generic":     return CadenceErrorTypes.Generic;
                    case "panic":       return CadenceErrorTypes.Panic;
                    case "terminated":  return CadenceErrorTypes.Terminated;
                    case "timeout":     return CadenceErrorTypes.Timeout;

                    default:

                        throw new NotImplementedException();
                }
            }

            set
            {
                string typeString;

                switch (value)
                {
                    case CadenceErrorTypes.None:        typeString = null;          break;
                    case CadenceErrorTypes.Cancelled:   typeString = "cancelled";   break;
                    case CadenceErrorTypes.Custom:      typeString = "custom";      break;
                    case CadenceErrorTypes.Generic:     typeString = "generic";     break;
                    case CadenceErrorTypes.Panic:       typeString = "panic";       break;
                    case CadenceErrorTypes.Terminated:  typeString = "terminated";  break;
                    case CadenceErrorTypes.Timeout:     typeString = "timeout";     break;

                    default:

                        throw new NotImplementedException();
                }

                SetStringProperty("ErrorType", typeString);
            }
        }

        /// <summary>
        /// Optionally identifies the specific error.
        /// </summary>
        public string Error
        {
            get => GetStringProperty("Error");
            set => SetStringProperty("Error", value);
        }

        /// <summary>
        /// Optionally specifies additional error details.
        /// </summary>
        public string ErrorDetails
        {
            get => GetStringProperty("ErrorDetails");
            set => SetStringProperty("ErrorDetails", value);
        }

        /// <inheritdoc/>
        internal override ProxyMessage Clone()
        {
            var clone = new ProxyReply();

            CopyTo(clone);

            return clone;
        }

        /// <inheritdoc/>
        protected override void CopyTo(ProxyMessage target)
        {
            base.CopyTo(target);

            var typedTarget = (ProxyReply)target;

            typedTarget.RequestId    = this.RequestId;
            typedTarget.ErrorType    = this.ErrorType;
            typedTarget.Error        = this.Error;
            typedTarget.ErrorDetails = this.ErrorDetails;
        }

        /// <summary>
        /// Throws the related exception if the reply is reporting an error.
        /// </summary>
        public void ThrowOnError()
        {
            if (!string.IsNullOrEmpty(Error))
            {
                throw CadenceException.Create(ErrorType, Error, ErrorDetails);
            }
        }
    }
}