﻿//-----------------------------------------------------------------------------
// FILE:	    ServiceSpec.cs
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
    /// User modifiable service configuration.
    /// </summary>
    public class ServiceSpec : INormalizable
    {
        /// <summary>
        /// The service Name.
        /// </summary>
        [JsonProperty(PropertyName = "Name", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Name", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Name { get; set; }

        /// <summary>
        /// Service labels.
        /// </summary>
        [JsonProperty(PropertyName = "Labels", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Labels", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public Dictionary<string, string> Labels { get; set; }

        /// <summary>
        /// User modifiable service task configuration.
        /// </summary>
        [JsonProperty(PropertyName = "TaskTemplate", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "TaskTemplate", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ServiceTaskTemplate TaskTemplate { get; set; }

        /// <summary>
        /// Service scheduling mode details.
        /// </summary>
        [JsonProperty(PropertyName = "Mode", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Mode", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ServiceSchedulingMode Mode { get; set; }

        /// <summary>
        /// Specifies the service update strategy.
        /// </summary>
        [JsonProperty(PropertyName = "UpdateConfig", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "UpdateConfig", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ServiceUpdateConfig UpdateConfig { get; set; }

        /// <summary>
        /// Specifies the service update strategy.
        /// </summary>
        [JsonProperty(PropertyName = "RollbackConfig", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "RollbackConfig", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ServiceRollbackConfig RollbackConfig { get; set; }

        /// <summary>
        /// Specifies attached networks.
        /// </summary>
        [JsonProperty(PropertyName = "Networks", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "Networks", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public List<ServiceNetwork> Networks { get; set; }

        /// <summary>
        /// Service endpoint and Docker load balancer settings.
        /// </summary>
        [JsonProperty(PropertyName = "EndpointSpec", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Populate)]
        [YamlMember(Alias = "EndpointSpec", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ServiceEndpointSpec EndpointSpec { get; set; }

        /// <inheritdoc/>
        public void Normalize()
        {
            Labels         = Labels ?? new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            TaskTemplate   = TaskTemplate ?? new ServiceTaskTemplate();
            Mode           = Mode ?? new ServiceSchedulingMode();
            UpdateConfig   = UpdateConfig ?? new ServiceUpdateConfig();
            RollbackConfig = RollbackConfig ?? new ServiceRollbackConfig();
            Networks       = Networks ?? new List<ServiceNetwork>();
            EndpointSpec   = EndpointSpec ?? new ServiceEndpointSpec();

            TaskTemplate?.Normalize();
            Mode?.Normalize();
            UpdateConfig?.Normalize();
            RollbackConfig?.Normalize();

            foreach (var item in Networks)
            {
                item?.Normalize();
            }

            EndpointSpec?.Normalize();
        }
    }
}
