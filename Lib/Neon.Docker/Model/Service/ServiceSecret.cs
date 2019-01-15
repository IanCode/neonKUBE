﻿//-----------------------------------------------------------------------------
// FILE:	    ServiceSecret.cs
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
    /// Service secret.
    /// </summary>
    public class ServiceSecret : INormalizable
    {
        /// <summary>
        /// The Docker secret ID.
        /// </summary>
        [JsonProperty(PropertyName = "SecretID", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "SecretID", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string SecretID { get; set; }

        /// <summary>
        /// The secret name.
        /// </summary>
        [JsonProperty(PropertyName = "SecretName", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "SecretName", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string SecretName { get; set; }

        /// <summary>
        /// Secret file information.
        /// </summary>
        [JsonProperty(PropertyName = "File", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "File", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ServiceFile File { get; set; }

        /// <inheritdoc/>
        public void Normalize()
        {
            File = File ?? new ServiceFile();

            File?.Normalize();
        }
    }
}
