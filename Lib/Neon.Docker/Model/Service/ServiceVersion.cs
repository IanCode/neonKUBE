﻿//-----------------------------------------------------------------------------
// FILE:	    ServiceVersion.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

namespace Neon.Docker
{
    /// <summary>
    /// <b>Windows-only:</b> Specifies how Windows credentials are to be
    /// loaded for the container.
    /// </summary>
    public class ServiceVersion : INormalizable
    {
        /// <summary>
        /// Update index for the service when the <see cref="ServiceDetails"/> snapshot was taken.
        /// </summary>
        [JsonProperty(PropertyName = "Index", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Index", ApplyNamingConventions = false)]
        [DefaultValue(0)]
        public long Index { get; set; }

        /// <inheritdoc/>
        public void Normalize()
        {
        }
    }
}
