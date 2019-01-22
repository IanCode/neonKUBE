﻿//-----------------------------------------------------------------------------
// FILE:	    KubeContextExtension.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

using Neon.Common;
using Neon.Cryptography;

namespace Neon.Kube
{
    /// <summary>
    /// Holds extended cluster information such as the cluster definition and
    /// node SSH credentials.  These records are persisted as files to the 
    /// <b>$HOME/.neonkube/contexts</b> folder in YAML files named like
    /// <b><i>NAME</i>.context.yaml</b>, where <i>NAME</i> identifies the 
    /// associated Kubernetes context.
    /// </summary>
    public class KubeContextExtension
    {
        private object syncRoot = new object();
        private string path;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public KubeContextExtension()
        {
        }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        /// <param name="path">Optionally specifies the path to the extension file.</param>
        public KubeContextExtension(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// The cluster definition.
        /// </summary>
        [JsonProperty(PropertyName = "clusterDefinition", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "clusterDefinition", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public ClusterDefinition ClusterDefinition { get; set; }

        /// <summary>
        /// Indicates whether provisioning is complete but setup is still
        /// pending for this cluster
        /// </summary>
        [JsonProperty(PropertyName = "SetupPending", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "SetupPending", ApplyNamingConventions = false)]
        [DefaultValue(false)]
        public bool SetupPending { get; set; } = false;

        /// <summary>
        /// The SSH root username.
        /// </summary>
        [JsonProperty(PropertyName = "SshUsername", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "SshUsername", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string SshUsername { get; set; }

        /// <summary>
        /// The SSH root password.
        /// </summary>
        [JsonProperty(PropertyName = "SshPassword", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "SshPassword", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string SshPassword { get; set; }

        /// <summary>
        /// Returns a <see cref="SshCredentials"/> instance suitable for connecting to
        /// a cluster node.
        /// </summary>
        [JsonIgnore]
        [YamlIgnore]
        public SshCredentials SshCredentials => SshCredentials.FromUserPassword(SshUsername, SshPassword);

        /// <summary>
        /// Temporarily holds the strong password during cluster setup.
        /// </summary>
        [JsonProperty(PropertyName = "SshStrongPassword", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "SshStrongPassword", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string SshStrongPassword { get; set; }

        /// <summary>
        /// Indicates whether a strong host SSH password was generated for the cluster.
        /// This defaults to <c>false</c>.
        /// </summary>
        [JsonProperty(PropertyName = "HasStrongSshPassword", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "HasStrongSshPassword", ApplyNamingConventions = false)]
        [DefaultValue(false)]
        public bool HasStrongSshPassword { get; set; }

        /// <summary>
        /// The SSH RSA private key fingerprint used to secure the cluster nodes.  Thisk is a
        /// MD5 hash encoded as hex bytes separated by colons.
        /// </summary>
        [JsonProperty(PropertyName = "SshNodeFingerprint")]
        [YamlMember(Alias = "SshNodeFingerprint", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(false)]
        public string SshNodeFingerprint { get; set; }

        /// <summary>
        /// The SSH RSA private key used to secure the cluster nodes.
        /// </summary>
        [JsonProperty(PropertyName = "SshNodePrivateKey")]
        [YamlMember(Alias = "SshNodePrivateKey", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(false)]
        public string SshNodePrivateKey { get; set; }

        /// <summary>
        /// The SSH RSA private key used to secure the cluster nodes.
        /// </summary>
        [JsonProperty(PropertyName = "SshNodePublicKey")]
        [YamlMember(Alias = "SshNodePublicKey", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(false)]
        public string SshNodePublicKey { get; set; }

        /// <summary>
        /// The public and private parts of the SSH client key used to
        /// authenticate an SSH session with a cluster node.
        /// </summary>
        [JsonProperty(PropertyName = "SshClientKey", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "SshClientKey", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public SshClientKey SshClientKey { get; set; }

        /// <summary>
        /// The command to be used join nodes to an existing cluster.
        /// </summary>
        [JsonProperty(PropertyName = "ClusterJoinCommand", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "ClusterJoinCommand", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string ClusterJoinCommand { get; set; }

        /// <summary>
        /// The Kubernetes admin configuration file obtained from the first master at: <b>/etc/kubernetes/admin.conf</b>.
        /// </summary>
        [JsonProperty(PropertyName = "AdminConfig", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "AdminConfig", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string AdminConfig { get; set; }

        /// <summary>
        /// Sets the file path where the extension will be persisted.
        /// </summary>
        /// <param name="path">The target path.</param>
        internal void SetPath(string path)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path));

            this.path = path;
        }

        /// <summary>
        /// <para>
        /// Persists the extension data.
        /// </para>
        /// <note>
        /// A valid path must have been passed to the constructor for this to work.
        /// </note>
        /// </summary>
        public void Save()
        {
            lock (syncRoot)
            {
                File.WriteAllText(path, NeonHelper.YamlSerialize(this));
            }
        }
    }
}