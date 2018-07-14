﻿//-----------------------------------------------------------------------------
// FILE:	    DashboardOptions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Neon.Common;

namespace Neon.Hive
{
    /// <summary>
    /// Controls which built-in dashboards are to be enabled for the hive.
    /// </summary>
    public class DashboardOptions
    {
        private const bool defaultKibana = true;
        private const bool defaultConsul = true;
        private const bool defaultCeph   = true;

        /// <summary>
        /// Enables the Elastic Kibana dashboard if logging is enabled for the hive.
        /// This defaults to <c>true</c>.
        /// </summary>
        [JsonProperty(PropertyName = "Kibana", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultKibana)]
        public bool Kibana { get; set; } = defaultKibana;

        /// <summary>
        /// Enables the Consul dashboard.  This defaults to <c>true</c>.
        /// </summary>
        [JsonProperty(PropertyName = "Consul", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultConsul)]
        public bool Consul { get; set; } = defaultConsul;

        /// <summary>
        /// Enables the Ceph dashboard.  This defaults to <c>true</c>.
        /// </summary>
        [JsonProperty(PropertyName = "Ceph", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(defaultCeph)]
        public bool Ceph { get; set; } = defaultCeph;

        /// <summary>
        /// Validates the options and also ensures that all <c>null</c> properties are
        /// initialized to their default values.
        /// </summary>
        /// <param name="hiveDefinition">The hive definition.</param>
        /// <exception cref="HiveDefinitionException">Thrown if the definition is not valid.</exception>
        [Pure]
        public void Validate(HiveDefinition hiveDefinition)
        {
            Covenant.Requires<ArgumentNullException>(hiveDefinition != null);
        }
    }
}