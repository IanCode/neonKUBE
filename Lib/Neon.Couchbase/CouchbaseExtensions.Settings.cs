﻿//-----------------------------------------------------------------------------
// FILE:	    CouchbaseExtensions.Settings.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.Core.Serialization;

using Neon.Common;
using Neon.Data;
using Neon.Time;

namespace Couchbase
{
    /// <summary>
    /// Couchbase related extensions.
    /// </summary>
    public static partial class CouchbaseExtensions
    {
        /// <summary>
        /// Returns a Couchbase cluster connection using specified settings and the username and password.
        /// </summary>
        /// <param name="settings">The Couchbase settings.</param>
        /// <param name="username">The cluster username.</param>
        /// <param name="password">The cluster password.</param>
        /// <returns>The connected <see cref="Cluster"/>.</returns>
        public static Cluster OpenCluster(this CouchbaseSettings settings, string username, string password)
        {
            Covenant.Requires<ArgumentNullException>(settings.Servers != null && settings.Servers.Count > 0);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(username));
            Covenant.Requires<ArgumentNullException>(password != null);

            var config  = settings.ToClientConfig();
            var cluster = new Cluster(config);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                cluster.Authenticate(new Authentication.PasswordAuthenticator(username, password));
            }

            return cluster;
        }

        /// <summary>
        /// Returns a Couchbase cluster connection using specified settings and <see cref="Credentials"/>.
        /// </summary>
        /// <param name="settings">The Couchbase settings.</param>
        /// <param name="credentials">Cluster credentials.</param>
        /// <returns>The connected <see cref="Cluster"/>.</returns>
        public static Cluster OpenCluster(this CouchbaseSettings settings, Credentials credentials = null)
        {
            Covenant.Requires<ArgumentNullException>(settings.Servers != null && settings.Servers.Count > 0);
            Covenant.Requires<ArgumentNullException>(credentials != null);
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(credentials.Username));
            Covenant.Requires<ArgumentNullException>(credentials.Password != null);

            var config  = settings.ToClientConfig();
            var cluster = new Cluster(config);

            cluster.Authenticate(credentials.Username, credentials.Password);

            return cluster;
        }

        /// <summary>
        /// Returns a Couchbase bucket connection using specified settings and the cluster credentials.
        /// </summary>
        /// <param name="settings">The Couchbase settings.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="timeout">The optional timeout (defaults to 30 seconds).</param>
        /// <returns>The connected <see cref="NeonBucket"/>.</returns>
        /// <exception cref="TimeoutException">Thrown if the bucket is not ready after waiting <paramref name="timeout"/>.</exception>
        public static NeonBucket OpenBucket(this CouchbaseSettings settings, string username, string password, TimeSpan timeout = default(TimeSpan))
        {
            var config = settings.ToClientConfig();

            config.BucketConfigs.Clear();

            config.BucketConfigs.Add(settings.Bucket,
                new BucketConfiguration()
                {
                    BucketName = settings.Bucket,

                    PoolConfiguration = new PoolConfiguration()
                    {
                        ConnectTimeout = settings.ConnectTimeout,
                        SendTimeout    = settings.SendTimeout,
                        MaxSize        = settings.MaxPoolConnections,
                        MinSize        = settings.MinPoolConnections
                    }
                });

            var cluster = new Cluster(config);

            // We have to wait for two Couchbase operations to complete
            // successfully:
            //
            //      1. Authenticate
            //      2. Open Bucket
            //
            // Each of these can fail if Couchbase isn't ready.  The Open Bucket
            // can fail after the Authenticate succeeded because the bucket is still
            // warming up.

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            NeonHelper.WaitFor(
                () =>
                {
                    try
                    {
                        cluster.Authenticate(username, password);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout: timeout,
                pollTime: TimeSpan.FromSeconds(0.5));

            timeout = timeout - stopwatch.Elapsed;  // Adjust the timeout downward by the time taken to authenticate.

            var bucket = (NeonBucket)null;

            NeonHelper.WaitFor(
                () =>
                {
                    try
                    {
                        bucket = new NeonBucket(cluster.OpenBucket(settings.Bucket), settings);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout: timeout,
                pollTime: TimeSpan.FromSeconds(0.5));

            return bucket;
        }

        /// <summary>
        /// Returns a Couchbase bucket connection using specified settings and credentials.
        /// </summary>
        /// <param name="settings">The Couchbase settings.</param>
        /// <param name="credentials">The credentials.</param>
        /// <returns>The connected <see cref="NeonBucket"/>.</returns>
        public static NeonBucket OpenBucket(this CouchbaseSettings settings, Credentials credentials)
        {
            Covenant.Requires<ArgumentNullException>(credentials != null);

            return settings.OpenBucket(credentials.Username, credentials.Password);
        }

        /// <summary>
        /// Converts a <see cref="CouchbaseSettings"/> into a <see cref="ClientConfiguration"/>.
        /// </summary>
        /// <param name="settings">The simplified Couchbase settings instance.</param>
        /// <returns>A Couchbase <see cref="ClientConfiguration"/>.</returns>
        public static ClientConfiguration ToClientConfig(this CouchbaseSettings settings)
        {
            var config = new ClientConfiguration();

            config.Servers.Clear();

            foreach (var uri in settings.Servers)
            {
                config.Servers.Add(uri);
            }

            config.UseSsl                   = false;
            config.Serializer               = () => new DefaultSerializer(NeonHelper.JsonRelaxedSerializerSettings.Value, NeonHelper.JsonRelaxedSerializerSettings.Value);
            config.DefaultOperationLifespan = (uint)settings.OperationTimeout;
            config.DefaultConnectionLimit   = settings.MaxPoolConnections;

            return config;
        }
    }
}
