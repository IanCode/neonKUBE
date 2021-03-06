﻿//-----------------------------------------------------------------------------
// FILE:	    SshClientKey.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using Neon.IO;

namespace Neon.Kube
{
    /// <summary>
    /// Describes a client key used for SSH public key authentication.
    /// </summary>
    /// <remarks>
    /// <note>
    /// Only <b>RSA</b> keys should be used in production.  Other keys like DSA are
    /// no longer considered secure.
    /// </note>
    /// <para>
    /// SSH authentication keys have two parts, the public key that needs to be deployed
    /// to every server machine and the private key that will be retained on client
    /// machines which will be used to sign authentication challenges by servers.
    /// </para>
    /// <para>
    /// The <see cref="PublicPUB"/> property holds the public key.  This key has a 
    /// standard format can can be appended directly to the <b>authorized_keys</b>
    /// file on a Linux machine.
    /// </para>
    /// <para>
    /// <see cref="PrivatePEM"/> and <see cref="PrivatePPK"/> hold the private key
    /// using two different formats.  <see cref="PrivatePEM"/> uses the <b>OpenSSH</b>
    /// format and is suitable for deployment on Linux client workstations.  <see cref="PrivatePPK"/> 
    /// uses the <b>PuTTY Private Key (PPK)</b> format and is suitable for deploying
    /// on Windows client workstations that use PuTTY and WinSCP.
    /// </para>
    /// <para>
    /// <see cref="Passphrase"/> is not currently used but eventually, this will
    /// enable an additional level of encryption at rest.
    /// </para>
    /// </remarks>
    public class SshClientKey
    {
        /// <summary>
        /// The RSA public key to deployed on the server for authenticating SSH clients.
        /// This has the <b>PUB</b> format as generated by the Linux <b>ssh-keygen</b>
        /// tool.
        /// </summary>
        [JsonProperty(PropertyName = "PublicPUB", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "publicPUB", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string PublicPUB { get; set; }

        /// <summary>
        /// The private key formatted for <b>OpenSSH</b> (PEM formatted).  
        /// </summary>
        [JsonProperty(PropertyName = "PrivatePEM", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "privatePEM", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string PrivatePEM { get; set; }

        /// <summary>
        /// The private key formatted as <b>PuTTY Private Key (PPK)</b>.
        /// </summary>
        [JsonProperty(PropertyName = "PrivatePPK", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "privatePPK", ScalarStyle = ScalarStyle.Literal, ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string PrivatePPK { get; set; }

        /// <summary>
        /// <b>Not Implemented Yet:</b> The optional passphrase used for additional security.
        /// </summary>
        [JsonProperty(PropertyName = "Passphrase", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [YamlMember(Alias = "passphrase", ApplyNamingConventions = false)]
        [DefaultValue(null)]
        public string Passphrase { get; set; }
    }
}
