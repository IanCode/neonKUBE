﻿//-----------------------------------------------------------------------------
// FILE:	    HyperVHostingManager.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Common;
using Neon.Cryptography;
using Neon.IO;
using Neon.Net;
using Neon.Time;

namespace Neon.Kube
{
    /// <summary>
    /// Manages cluster provisioning on remote Hyper-V servers.
    /// </summary>
    [HostingProvider(HostingEnvironments.HyperV)]
    public class HyperVHostingManager : HostingManager
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Ensures that the assembly hosting this hosting manager is loaded.
        /// </summary>
        public static void Load()
        {
            // We don't have to do anything here because the assembly is loaded
            // as a byproduct of calling this.
        }

        //---------------------------------------------------------------------
        // Instance members

        private ClusterProxy                    cluster;
        private SetupController<NodeDefinition> controller;
        private string                          driveTemplatePath;
        private string                          vmDriveFolder;
        private string                          switchName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cluster">The cluster being managed.</param>
        /// <param name="logFolder">
        /// The folder where log files are to be written, otherwise or <c>null</c> or 
        /// empty if logging is disabled.
        /// </param>
        public HyperVHostingManager(ClusterProxy cluster, string logFolder = null)
        {
            cluster.HostingManager = this;

            this.cluster = cluster;
        }

        /// <inheritdoc/>
        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        /// <inheritdoc/>
        public override void Validate(ClusterDefinition clusterDefinition)
        {
        }

        /// <inheritdoc/>
        public override bool Provision(bool force)
        {
            throw new NotImplementedException("$todo(jeff.lill): Implement this.");
        }

        /// <inheritdoc/>
        public override (string Address, int Port) GetSshEndpoint(string nodeName)
        {
            return (Address: cluster.GetNode(nodeName).PrivateAddress.ToString(), Port: NetworkPorts.SSH);
        }

        /// <inheritdoc/>
        public override void AddPostProvisionSteps(SetupController<NodeDefinition> controller)
        {
        }

        /// <inheritdoc/>
        public override string DrivePrefix
        {
            get { return "sd"; }
        }
    }
}