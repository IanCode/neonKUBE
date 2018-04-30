﻿//-----------------------------------------------------------------------------
// FILE:	    LoadBalancerHttpBackend.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Common;
using Neon.Net;

namespace Neon.Cluster
{
    /// <summary>
    /// Describes an HTTP/HTTPS load balancer backend.
    /// </summary>
    public class LoadBalancerHttpBackend : LoadBalancerBackend
    {
        /// <summary>
        /// Forward the request to this backend using TLS (defaults to <c>false</c>).
        /// </summary>
        [JsonProperty(PropertyName = "Tls", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(false)]
        public bool Tls { get; set; } = false;

        /// <summary>
        /// Validates the backend.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <param name="rule">The parent rule.</param>
        public void Validate(LoadBalancerValidationContext context, LoadBalancerHttpRule rule)
        {
            base.Validate(context, rule.Name);
        }
    }
}