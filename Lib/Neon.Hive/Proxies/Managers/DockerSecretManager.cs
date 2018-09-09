﻿//-----------------------------------------------------------------------------
// FILE:	    DockerSecretManager.cs
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

using Consul;

using Neon.Common;
using Neon.IO;
using Neon.Net;
using Neon.Retry;
using Neon.Time;

namespace Neon.Hive
{
    /// <summary>
    /// Handles Docker secret related operations for a <see cref="HiveProxy"/>.
    /// </summary>
    public sealed class DockerSecretManager
    {
        private HiveProxy hive;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="hive">The parent <see cref="HiveProxy"/>.</param>
        internal DockerSecretManager(HiveProxy hive)
        {
            Covenant.Requires<ArgumentNullException>(hive != null);

            this.hive = hive;
        }

        /// <summary>
        /// Determines whether a Docker secret exists.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns><c>true</c> if the secret exists.</returns>
        /// <exception cref="HiveException">Thrown if the operation failed.</exception>
        public bool Exists(string secretName)
        {
            var manager  = hive.GetReachableManager();
            var response = manager.DockerCommand(RunOptions.None, "docker secret inspect", secretName);

            if (response.ExitCode == 0)
            {
                return true;
            }
            else
            {
                // $todo(jeff.lill): 
                //
                // I'm trying to distinguish between a a failure because the secret doesn't
                // exist and other potential failures (e.g. Docker is not running).
                //
                // This is a bit fragile.

                if (response.ErrorText.StartsWith("Status: Error: no such secret:", StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
                else
                {
                    throw new HiveException(response.ErrorSummary);
                }
            }
        }

        /// <summary>
        /// Creates or updates a hive Docker string secret.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="value">The secret value.</param>
        /// <param name="options">Optional command run options.</param>
        /// <exception cref="HiveException">Thrown if the operation failed.</exception>
        public void Set(string secretName, string value, RunOptions options = RunOptions.None)
        {
            Covenant.Requires<ArgumentException>(HiveDefinition.IsValidName(secretName));
            Covenant.Requires<ArgumentNullException>(value != null);

            Set(secretName, Encoding.UTF8.GetBytes(value), options);
        }

        /// <summary>
        /// Creates or updates a hive Docker binary secret.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="value">The secret value.</param>
        /// <param name="options">Optional command run options.</param>
        /// <exception cref="HiveException">Thrown if the operation failed.</exception>
        public void Set(string secretName, byte[] value, RunOptions options = RunOptions.None)
        {
            Covenant.Requires<ArgumentException>(HiveDefinition.IsValidName(secretName));
            Covenant.Requires<ArgumentNullException>(value != null);

            var bundle = new CommandBundle("./create-secret.sh");

            bundle.AddFile("secret.dat", value);
            bundle.AddFile("create-secret.sh",
$@"#!/bin/bash

if docker secret inspect {secretName} ; then
    echo ""Secret already exists; not setting it again.""
else
    cat secret.dat | docker secret create {secretName} -

    # It appears that Docker secrets may not be available
    # immediately after they are created.  So, we're going 
    # poll for a while until we can inspect the new secret.

    count=0

    while [ $count -le 30 ]
    do
        if docker secret inspect {secretName} ; then
            exit 0
        fi
        
        sleep 1
        count=$(( count + 1 ))
    done

    echo ""Created secret [{secretName}] is not ready after 30 seconds."" >&2
    exit 1
fi
",
                isExecutable: true);

            var response = hive.GetReachableManager().SudoCommand(bundle, options);

            if (response.ExitCode != 0)
            {
                throw new HiveException(response.ErrorSummary);
            }
        }

        /// <summary>
        /// Deletes a hive Docker secret.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="options">Optional command run options.</param>
        /// <exception cref="HiveException">Thrown if the operation failed.</exception>
        public void Remove(string secretName, RunOptions options = RunOptions.None)
        {
            Covenant.Requires<ArgumentException>(HiveDefinition.IsValidName(secretName));

            var bundle = new CommandBundle("./delete-secret.sh");

            bundle.AddFile("delete-secret.sh",
$@"#!/bin/bash
docker secret inspect {secretName}

if [ ""$?"" != ""0"" ] ; then
    echo ""Secret doesn't exist.""
else
    docker secret rm {secretName}
fi
",              isExecutable: true);

            var response = hive.GetReachableManager().SudoCommand(bundle, RunOptions.None);

            if (response.ExitCode != 0)
            {
                throw new HiveException(response.ErrorSummary);
            }
        }

        /// <summary>
        /// Retrieves a Docker secret.  Note that this will take several seconds.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="options">Optional command run options.</param>
        /// <exception cref="TimeoutException">Thrown if the operation timed out.</exception>
        /// <exception cref="HiveException">Thrown if the operation failed.</exception>
        public byte[] Retrieve(string secretName, RunOptions options = RunOptions.None)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(secretName));

            // This works by mounting the secret into a new [neon-secret-retriever]
            // service instance, passing arguments requesting that the secret be
            // persisted to a Consul key.
            //
            // We'll monitor the Consul for the new key and once we've detected that
            // [neon-secret-retriever] has persisted the secret, we'll read it and then
            // we'll remove the secret key and the service.
            //
            // As a convention, we're going to persist the secrets to Consul keys like:
            //
            //      [timestamp-GUID]
            //
            // where [timestamp] will be set to something like [2018-06-05T14:30:13.000Z] 
            // indicating the time when the secret was requested and GUID is a generated 
            // unique ID.  We're also going to name the service [neon-secret-retriever-GUID].
            //
            // To help prevent the accumulation of secret keys and [neon-secret-retriever]
            // service instances, [neon-hive-manager] removes secrets with timestamps
            // older than an hour and Docker services whose name start with "neon-secret-retriever-"
            // that have been running for more than an hour.

            var timeout   = TimeSpan.FromSeconds(30);     // It should never take this long
            var timestamp = DateTime.UtcNow.ToString(NeonHelper.DateFormatTZ);
            var guid      = Guid.NewGuid().ToString("D").ToLowerInvariant();
            var consulKey = $"neon/service/neon-secret-retriever/{timestamp}~{guid}";
            var manager   = hive.GetReachableManager();

            // Start the [neon-secret-retriever] service.

            var response = manager.SudoCommand($"docker service create {hive.Definition.SecretRetrieverImage}", options, secretName, consulKey);

            if (response.ExitCode != 0)
            {
                throw new HiveException(response.ErrorSummary);
            }

            // Wait for service to write the secret to Consul.

            var secret = (byte[])null;

            NeonHelper.WaitFor(
                () =>
                {
                    try
                    {
                        secret = hive.Consul.Client.KV.GetBytesOrDefault(consulKey).Result;

                        if (secret != null)
                        {
                            // [neon-secret-retriever] has written the secret.

                            return true;
                        }
                    }
                    catch
                    {
                        // Intentionally ignoring this.
                    }

                    return false;
                },
                timeout: timeout,
                pollTime: TimeSpan.FromSeconds(0.5));

            // We have the secret so remove the consul key and the service.

            hive.Consul.Client.KV.Delete(consulKey);

            response = manager.SudoCommand($"docker service rm {hive.Definition.SecretRetrieverImage}", options);

            if (response.ExitCode != 0)
            {
                throw new HiveException(response.ErrorSummary);
            }

            return secret;
        }
    }
}
