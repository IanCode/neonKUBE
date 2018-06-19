﻿//-----------------------------------------------------------------------------
// FILE:	    DockerManager.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Common;
using Neon.Docker;
using Neon.IO;
using Neon.Net;
using Neon.Retry;
using Neon.Time;
namespace Neon.Cluster
{
    /// <summary>
    /// Handles Docker related operations for a <see cref="ClusterProxy"/>.
    /// </summary>
    public sealed class DockerManager
    {
        private ClusterProxy cluster;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="cluster">The parent <see cref="ClusterProxy"/>.</param>
        internal DockerManager(ClusterProxy cluster)
        {
            Covenant.Requires<ArgumentNullException>(cluster != null);

            this.cluster = cluster;
            this.Config  = new DockerConfigManager(cluster);
            this.Secret  = new DockerSecretManager(cluster);
        }

        /// <summary>
        /// Returns Docker config manager.
        /// </summary>
        public DockerConfigManager Config { get; private set; }

        /// <summary>
        /// Returns the Docker secret manager.
        /// </summary>
        public DockerSecretManager Secret { get; private set; }

        /// <summary>
        /// Inspects a service, returning details about its current state.
        /// </summary>
        /// <param name="name">The service name.</param>
        /// <param name="strict">Optionally specify strict JSON parsing.</param>
        /// <returns>The <see cref="ServiceDetails"/> or <c>null</c> if the service doesn't exist.</returns>
        public ServiceDetails InspectService(string name, bool strict = false)
        {
            var response = cluster.GetHealthyManager().DockerCommand(RunOptions.None, "docker", "service", "inspect", name);

            if (response.ExitCode != 0)
            {
                if (response.AllText.Contains("Status: Error: no such service:"))
                {
                    return null;
                }

                throw new Exception($"Cannot inspect service [{name}]: {response.AllText}");
            }

            // The inspection response is actually an array with a single
            // service details element, so we'll need to extract that element
            // and then parse it.

            var jArray      = JArray.Parse(response.OutputText);
            var jsonDetails = jArray[0].ToString(Formatting.Indented);
            var details     = NeonHelper.JsonDeserialize<ServiceDetails>(jsonDetails, strict);

            details.Normalize();

            return details;
        }
    }
}
