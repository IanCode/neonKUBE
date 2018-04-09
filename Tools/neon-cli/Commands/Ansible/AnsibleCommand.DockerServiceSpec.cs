﻿//-----------------------------------------------------------------------------
// FILE:	    AnsibleCommand.DockerServiceSpec.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ICSharpCode.SharpZipLib.Zip;

using Neon.Cluster;
using Neon.Cryptography;
using Neon.Common;
using Neon.IO;
using Neon.Net;

namespace NeonCli.Ansible.DockerService
{
    // NOTE: The types below are accurate as of Docker API version 1.35.

    public enum EndpointMode
    {
        [EnumMember(Value = "vip")]
        Vip = 0,

        [EnumMember(Value = "dnsrr")]
        DnsRR
    }

    public enum IsolationMode
    {
        [EnumMember(Value = "default")]
        Default = 0,

        [EnumMember(Value = "process")]
        Process,

        [EnumMember(Value = "hyperv")]
        HyperV
    }

    public enum ServiceMode
    {
        [EnumMember(Value = "replicated")]
        Replicated = 0,

        [EnumMember(Value = "global")]
        Global
    }

    public enum RestartCondition
    {
        [EnumMember(Value = "any")]
        Any = 0,

        [EnumMember(Value = "none")]
        None,

        [EnumMember(Value = "on-failure")]
        OnFailure
    }

    public enum UpdateOrder
    {
        [EnumMember(Value = "stop-first")]
        StopFirst = 0,

        [EnumMember(Value = "start-first")]
        StartFirst
    }

    public enum RollbackOrder
    {
        [EnumMember(Value = "stop-first")]
        StopFirst = 0,

        [EnumMember(Value = "start-first")]
        StartFirst
    }

    public enum UpdateFailureAction
    {
        [EnumMember(Value = "pause")]
        Pause = 0,

        [EnumMember(Value = "continue")]
        Continue,

        [EnumMember(Value = "rollback")]
        Rollback
    }

    public enum RollbackFailureAction
    {
        [EnumMember(Value = "pause")]
        Pause = 0,

        [EnumMember(Value = "continue")]
        Continue,
    }

    public enum PortMode
    {
        [EnumMember(Value = "ingress")]
        Ingress = 0,

        [EnumMember(Value = "host")]
        Host
    }

    public enum PortProtocol
    {
        [EnumMember(Value = "tcp")]
        Tcp = 0,

        [EnumMember(Value = "udp")]
        Udp,

        [EnumMember(Value = "sctp")]
        Sctp
    }

    public class PublishPort
    {
        public string Name { get; set; }

        public int? Published { get; set; }

        public int? Target { get; set; }

        public PortMode? Mode { get; set; }

        public PortProtocol? Protocol { get; set; }

        public string ToCommandOption()
        {
            var sb = new StringBuilder();

            sb.AppendWithSeparator($"published={Published}", ",");
            sb.AppendWithSeparator($"target={Target}", ",");

            if (Mode.HasValue)
            {
                sb.AppendWithSeparator($"mode={Mode.Value}", ",");
            }
            else
            {
                sb.AppendWithSeparator($"mode=ingress", ",");
            }

            if (Protocol.HasValue)
            {
                sb.AppendWithSeparator($"protocol={Protocol.Value}", ",");
            }
            else
            {
                sb.AppendWithSeparator($"protocol=tcp", ",");
            }

            return sb.ToString();
        }
    }

    public enum MountType
    {
        [EnumMember(Value = "volume")]
        Volume = 0,

        [EnumMember(Value = "bind")]
        Bind,

        [EnumMember(Value = "tmpfs")]
        Tmpfs
    }

    public enum MountConsistency
    {
        [EnumMember(Value = "default")]
        Default,

        [EnumMember(Value = "consistent")]
        Consistent,

        [EnumMember(Value = "cached")]
        Cached,

        [EnumMember(Value = "delegated")]
        Delegated
    }

    public enum MountBindPropagation
    {
        [EnumMember(Value = "rprivate")]
        RPrivate = 0,

        [EnumMember(Value = "shared")]
        Shared,

        [EnumMember(Value = "slave")]
        Slave,

        [EnumMember(Value = "private")]
        Private,

        [EnumMember(Value = "rshared")]
        RShared,

        [EnumMember(Value = "rslave")]
        RSlave
    }

    public class Mount
    {
        public MountType? Type { get; set; }

        public string Source { get; set; }

        public string Target { get; set; }

        public bool? ReadOnly { get; set; }

        public MountConsistency? Consistency { get; set; }

        public MountBindPropagation? BindPropagation { get; set; }

        public string VolumeDriver { get; set; }

        public List<string> VolumeLabel { get; private set; } = new List<string>();

        public bool? VolumeNoCopy { get; set; }

        public List<string> VolumeOpt { get; private set; } = new List<string>();

        public long? TmpfsSize { get; set; }

        public string TmpfsMode { get; set; }

        public string ToCommandOption()
        {
            var sb = new StringBuilder();

            if (Type.HasValue)
            {
                sb.AppendWithSeparator($"type={Type.Value}", ",");
            }
            else
            {
                sb.AppendWithSeparator($"type=volume", ",");
            }

            if (Source != null)
            {
                sb.AppendWithSeparator($"source={Source}", ",");
            }

            if (Target != null)
            {
                sb.AppendWithSeparator($"target={Target}", ",");
            }

            if (ReadOnly.HasValue)
            {
                sb.AppendWithSeparator($"readonly={ReadOnly.Value.ToString().ToLowerInvariant()}", ",");
            }
            else
            {
                sb.AppendWithSeparator($"readonly=false", ",");
            }

            if (Consistency.HasValue)
            {
                sb.AppendWithSeparator($"consistency={Consistency.Value}", ",");
            }
            else
            {
                sb.AppendWithSeparator($"consistency=default", ",");
            }

            if (BindPropagation.HasValue)
            {
                sb.AppendWithSeparator($"bind-propagation={BindPropagation.Value}", ",");
            }
            else
            {
                sb.AppendWithSeparator($"bind-propagation=rprivate", ",");
            }

            if (!Type.HasValue || Type.Value == MountType.Volume)
            {
                if (VolumeDriver != null)
                {
                    sb.AppendWithSeparator($"volume-driver={VolumeDriver}", ",");
                }

                if (VolumeNoCopy.HasValue)
                {
                    sb.AppendWithSeparator($"volume-nocopy={VolumeNoCopy.ToString().ToLowerInvariant()}", ",");
                }
                else
                {
                    sb.AppendWithSeparator($"volume-nocopy=true", ",");
                }

                // I believe this needs to be specified last in the command line option.

                if (VolumeLabel.Count > 0)
                {
                    sb.AppendWithSeparator("volume-label=", ",");

                    foreach (var label in VolumeLabel)
                    {
                        sb.AppendWithSeparator(label, ",");
                    }
                }
            }

            return sb.ToString();
        }
    }

    public class Secret
    {
        public string Source { get; set; }

        public string Target { get; set; }

        public string Uid { get; set; }

        public string Gid { get; set; }

        public string Mode { get; set; }

        public string ToCommandOption()
        {
            var sb = new StringBuilder();

            if (Source != null)
            {
                sb.AppendWithSeparator($"source={Source}", ",");
            }

            if (Target != null)
            {
                sb.AppendWithSeparator($"target={Target}", ",");
            }

            if (Uid != null)
            {
                sb.AppendWithSeparator($"uid={Uid}", ",");
            }

            if (Gid != null)
            {
                sb.AppendWithSeparator($"gid={Gid}", ",");
            }

            if (Mode != null)
            {
                sb.AppendWithSeparator($"mode={Mode}", ",");
            }

            return sb.ToString();
        }
    }

    public class Config
    {
        public string Source { get; set; }

        public string Target { get; set; }

        public string Uid { get; set; }

        public string Gid { get; set; }

        public string Mode { get; set; }

        public string ToCommandOption()
        {
            var sb = new StringBuilder();

            if (Source != null)
            {
                sb.AppendWithSeparator($"source={Source}", ",");
            }

            if (Target != null)
            {
                sb.AppendWithSeparator($"target={Target}", ",");
            }

            if (Uid != null)
            {
                sb.AppendWithSeparator($"uid={Uid}", ",");
            }

            if (Gid != null)
            {
                sb.AppendWithSeparator($"gid={Gid}", ",");
            }

            if (Mode != null)
            {
                sb.AppendWithSeparator($"mode={Mode}", ",");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Specifies a Docker service.
    /// </summary>
    public class DockerServiceSpec
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// Creates a <see cref="DockerServiceSpec"/> by parsing the JSON responses from a
        /// <b>docker service inspect SERVICE</b> for a service as well as the table output
        /// from a <b>docker network ls --no-trunc</b> command listing the current networks.
        /// </summary>
        /// <param name="context">The Annsible module context.</param>
        /// <param name="inspectJson"><b>docker service inspect SERVICE</b> command output for the service.</param>
        /// <param name="networksText"><b>docker network ls --no-trunc</b> command output.</param>
        /// <returns>The parsed <see cref="DockerServiceSpec"/>.</returns>
        public static DockerServiceSpec FromDockerInspect(ModuleContext context, string inspectJson, string networksText)
        {
            var service = new DockerServiceSpec();

            service.Parse(context, inspectJson, networksText);

            return service;
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Constructor.
        /// </summary>
        public DockerServiceSpec()
        {
        }

        /// <summary>
        /// Optionally specifies service arguments.
        /// </summary>
        public List<string> Args { get; set; } = new List<string>();

        /// <summary>
        /// Optionally specifies credential specifications for Windows managed services.
        /// These are formatted like <b>file://NAME</b> or <b>registry://KEY</b>.
        /// </summary>
        public List<string> CredentialSpec { get; set; } = new List<string>();

        /// <summary>
        /// Identifies the configurations to be made available to the service.
        /// </summary>
        public List<Config> Config { get; set; } = new List<Config>();

        /// <summary>
        /// Specifies service container placement constraints.  These will look
        /// like <b>LABEL=VALUE</b> or <b>LABEL!=VALUE</b>.
        /// </summary>
        public List<string> Constraint { get; set; } = new List<string>();

        /// <summary>
        /// Specifies the service container labels.  These will look like
        /// <b>LABEL=VALUE</b>.
        /// </summary>
        public List<string> ContainerLabel { get; set; } = new List<string>();

        /// <summary>
        /// Indicates that the module should detach immediately from the service
        /// after signalling its creation or update rather than waiting for it
        /// to converge.
        /// </summary>
        public bool? Detach { get; set; }

        /// <summary>
        /// Specifies the DNS nameserver IP addresses for the container.
        /// </summary>
        public List<IPAddress> Dns { get; set; } = new List<IPAddress>();

        /// <summary>
        /// DNS options.  I believe these will be formatted like <b>OPTION=VALUE</b>
        /// but I'm not going to enforce this because I'm not sure.  The options
        /// are described here: http://manpages.ubuntu.com/manpages/precise/man5/resolvconf.conf.5.html
        /// </summary>
        public List<string> DnsOption { get; set; } = new List<string>();

        /// <summary>
        /// Specifies the DNS domains to be searched for non-fully qualified hostnames.
        /// </summary>
        public List<string> DnsSearch { get; set; } = new List<string>();

        /// <summary>
        /// Specifies the endpoint mode.
        /// </summary>
        public EndpointMode? EndpointMode { get; set; }

        /// <summary>
        /// Optionally overrides the image entrypoint command and arguments.
        /// </summary>
        public List<string> Command { get; set; } = new List<string>();

        /// <summary>
        /// Specifies environment variables to be passed to the service containers.  These
        /// will be formatted as <b>NAME=VALUE</b> to set explicit values or just <b>NAME</b>
        /// to pass the current value of a host variable.
        /// </summary>
        public List<string> Env { get; set; } = new List<string>();

        /// <summary>
        /// Specifies the host files with environment variable definitions to be
        /// passed to the service containers.
        /// </summary>
        public List<string> EnvFile { get; set; } = new List<string>();

        /// <summary>
        /// Specifies additional service container placement constraints.
        /// </summary>
        public List<string> GenericResource { get; set; } = new List<string>();

        /// <summary>
        /// Specifies supplementary user groups for the service containers.
        /// </summary>
        public List<string> Group { get; set; } = new List<string>();

        /// <summary>
        /// Optionally specifies the command to be executed within the service containers
        /// to determine the container health status.
        /// </summary>
        /// <remarks>
        /// This list is empty to if the default image health command it to be
        /// used, or else it will start with <b>NONE</b> to disable health checks,
        /// <b>CMD</b> for a regular command followed by optional arguments or 
        /// <b>CMD-SHELL</b> followed by a single argument that will be executed 
        /// in the image's default shell.
        /// </remarks>
        public List<string> HealthCmd { get; set; } = new List<string>();

        /// <summary>
        /// Optionally specifies the interval between health checks (nanoseconds).
        /// </summary>
        public long? HealthInterval { get; set; }

        /// <summary>
        /// Optionally specifies the number of times the <see cref="HealthCmd"/> can
        /// fail before a service container will be considered unhealthy.
        /// </summary>
        public long? HealthRetries { get; set; }

        /// <summary>
        /// Optionally specifies the period after the service container starts when
        /// health check failures will be ignored (nanoseconds).
        /// </summary>
        public long? HealthStartPeriod { get; set; }

        /// <summary>
        /// Optionally specifies the maximum time to wait for a health check to
        /// be completed (nanoseconds).
        /// </summary>
        public long? HealthTimeout { get; set; }

        /// <summary>
        /// Optionally specifies custom host/IP address mappings to be added to the service
        /// container's <b>/etc/hosts</b> file.  These are formatted like <b>HOST:IP</b>.
        /// </summary>
        public List<string> Host { get; set; } = new List<string>();

        /// <summary>
        /// Optionally overrides <see cref="Name"/> as the service's DNS hostname.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Specifies the Docker image.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// Optionally indicates that the service <see cref="Image"/> should not be repulled
        /// and updated if the image and tag are unchanged, ignoring the image SHA-256.
        /// See the remarks for more information.  This defaults to <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property has no effect when creating a service for the first time or
        /// if <see cref="Image"/> identifies a specific image via a SHA-256.  In these
        /// cases, the latest identified image will be downloaded and used to launch
        /// the service.
        /// </para>
        /// <para>
        /// When updating a service with <see cref="ImageUpdate"/>=<c>false</c>, the
        /// current service image will be compared to <see cref="Image"/>.  If the 
        /// image and tags match, then no attempt will be made to update the image.
        /// </para>
        /// <para>
        /// When updating a service with <see cref="ImageUpdate"/>=<c>true</c>, the
        /// registry will be checked for a new image and the service will be restarted
        /// to use the new image, if any.
        /// </para>
        /// </remarks>
        public bool? ImageUpdate { get; set; }

        /// <summary>
        /// Service container isolation mode (Windows only).
        /// </summary>
        public IsolationMode? Isolation { get; set; }

        /// <summary>
        /// Optionally specifies service labels.  These are formatted like <b>NAME=VALUE</b>.
        /// </summary>
        public List<string> Label { get; set; } = new List<string>();

        /// <summary>
        /// Limits the number of CPUs to be assigned to the service containers (double).
        /// </summary>
        public double? LimitCpu { get; set; }

        /// <summary>
        /// Optionally specifies the maximum RAM to be assigned to the container (bytes).
        /// </summary>
        public long? LimitMemory { get; set; }

        /// <summary>
        /// Optionally specifies the service logging driver.
        /// </summary>
        public string LogDriver { get; set; }

        /// <summary>
        /// Optionally specifies the log driver options.
        /// </summary>
        public List<string> LogOpt { get; set; } = new List<string>();

        /// <summary>
        /// Specifies the service mode.
        /// </summary>
        public ServiceMode? Mode { get; set; }

        /// <summary>
        /// Optionally specifies any service filesystem mounts.
        /// </summary>
        public List<Mount> Mount { get; set; } = new List<Mount>();

        /// <summary>
        /// The service name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optionally specifies any network attachments.
        /// </summary>
        public List<string> Network { get; set; } = new List<string>();

        /// <summary>
        /// Optionally disable container health checks.
        /// </summary>
        public bool? NoHealthCheck { get; set; }

        /// <summary>
        /// Optionally prevent querying the registry to resolve image digests
        /// and supported platforms.
        /// </summary>
        public bool? NoResolveImage { get; set; }

        /// <summary>
        /// Specifies service container placement preferences.  I'm not
        /// entirely sure of the format, so we're not going to parse these.
        /// </summary>
        public List<string> PlacementPref { get; set; } = new List<string>();

        /// <summary>
        /// Optionally publish a service port to the ingress network.
        /// </summary>
        public List<PublishPort> Publish { get; set; } = new List<PublishPort>();

        /// <summary>
        /// Optionally mount the container's root filesystem as read-only.
        /// </summary>
        public bool? ReadOnly { get; set; }

        /// <summary>
        /// Specifies the number of service instances deploy.
        /// </summary>
        public long? Replicas { get; set; }

        /// <summary>
        /// Optionally specifies the number of CPUs to reserve for each service
        /// instance.  This is a double so you can specify things like 1.5 CPUs.
        /// </summary>
        public double? ReserveCpu { get; set; }

        /// <summary>
        /// Optionally specifies the RAM to reserver for each service instance (bytes).
        /// </summary>
        public long? ReserveMemory { get; set; }

        /// <summary>
        /// Optionally specifies the condition when service containers will
        /// be restarted.
        /// </summary>
        public RestartCondition? RestartCondition { get; set; }

        /// <summary>
        /// Optionally specifies the delay between restart attempts (nanoseconds).
        /// </summary>
        public long? RestartDelay { get; set; }

        /// <summary>
        /// Optionally specifies the maximum number of service container restart attempts.
        /// </summary>
        public long? RestartMaxAttempts { get; set; } = -1;

        /// <summary>
        /// Optionally specifies the Window used to evaluate the restart policy (nanoseconds).
        /// </summary>
        public long? RestartWindow { get; set; }

        /// <summary>
        /// Optionally specifies the delay between service task rollbacks (nanoseconds).
        /// </summary>
        public long? RollbackDelay { get; set; }

        /// <summary>
        /// The action to take when service rollback fails.
        /// </summary>
        public RollbackFailureAction? RollbackFailureAction { get; set; }

        /// <summary>
        /// Optionally specifies the failure rate to tolerate during a rollback.
        /// </summary>
        public double? RollbackMaxFailureRatio { get; set; }

        /// <summary>
        /// Optionally specifies the time to wait after each task rollback to 
        /// monitor for failure (nanoseconds).
        /// </summary>
        public long? RollbackMonitor { get; set; }

        /// <summary>
        /// Optionally specifies the service task rollback order.
        /// </summary>
        public RollbackOrder? RollbackOrder { get; set; }

        /// <summary>
        /// Optionally specifies the maximum number of service tasks to be
        /// rolled back at once.
        /// </summary>
        public long? RollbackParallism { get; set; }

        /// <summary>
        /// Optionally specifies the secrets to be exposed to the service.
        /// </summary>
        public List<Secret> Secret { get; set; } = new List<Secret>();

        /// <summary>
        /// Optionally specifies the time to wait for a service container to
        /// stop gracefully after being signalled to stop before Docker will
        /// kill it forcefully (nanoseconds).
        /// </summary>
        public long? StopGracePeriod { get; set; }

        /// <summary>
        /// Optionally specifies the signal to be used to stop service containers.
        /// I believe this can be an integer or a signal name.
        /// </summary>
        public string StopSignal { get; set; }

        /// <summary>
        /// Optionally allocate a TTY for the service containers.
        /// </summary>
        public bool? Tty { get; set; }

        /// <summary>
        /// Optionally specifies the delay between service container updates (nanoseconds).
        /// </summary>
        public long? UpdateDelay { get; set; }

        /// <summary>
        /// Optionally specifies the action to take when a service container update fails.
        /// </summary>
        public UpdateFailureAction? UpdateFailureAction { get; set; }

        /// <summary>
        /// Optionally specifies the failure rate to tolerate during an update.
        /// </summary>
        public double? UpdateMaxFailureRatio { get; set; }

        /// <summary>
        /// Optionally specifies the time to wait after each service task update to 
        /// monitor for failure (nanoseconds).
        /// </summary>
        public long? UpdateMonitor { get; set; }

        /// <summary>
        /// Optionally specifies the service task update order.
        /// </summary>
        public UpdateOrder? UpdateOrder { get; set; }

        /// <summary>
        /// Optionally specifies the maximum number of service tasks to be
        /// updated at once.
        /// </summary>
        public long? UpdateParallism { get; set; }

        /// <summary>
        /// Optionally specifies the service container username/group.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Optionally sends registry authentication details to swarm agents
        /// hosting the service containers.
        /// </summary>
        public bool? WithRegistryAuth { get; set; }

        /// <summary>
        /// Optionally specifies the working directory within the service container.
        /// This will be set as the current directory before Docker executes a command
        /// within the container.
        /// </summary>
        public string WorkDir { get; set; }

        /// <summary>
        /// Parsing the JSON responses from a <b>docker service inspect SERVICE</b> for a 
        /// service as well as the table output from a <b>docker network ls --no-trunc</b> 
        /// command listing the current networks.
        /// </summary>
        /// <param name="context">The Annsible module context.</param>
        /// <param name="inspectJson"><b>docker service inspect SERVICE</b> command output for the service.</param>
        /// <param name="networksText"><b>docker network ls --no-trunc</b> command output.</param>
        /// <returns>The parsed <see cref="DockerServiceSpec"/>.</returns>
        /// <exception cref="Exception">Various exceptions are thrown for errors.</exception>
        private void Parse(ModuleContext context, string inspectJson, string networksText)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(inspectJson));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(networksText));

            //-----------------------------------------------------------------
            // Parse the network definitions so we can map network UUIDs to network names.
            // We're expecting the [docker network ls --no-trunc] output to look like:
            // 
            //  NETWORK ID                                                         NAME                DRIVER              SCOPE
            //  f2c93c25908a391398ef5416940c06322f3ac5f72ea915dd6a09a2efa49677b5   bridge              bridge              local
            //  d28f66e2c56338cdf3b870294ba7a2378d482f0890b89923cd359bf27305c180   docker_gwbridge     bridge              local
            //  b8840a636be38a454bcb0a87e660a37c21f8145112619e00ef632f51a2ca60a5   host                host                local
            //  c3oo8mz0vugdsqjytykcef664                                          ingress             overlay             swarm
            //  tzer69rfle2h6rbcjo9s4b3xo                                          neon-private        overlay             swarm
            //  x9o5teq0z1spgb6md9qmno1rz                                          neon-public         overlay             swarm
            //  f147d7e9bacb06d963bb09a2860953a78ab9c971852c310050a0341aab607e15   none                null                local

            var networkIdToName = new Dictionary<string, string>();

            using (var reader = new StringReader(networksText))
            {
                foreach (var line in reader.Lines().Skip(1))
                {
                    var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (fields.Length >= 2)
                    {
                        networkIdToName.Add(fields[0], fields[1]);
                    }
                }
            }

            // See the link below for more information on the REST API JSON format:
            //
            //      https://docs.docker.com/engine/api/v1.35/#operation/ServiceInspect
            //
            // The parsing code below basically follows the order of the properties
            // as defined by the REST specification.

            // We're expecting the inspection JSON to be a single item
            // array holding the service information.

            var jArray = JArray.Parse(inspectJson);

            if (jArray.Count != 1)
            {
                throw new ArgumentException("Invalid service inspection: expected a single element array.");
            }

            //-----------------------------------------------------------------
            // Extract the current service state from the service JSON.

            var spec         = (JObject)jArray[0]["Spec"];
            var taskTemplate = (JObject)spec["TaskTemplate"];

            //-----------------------------------------------------------------
            // Spec.Name

            this.Name = GetStringProperty(spec, "Name");

            //-----------------------------------------------------------------
            // Spec.Labels

            var labels = (JObject)spec.GetValue("Labels");

            foreach (var item in labels)
            {
                this.Label.Add($"{item.Key}={item.Value}");
            }

            //-----------------------------------------------------------------
            // Ignoring [Spec.TaskTemplate.PluginSpec] currently experimental
            // and I'm not sure that modifying managed plugins with this module
            // is a great idea anyway.

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.ContainerSpec

            var containerSpec = (JObject)taskTemplate["ContainerSpec"];

            this.Image = (string)containerSpec["Image"];

            foreach (var item in GetObjectProperty(containerSpec, "Labels"))
            {
                this.ContainerLabel.Add($"{item.Key}={item.Value}");
            }

            foreach (string arg in GetArrayProperty(containerSpec, "Command"))
            {
                this.Command.Add(arg);
            }

            foreach (string arg in GetArrayProperty(containerSpec, "Args"))
            {
                this.Args.Add(arg);
            }

            this.Hostname = GetStringProperty(containerSpec, "Hostname");

            foreach (string env in GetArrayProperty(containerSpec, "Env"))
            {
                this.Env.Add(env);
            }

            this.WorkDir = GetStringProperty(containerSpec, "Dir");
            this.User    = GetStringProperty(containerSpec, "User");

            foreach (string group in GetArrayProperty(containerSpec, "Groups"))
            {
                this.Group.Add(group);
            }

            // $todo(jeff.lill): Ignoring [Spec.TaskTemplate.Privileges] for now.

            this.Tty      = GetBoolProperty(containerSpec, "TTY");

            // $todo(jeff.lill): Ignoring [Spec.TaskTemplate.OpenStdin] for now.
            //
            // I think this corresponds to the [docker run -i] flag for containers 
            // but this doesn't make sense for services, right?

            this.ReadOnly = GetBoolProperty(containerSpec, "ReadOnly");

            foreach (JObject item in GetArrayProperty(containerSpec, "Mounts"))
            {
                var mount = new Mount();

                mount.Target          = GetStringProperty(item, "Target");
                mount.Source          = GetStringProperty(item, "Source");
                mount.Type            = GetEnumProperty<MountType>(item, "Type").Value;
                mount.ReadOnly        = GetBoolProperty(item, "ReadOnly");
                mount.Consistency     = GetEnumProperty<MountConsistency>(item, "Consistency").Value;

                switch (mount.Type)
                {
                    case MountType.Bind:

                        {
                            var bindOptions = GetObjectProperty(item, "BindOptions");

                            mount.BindPropagation = GetEnumProperty<MountBindPropagation>(bindOptions, "Propagation");
                        }
                        break;

                    case MountType.Volume:

                        {
                            var volumeOptions = GetObjectProperty(item, "VolumeOptions");

                            mount.VolumeNoCopy = GetBoolProperty(volumeOptions, "NoCopy");

                            foreach (var label in GetObjectProperty(volumeOptions, "Labels"))
                            {
                                mount.VolumeLabel.Add($"{label.Key}={label.Value}");
                            }

                            var driverConfig = GetObjectProperty(item, "DriverConfig");

                            mount.VolumeDriver = GetStringProperty(driverConfig, "Name");

                            var sb = new StringBuilder();

                            foreach (var option in GetObjectProperty(volumeOptions, "Options"))
                            {
                                sb.AppendWithSeparator($"{option.Key}={option.Value}", ",");
                            }
                        }
                        break;

                    case MountType.Tmpfs:

                        {
                            var tmpfsOptions = GetObjectProperty(item, "TempfsOptions");

                            mount.TmpfsSize = GetLongProperty(tmpfsOptions, "SizeBytes");
                            mount.TmpfsMode = GetFileModeProperty(tmpfsOptions, "Mode");
                        }
                        break;
                }

                this.Mount.Add(mount);
            }

            this.StopSignal      = GetStringProperty(containerSpec, "StopSignal");
            this.StopGracePeriod = GetLongProperty(containerSpec, "StopGracePeriod");

            // NOTE:
            //
            // [HealthCheck.Test] is either an empty array, indicating that the HEALTHCHECK
            // specified in the image (if any will be used).  Otherwise the first
            // element of the the array describes the health check type with the
            // command itself to follow.  Here's how this looks:
            //
            //      []                      - Use image health check
            //      ["NONE"]                - Disable health checking
            //      ["CMD", args...]        - Execute a command
            //      ["CMD-SHELL", command]  - Run a command in the container's shell

            var healthCheck = GetObjectProperty(containerSpec, "HealthCheck");

            foreach (string arg in GetArrayProperty(healthCheck, "Test"))
            {
                this.HealthCmd.Add(arg);
            }

            this.HealthInterval    = GetLongProperty(healthCheck, "Interval");
            this.HealthTimeout     = GetLongProperty(healthCheck, "Timeout");
            this.HealthRetries     = GetLongProperty(healthCheck, "Retries");
            this.HealthStartPeriod = GetLongProperty(healthCheck, "StartPeriod");

            foreach (string host in GetArrayProperty(containerSpec, "Hosts"))
            {
                // NOTE: 
                //
                // The REST API allows additional aliases to be specified after
                // the first host name.  We're going to ignore these because 
                // there's no way to specify these on the command line which
                // specifies these as:
                //
                //      HOST:IP

                var fields = host.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                this.Host.Add($"{fields[0]}:{fields[1]}");
            }

            var dnsConfig = GetObjectProperty(containerSpec, "DNSConfig");

            foreach (string nameserver in GetArrayProperty(dnsConfig, "Nameservers"))
            {
                this.Dns.Add(IPAddress.Parse(nameserver));
            }

            foreach (string domain in GetArrayProperty(dnsConfig, "Search"))
            {
                this.DnsSearch.Add(domain);
            }

            foreach (string option in GetArrayProperty(dnsConfig, "Options"))
            {
                // $todo(jeff.lill):
                //
                // I'm guessing here that the service inspect JSON uses ':'
                // instead of '=' like the command line.  I'm going to 
                // convert any colons to equal signs.

                this.DnsOption.Add(option.Replace(':', '='));
            }

            foreach (JObject secretSpec in GetArrayProperty(containerSpec, "secrets"))
            {
                var secret = new Secret();

                secret.Source = GetStringProperty(secretSpec, "SecretName");

                var secretFile = GetObjectProperty(secretSpec, "File");

                secret.Target = GetStringProperty(secretFile, "Name");
                secret.Uid    = GetStringProperty(secretFile, "UID");
                secret.Gid    = GetStringProperty(secretFile, "GID");
                secret.Mode   = GetFileModeProperty(secretFile, "Mode");

                this.Secret.Add(secret);
            }

            foreach (JObject configSpec in GetArrayProperty(containerSpec, "Configs"))
            {
                var config = new Config();

                config.Source = GetStringProperty(configSpec, "ConfigName");

                var configFile = GetObjectProperty(configSpec, "File");

                config.Target = GetStringProperty(configFile, "Name");
                config.Uid    = GetStringProperty(configFile, "UID");
                config.Gid    = GetStringProperty(configFile, "GID");
                config.Mode   = GetFileModeProperty(configFile, "Mode");

                this.Config.Add(config);
            }

            this.Isolation = GetEnumProperty<IsolationMode>(containerSpec, "Isolation");

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.Resources

            // $todo(jeff.lill):
            //
            // I'm ignoring the [Limits.GenericResources] and [Reservation.GenericResources]
            // properties right now because I suprised that there are two of these.  The command
            // line appears to support only one global combined [GenericResources] concept.

            const long oneBillion = 1000000000L;

            var resources = GetObjectProperty(taskTemplate, "Resources");
            var limits    = GetObjectProperty(resources, "Limits");
            
            var nanoCpus  = GetLongProperty(limits, "NanoCPUs");

            if (nanoCpus.HasValue)
            {
                this.LimitCpu = nanoCpus / oneBillion;
            }

            this.LimitMemory = GetLongProperty(limits, "MemoryBytes");

            var reservation = GetObjectProperty(resources, "Reservation");

            nanoCpus = GetLongProperty(reservation, "NanoCPUs");

            if (nanoCpus.HasValue)
            {
                this.ReserveCpu = nanoCpus / oneBillion;
            }

            this.ReserveMemory = GetLongProperty(reservation, "MemoryBytes");

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.RestartPolicy

            var restartPolicy = GetObjectProperty(taskTemplate, "RestartPolicy");

            this.RestartCondition   = GetEnumProperty<RestartCondition>(restartPolicy, "Condition");
            this.RestartDelay       = GetLongProperty(restartPolicy, "Delay");
            this.RestartMaxAttempts = GetLongProperty(restartPolicy, "MaxAttempts");
            this.RestartWindow      = GetLongProperty(restartPolicy, "Window");

            //-----------------------------------------------------------------
            // Spec.TaskTemplatePlacement

            // $todo(jeff.lill):
            //
            // We're going to ignore the [Preferences] and [Platforms] fields for now.

            var placement = GetObjectProperty(taskTemplate, "Placement");

            foreach (string constraint in GetArrayProperty(placement, "Constraints"))
            {
                this.Constraint.Add(constraint);
            }

            // $todo(jeff.lill): Ignoring the [Runtime] property.

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.Network

            // $todo(jeff.lill):
            //
            // Inspect reports networks are referenced by UUID, not name.  We'll 
            // use the network map passed to the method to try to associate the
            // network names.
            //
            // Note that it's possible (but unlikely) for the set of cluster networks
            // to have changed between listing them and inspecting the service, so
            // we might not be able to map a network ID to a name.
            //
            // In this case, we won't add the referenced network to this service
            // specification.  The ultimate impact will be to potentially trigger 
            // an unnecessary service update, but since this will be super rare
            // and shouldn't have any adverse impact, so I'm not going to worry
            // about it.

            foreach (JObject network in GetArrayProperty(taskTemplate, "Networks"))
            {
                if (network.TryGetValue("Target", out var networkIdToken))
                {
                    var networkId = (string)networkIdToken;

                    if (networkIdToName.TryGetValue(networkId, out var networkName))
                    {
                        this.Network.Add(networkName);
                    }
                }
            }

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.LogDriver

            var logDriver = GetObjectProperty(taskTemplate, "LogDriver");

            this.LogDriver = GetStringProperty(logDriver, "Name");

            var logOptions = GetObjectProperty(logDriver, "Options");

            foreach (var item in logOptions)
            {
                this.LogOpt.Add($"{item.Key}={item.Value}");
            }

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.Mode

            var mode       = (JObject)spec["Mode"];

            if (mode.ContainsKey("Global"))
            {
                this.Mode = ServiceMode.Global;
            }
            else if (mode.ContainsKey("Replicated"))
            {
                var replicated = GetObjectProperty(mode, "Replicated");

                this.Mode     = ServiceMode.Replicated;
                this.Replicas = GetLongProperty(replicated, "Replicas");
            }
            else
            {
                throw new NotSupportedException("Unexpected service [Spec.TaskTemplate.Mode].");
            }

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.UpdateConfig

            var updateConfig = (JObject)spec["UpdateConfig"];

            this.UpdateParallism       = GetLongProperty(updateConfig, "Parallelism");
            this.UpdateDelay           = GetLongProperty(updateConfig, "Delay");
            this.UpdateFailureAction   = GetEnumProperty<UpdateFailureAction>(updateConfig, "FailureAction");
            this.UpdateMonitor         = GetLongProperty(updateConfig, "Monitor");
            this.UpdateMaxFailureRatio = GetDoubleProperty(updateConfig, "MaxFailureRatio");
            this.UpdateOrder           = GetEnumProperty<UpdateOrder>(updateConfig, "Order");

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.RollbackConfig

            var rollbackConfig = (JObject)spec["RollbackConfig"];

            this.RollbackParallism       = GetLongProperty(rollbackConfig, "Parallelism");
            this.RollbackDelay           = GetLongProperty(rollbackConfig, "Delay");
            this.RollbackFailureAction   = GetEnumProperty<RollbackFailureAction>(rollbackConfig, "FailureAction");
            this.RollbackMonitor         = GetLongProperty(rollbackConfig, "Monitor");
            this.RollbackMaxFailureRatio = GetDoubleProperty(rollbackConfig, "MaxFailureRatio");
            this.RollbackOrder           = GetEnumProperty<RollbackOrder>(rollbackConfig, "Order");

            //-----------------------------------------------------------------
            // Spec.TaskTemplate.EndpointSpec

            var endpointSpec = (JObject)spec["EndpointSpec"];

            this.EndpointMode = GetEnumProperty<EndpointMode>(endpointSpec, "Mode");

            foreach (JObject item in GetArrayProperty(endpointSpec, "Ports"))
            {
                var port = new PublishPort();

                port.Name      = GetStringProperty(item, "Name");
                port.Protocol  = GetEnumProperty<PortProtocol>(item, "Protocol");
                port.Target    = GetIntProperty(item, "TargetPort");
                port.Published = GetIntProperty(item, "PublishedPort");
                port.Mode      = GetEnumProperty<PortMode>(item, "PublishMode");
            }
        }

        //---------------------------------------------------------------------
        // JSON helpers:

        /// <summary>
        /// Looks up a <see cref="JToken"/> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property token or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static JToken GetJTokenProperty(JObject jObject, string name)
        {
            if (jObject == null)
            {
                return null;
            }

            if (jObject.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Looks up a <c>string</c> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property string or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static string GetStringProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);

            if (jToken != null)
            {
                return (string)jToken;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Looks up a <c>bool</c> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property boolean or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static bool? GetBoolProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);

            if (jToken != null)
            {
                switch (jToken.Type)
                {
                    case JTokenType.Boolean:

                        return (bool)jToken;

                    case JTokenType.Integer:

                        return (long)jToken != 0;

                    case JTokenType.Float:

                        return (double)jToken != 0.0;

                    case JTokenType.String:

                        switch (((string)jToken).ToLowerInvariant())
                        {
                            case "0":
                            case "false":
                            case "off":
                            case "no":

                                return false;

                            case "1":
                            case "true":
                            case "on":
                            case "yes":

                                return true;

                            default:

                                return false;
                        }

                    case JTokenType.None:
                    case JTokenType.Null:

                        return false;

                    default:

                        return false;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Looks up an <c>int</c> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property int or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static int? GetIntProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);

            if (jToken == null)
            {
                return null;
            }

            switch (jToken.Type)
            {
                case JTokenType.Integer:

                    return (int)jToken;

                case JTokenType.String:

                    if (int.TryParse((string)jToken, out var value))
                    {
                        return value;
                    }
                    else
                    {
                        return null;
                    }

                default:

                    return null;
            }
        }

        /// <summary>
        /// Looks up a <c>long</c> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property int or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static long? GetLongProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);

            if (jToken == null)
            {
                return null;
            }

            switch (jToken.Type)
            {
                case JTokenType.Integer:

                    return (long)jToken;

                case JTokenType.String:

                    if (long.TryParse((string)jToken, out var value))
                    {
                        return value;
                    }
                    else
                    {
                        return null;
                    }

                default:

                    return null;
            }
        }

        /// <summary>
        /// Looks up a <c>double</c> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property int or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static double? GetDoubleProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);

            if (jToken == null)
            {
                return null;
            }

            switch (jToken.Type)
            {
                case JTokenType.Float:
                case JTokenType.Integer:

                    return (double)jToken;

                case JTokenType.String:

                    if (double.TryParse((string)jToken, out var value))
                    {
                        return value;
                    }
                    else
                    {
                        return null;
                    }

                default:

                    return null;
            }
        }

        /// <summary>
        /// Looks up an <c>int</c> property converting it to an octal string
        /// or returning <c>null</c> if the property doesn't exist.  This is
        /// handy for parsing Linux style file modes from the decimal integers
        /// Docker reports.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property value or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static string GetFileModeProperty(JObject jObject, string name)
        {
            var decimalMode = GetIntProperty(jObject, name);

            if (decimalMode.HasValue)
            {
                return Convert.ToString(decimalMode.Value, 8);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Looks up an <c>enum</c> property, returning <c>null</c> if the
        /// property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property int or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static TEnum? GetEnumProperty<TEnum>(JObject jObject, string name)
            where TEnum : struct
        {
            var value = GetStringProperty(jObject, name);

            if (value != null)
            {
                return Enum.Parse<TEnum>(value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Looks up a <see cref="JObject"/> property value returning an
        /// empty object if the property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property object or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static JObject GetObjectProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);
            var value  = jToken as JObject;

            if (value != null)
            {
                return value;
            }
            else
            {
                return new JObject();
            }
        }

        /// <summary>
        /// Looks up a <see cref="JArray"/> property value returning an
        /// empty array if the property doesn't exist.
        /// </summary>
        /// <param name="jObject">The parent object or <c>null</c>.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The property array or <c>null</c> if the property doesn't exist or
        /// if <see cref="jObject"/> is <c>null</c>.
        /// </returns>
        private static JArray GetArrayProperty(JObject jObject, string name)
        {
            var jToken = GetJTokenProperty(jObject, name);
            var value  = jToken as JArray;

            if (value != null)
            {
                return value;
            }
            else
            {
                return new JArray();
            }
        }
    }
}