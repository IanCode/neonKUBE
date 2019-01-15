﻿//-----------------------------------------------------------------------------
// FILE:	    ServiceFile.cs
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
    /// Configuration information for a Docker secret or config that
    /// is mapped into a service container.
    /// </summary>
    public class ServiceFile : INormalizable
    {
        /// <summary>
        /// Path to the target file within the container.
        /// </summary>
        [JsonProperty(PropertyName = "Name", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Name", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Name { get; set; }

        /// <summary>
        /// Identifies the user that owns the file.
        /// </summary>
        [JsonProperty(PropertyName = "UID", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "UID", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string UID { get; set; }

        /// <summary>
        /// Identifies the group that owns the file.
        /// </summary>
        [JsonProperty(PropertyName = "GID", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "GID", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string GID { get; set; }

        /// <summary>
        /// <para>
        /// The Linux file mode for the file.
        /// </para>
        /// <note>
        /// This value is encoded as decimal.  You'll need to convert to octal to
        /// see what it looks like as standard Linux permissions.
        /// </note>
        /// </summary>
        [JsonProperty(PropertyName = "Mode", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Mode", ApplyNamingConventions = false)]
        [DefaultValue(1777)]
        public int Mode { get; set; }

        /// <inheritdoc/>
        public void Normalize()
        {
        }
    }
}
