﻿//-----------------------------------------------------------------------------
// FILE:	    Test_ServiceInspect.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Consul;

using Neon.Common;
using Neon.Cluster;
using Neon.Cryptography;
using Neon.Docker;
using Neon.IO;
using Neon.Xunit;
using Neon.Xunit.Cluster;

using Xunit;

namespace TestNeonCluster
{
    /// <summary>
    /// Verifies that the <see cref="ServiceDetails"/> class maps correctly to
    /// the service inspection details returned for actual Docker services.
    /// </summary>
    public class Test_ServiceInspect : IClassFixture<DockerFixture>
    {
        // Enable strict JSON parsing by default so that unit tests will be
        // able to detect misnamed properties and also be able to discover 
        // new service properties added to the REST API by Docker.

        private const bool strict = true;

        private DockerFixture docker;

        public Test_ServiceInspect(DockerFixture docker)
        {
            this.docker = docker;

            // We're passing [login=null] below to connect to the cluster specified
            // by the NEON_TEST_CLUSTER environment variable.  This needs to be 
            // initialized with the login for a deployed cluster.

            if (this.docker.Initialize())
            {
                // Initialize the service with some secrets, configs, and networks
                // we can reference from services in our tests.

                docker.Reset();

                docker.CreateSecret("secret-1", "password1");
                docker.CreateSecret("secret-2", "password2");

                docker.CreateConfig("config-1", "config1");
                docker.CreateConfig("config-2", "config2");

                docker.CreateNetwork("network-1");
                docker.CreateNetwork("network-2");
            }
            else
            {
                docker.ClearServices();
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Simple()
        {
            // Deploy a very simple service and then verify that the
            // service details were parsed correctly.

            docker.CreateService("test", "neoncluster/test");

            var info = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            // ID, Version, and Time fields

            Assert.Equal(info.ID, details.ID.Substring(0, 12));     // ListServices returns the 12 character short ID
            Assert.True(details.Version.Index > 0);

            Assert.Equal(details.CreatedAtUtc, details.UpdatedAtUtc);

            var minTime = DateTime.UtcNow - TimeSpan.FromMinutes(10);
            var maxTime = DateTime.UtcNow;

            Assert.True(minTime <= details.CreatedAtUtc);
            Assert.True(details.CreatedAtUtc < maxTime);

            Assert.True(minTime <= details.UpdatedAtUtc);
            Assert.True(details.UpdatedAtUtc < maxTime);

            // Spec.TaskTemplate.ContainerSpec

            Assert.Equal("neoncluster/test:latest", details.Spec.TaskTemplate.ContainerSpec.ImageWithoutSHA);
            Assert.Equal(10000000000L, details.Spec.TaskTemplate.ContainerSpec.StopGracePeriod);
            Assert.Equal(ServiceIsolationMode.Default, details.Spec.TaskTemplate.ContainerSpec.Isolation);

            Assert.Empty(details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Test);
            Assert.Equal(0L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Interval);
            Assert.Equal(0L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Timeout);
            Assert.Equal(0L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Retries);
            Assert.Equal(0L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.StartPeriod);

            // Spec.TaskTemplate.Resources

            Assert.Equal(0, details.Spec.TaskTemplate.Resources.Limits.NanoCPUs);
            Assert.Equal(0, details.Spec.TaskTemplate.Resources.Limits.MemoryBytes);
            Assert.Empty(details.Spec.TaskTemplate.Resources.Limits.GenericResources);

            Assert.Equal(0, details.Spec.TaskTemplate.Resources.Reservations.NanoCPUs);
            Assert.Equal(0, details.Spec.TaskTemplate.Resources.Reservations.MemoryBytes);
            Assert.Empty(details.Spec.TaskTemplate.Resources.Reservations.GenericResources);

            // Spec.TaskTemplate.RestartPolicy

            Assert.Equal(ServiceRestartCondition.Any, details.Spec.TaskTemplate.RestartPolicy.Condition);
            Assert.Equal(5000000000L, details.Spec.TaskTemplate.RestartPolicy.Delay);
            Assert.Equal(0L, details.Spec.TaskTemplate.RestartPolicy.MaxAttempts);

            // Spec.TaskTemplate.Placement

            Assert.Single(details.Spec.TaskTemplate.Placement.Platforms);
            Assert.Equal("amd64", details.Spec.TaskTemplate.Placement.Platforms[0].Architecture);
            Assert.Equal("linux", details.Spec.TaskTemplate.Placement.Platforms[0].OS);

            // Spec.TaskTemplate (misc)

            Assert.Equal(0, details.Spec.TaskTemplate.ForceUpdate);
            Assert.Equal("container", details.Spec.TaskTemplate.Runtime);

            // Spec.Mode

            Assert.Null(details.Spec.Mode.Global);
            Assert.NotNull(details.Spec.Mode.Replicated);
            Assert.Equal(1, details.Spec.Mode.Replicated.Replicas);

            Assert.Null(details.Spec.Mode.Global);

            // Spec.UpdateConfig

            Assert.Equal(1, details.Spec.UpdateConfig.Parallelism);
            Assert.Equal(ServiceUpdateFailureAction.Pause, details.Spec.UpdateConfig.FailureAction);
            Assert.Equal(5000000000L, details.Spec.UpdateConfig.Monitor);
            Assert.Equal(0.0, details.Spec.UpdateConfig.MaxFailureRatio);
            Assert.Equal(ServiceUpdateOrder.StopFirst, details.Spec.UpdateConfig.Order);

            // Spec.RollbackConfig

            Assert.Equal(1, details.Spec.RollbackConfig.Parallelism);
            Assert.Equal(ServiceRollbackFailureAction.Pause, details.Spec.RollbackConfig.FailureAction);
            Assert.Equal(5000000000L, details.Spec.RollbackConfig.Monitor);
            Assert.Equal(0.0, details.Spec.RollbackConfig.MaxFailureRatio);
            Assert.Equal(ServiceRollbackOrder.StopFirst, details.Spec.RollbackConfig.Order);

            // Spec.EndpointSpec

            Assert.Equal(ServiceEndpointMode.Vip, details.Spec.TaskTemplate.EndpointSpec.Mode);
            Assert.Empty(details.Spec.EndpointSpec.Ports);

            // Endpoint

            Assert.Empty(details.Endpoint.Ports);
            Assert.Empty(details.Endpoint.VirtualIPs);

            // UpdateStatus

            Assert.Null(details.UpdateStatus);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Labels()
        {
            // Verify that we can deploy and parse service labels.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs: 
                    new string[]
                    {
                        "--label", "foo=bar",
                        "--label", "hello=world"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal(2, details.Spec.Labels.Count);
            Assert.Equal("bar", details.Spec.Labels["foo"]);
            Assert.Equal("world", details.Spec.Labels["hello"]);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Env()
        {
            // Verify that we can specify environment variables.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--env", "foo=bar",
                        "--env", "hello=world",
                        "--env", "MAIL"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal(3, details.Spec.TaskTemplate.ContainerSpec.Env.Count);
            Assert.Contains("foo=bar", details.Spec.TaskTemplate.ContainerSpec.Env);
            Assert.Contains("hello=world", details.Spec.TaskTemplate.ContainerSpec.Env);
            Assert.Contains("MAIL", details.Spec.TaskTemplate.ContainerSpec.Env);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void DNSConfig()
        {
            // Verify that we can specify DNS configuration.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--dns", "8.8.8.8",
                        "--dns-search", "foo.com",
                        "--dns-option", "timeout:2"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal(new string[] { "8.8.8.8" }, details.Spec.TaskTemplate.ContainerSpec.DNSConfig.Nameservers);
            Assert.Equal(new string[] { "foo.com" }, details.Spec.TaskTemplate.ContainerSpec.DNSConfig.Search);
            Assert.Equal(new string[] { "timeout:2" }, details.Spec.TaskTemplate.ContainerSpec.DNSConfig.Options);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Command()
        {
            // Verify that we can specify the container command and arguments.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--entrypoint", "sleep"
                    },
                serviceArgs:
                    new string[]
                    {
                        "50000000"
                    });

            var info = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal(new string[] { "sleep" }, details.Spec.TaskTemplate.ContainerSpec.Command);
            Assert.Equal(new string[] { "50000000" }, details.Spec.TaskTemplate.ContainerSpec.Args);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Misc()
        {
            // Verify that we can specify misc container properties.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--hostname", "sleeper",
                        "--workdir", "/",
                        "--user", "test",
                        "--group", "test",
                        "--tty",
                        "--read-only",
                        "--stop-signal", "kill",
                        "--stop-grace-period", "20000000ns",
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal("sleeper", details.Spec.TaskTemplate.ContainerSpec.Hostname);
            Assert.Equal("/", details.Spec.TaskTemplate.ContainerSpec.Dir);
            Assert.Equal("test", details.Spec.TaskTemplate.ContainerSpec.User);
            Assert.Equal("test", details.Spec.TaskTemplate.ContainerSpec.Groups.Single());
            Assert.True(details.Spec.TaskTemplate.ContainerSpec.ReadOnly);
            Assert.True(details.Spec.TaskTemplate.ContainerSpec.ReadOnly);
            Assert.Equal("kill", details.Spec.TaskTemplate.ContainerSpec.StopSignal);
            Assert.Equal(20000000L, details.Spec.TaskTemplate.ContainerSpec.StopGracePeriod);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Health()
        {
            // Verify that we can customize health checks.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--health-cmd", "echo ok",
                        "--health-interval", "250000000ns",
                        "--health-retries", "3",
                        "--health-start-period", "350000000ns",
                        "--health-timeout", "450000000ns"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal(new string[] { "CMD-SHELL", "echo ok" }, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Test);
            Assert.Equal(250000000L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Interval);
            Assert.Equal(3L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Retries);
            Assert.Equal(350000000L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.StartPeriod);
            Assert.Equal(450000000L, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Timeout);

            // ..and that we can disable a check entirely.

            docker.ClearServices();

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--no-healthcheck"
                    });

            info    = docker.ListServices().Single(s => s.Name == "test");
            details = docker.InspectService("test", strict);

            Assert.Equal(new string[] { "NONE" }, details.Spec.TaskTemplate.ContainerSpec.HealthCheck.Test);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Secrets()
        {
            // Verify that we can specify service secrets.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--secret", $"source=secret-1,target=secret,uid={TestHelper.TestUID},gid={TestHelper.TestGID},mode=0444",
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Single(details.Spec.TaskTemplate.ContainerSpec.Secrets);

            var secret = details.Spec.TaskTemplate.ContainerSpec.Secrets.Single();

            Assert.NotEmpty(secret.SecretID);
            Assert.Equal("secret-1", secret.SecretName);
            Assert.Equal("secret", secret.File.Name);
            Assert.Equal(TestHelper.TestUID, secret.File.UID);
            Assert.Equal(TestHelper.TestGID, secret.File.GID);
            Assert.Equal(Convert.ToInt32("444", 8), secret.File.Mode);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Configs()
        {
            // Verify that we can specify service configs.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--config", $"source=config-1,target=/my-config,uid={TestHelper.TestUID},gid={TestHelper.TestGID},mode=0444",
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Single(details.Spec.TaskTemplate.ContainerSpec.Configs);

            var config = details.Spec.TaskTemplate.ContainerSpec.Configs.Single();

            Assert.NotEmpty(config.ConfigID);
            Assert.Equal("config-1", config.ConfigName);
            Assert.Equal("/my-config", config.File.Name);
            Assert.Equal(TestHelper.TestUID, config.File.UID);
            Assert.Equal(TestHelper.TestGID, config.File.GID);
            Assert.Equal(Convert.ToInt32("444", 8), config.File.Mode);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Global()
        {
            // Deploy a service in GLOBAL (not replicated mode) and verify.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs: 
                new string[]
                    {
                        "--mode", "global"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.NotNull(details.Spec.Mode.Global);
            Assert.Null(details.Spec.Mode.Replicated);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void DNSRR()
        {
            // Deploy a service in DNSRR mode and verify.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                new string[]
                    {
                        "--endpoint-mode", "dnsrr"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            Assert.Equal(ServiceEndpointMode.DnsRR, details.Spec.TaskTemplate.EndpointSpec.Mode);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Ports()
        {
            // Deploy a service with port mappings and verify.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                new string[]
                    {
                        "--publish", "published=8000,target=80,mode=ingress,protocol=tcp",
                        "--publish", "published=8001,target=81,mode=host,protocol=udp"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            // We should see one virtual network CIDR assigned.

            Assert.Single(details.Endpoint.VirtualIPs);

            // Verify the service endpoints.

            Assert.Equal(2, details.Endpoint.Ports.Count);

            var port80 = details.Endpoint.Ports.Single(p => p.TargetPort == 80);
            var port81 = details.Endpoint.Ports.Single(p => p.TargetPort == 81);

            Assert.Equal(8000, port80.PublishedPort);
            Assert.Equal(80, port80.TargetPort);
            Assert.Equal(ServicePortMode.Ingress, port80.PublishMode);
            Assert.Equal(ServicePortProtocol.Tcp, port80.Protocol);

            Assert.Equal(8001, port81.PublishedPort);
            Assert.Equal(81, port81.TargetPort);
            Assert.Equal(ServicePortMode.Host, port81.PublishMode);
            Assert.Equal(ServicePortProtocol.Udp, port81.Protocol);

            // Verify the endpoint specifications too.

            var port80Spec = details.Endpoint.Spec.Ports.Single(p => p.TargetPort == 80);
            var port81Spec = details.Endpoint.Spec.Ports.Single(p => p.TargetPort == 81);

            Assert.Equal(8000, port80Spec.PublishedPort);
            Assert.Equal(80, port80Spec.TargetPort);
            Assert.Equal(ServicePortMode.Ingress, port80Spec.PublishMode);
            Assert.Equal(ServicePortProtocol.Tcp, port80Spec.Protocol);

            Assert.Equal(8001, port81Spec.PublishedPort);
            Assert.Equal(81, port81Spec.TargetPort);
            Assert.Equal(ServicePortMode.Host, port81Spec.PublishMode);
            Assert.Equal(ServicePortProtocol.Udp, port81Spec.Protocol);
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Mounts()
        {
            // Deploy a service with various mounts and verify.

            using (var tempFolder = new TempFolder())
            {
                //-------------------------------------------------------------
                // Volume mount (note that we're not verifying some properties 
                // like volume drivers, labels, etc.):

                docker.CreateService("test", "neoncluster/test",
                    dockerArgs:
                    new string[]
                        {
                            "--mount", $"type=volume,source=test-volume-a,target=/mount,readonly=false"
                        });

                var info    = docker.ListServices().Single(s => s.Name == "test");
                var details = docker.InspectService("test", strict);
                var mount   = details.Spec.TaskTemplate.ContainerSpec.Mounts.Single();

                Assert.Equal(ServiceMountType.Volume, mount.Type);
                Assert.Equal("test-volume-a", mount.Source);
                Assert.Equal("/mount", mount.Target);
                Assert.False(mount.ReadOnly);
                Assert.Null(mount.VolumeOptions);

                // Verify: readonly=true

                docker.ClearServices();

                docker.CreateService("test", "neoncluster/test",
                    dockerArgs:
                    new string[]
                        {
                            "--mount", $"type=volume,source=test-volume-b,target=/mount,readonly=true"
                        });

                info    = docker.ListServices().Single(s => s.Name == "test");
                details = docker.InspectService("test", strict);
                mount   = details.Spec.TaskTemplate.ContainerSpec.Mounts.Single();

                Assert.Equal(ServiceMountType.Volume, mount.Type);
                Assert.Equal("test-volume-b", mount.Source);
                Assert.Equal("/mount", mount.Target);
                Assert.True(mount.ReadOnly);
                Assert.Null(mount.VolumeOptions);

                //-------------------------------------------------------------
                // Bind mount:

                docker.ClearServices();

                // Docker doesn't like backslashes so convert them to forward slashes.

                var tempPath = tempFolder.Path.Replace("\\", "/");

                docker.CreateService("test", "neoncluster/test",
                    dockerArgs:
                    new string[]
                        {
                            "--mount", $"type=bind,source={tempPath},target=/mount,readonly=false"
                        });

                info    = docker.ListServices().Single(s => s.Name == "test");
                details = docker.InspectService("test", strict);
                mount   = details.Spec.TaskTemplate.ContainerSpec.Mounts.Single();

                if (NeonHelper.IsWindows)
                {
                    // Docker reports the mount source as [/host_mnt/DRIVE/...]
                    // on Windows, so we'll adjust the expected mount source.

                    tempPath = $"/host_mnt/{char.ToLowerInvariant(tempPath[0])}/{Path.GetFullPath(tempPath).Substring(3).Replace("\\", "/")}";
                }

                Assert.Equal(ServiceMountType.Bind, mount.Type);
                Assert.Equal(tempPath, mount.Source);
                Assert.Equal("/mount", mount.Target);
                Assert.False(mount.ReadOnly);
                Assert.Null(mount.VolumeOptions);

                //-------------------------------------------------------------
                // Tmpfs mount:

                docker.ClearServices();

                docker.CreateService("test", "neoncluster/test",
                    dockerArgs:
                    new string[]
                        {
                            "--mount", $"type=tmpfs,target=/mount,tmpfs-size=32000000,tmpfs-mode=777"
                        });

                info    = docker.ListServices().Single(s => s.Name == "test");
                details = docker.InspectService("test", strict);
                mount   = details.Spec.TaskTemplate.ContainerSpec.Mounts.Single();

                Assert.Equal(ServiceMountType.Tmpfs, mount.Type);
                Assert.Equal("/mount", mount.Target);
                Assert.False(mount.ReadOnly);
                Assert.Null(mount.VolumeOptions);
                Assert.NotNull(mount.TmpfsOptions);

                var tmpfsOptions = mount.TmpfsOptions;

                Assert.Equal(32000000L, tmpfsOptions.SizeBytes);
                Assert.Equal(777, tmpfsOptions.Mode);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Rollback()
        {
            // $note(jeff.lill):
            //
            // [docker service inspect SERVICE] doesn't appear to return
            // the service update status so we're going to comment our
            // those tests below.

            // Deploy a simple test service.

            docker.CreateService("test", "neoncluster/test",
                dockerArgs:
                    new string[]
                    {
                        "--replicas", "1"
                    });

            var info    = docker.ListServices().Single(s => s.Name == "test");
            var details = docker.InspectService("test", strict);

            //Assert.Equal(ServiceUpdateState.Completed, details.UpdateStatus.State);
            Assert.Equal(1, details.Spec.Mode.Replicated.Replicas);
            Assert.Null(details.PreviousSpec);      // We just started the service so verify that
                                                    // that there's no previous state.

            // Update the service to have 2 replicas and verify that both
            // the current and previous specifications are correct.

            docker.UpdateService("test",
                dockerArgs:
                    new string[]
                    {
                        "--replicas", "2"
                    });

            info    = docker.ListServices().Single(s => s.Name == "test");
            details = docker.InspectService("test", strict);

            //Assert.Equal(ServiceUpdateState.Completed, details.UpdateStatus.State);
            Assert.Equal(2, details.Spec.Mode.Replicated.Replicas);
            Assert.Equal(1, details.PreviousSpec.Mode.Replicated.Replicas);

            // Now rollback the service back and verify.

            docker.RollbackService("test");

            info    = docker.ListServices().Single(s => s.Name == "test");
            details = docker.InspectService("test", strict);

            //Assert.Equal(ServiceUpdateState.RollbackCompleted, details.UpdateStatus.State);
            Assert.Equal(1, details.Spec.Mode.Replicated.Replicas);

            // It's a bit weird that Docker doesn't reset the previous spec
            // back to NULL when we rolled back to the initial state.
            // We'll ensure that at least it holds the correct initial
            // state.

            Assert.NotNull(details.PreviousSpec);
            Assert.Equal(1, details.Spec.Mode.Replicated.Replicas);
        }

        [Fact(Skip = "Not supporting SELinux services yet.")]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void SELinux()
        {
            // $todo(jeff.lill): Not supporting SELinux yet.
        }

        [Fact(Skip = "Not supporting Windows services yet.")]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCluster)]
        public void Windows()
        {
            // $todo(jeff.lill):
            //
            // There are some Windows specific features to check:
            //
            //      1. details.Spec.TaskTemplate.ContainerSpec.Isolation
            //      2. details.Spec.TaskTemplate.ContainerSpec.Privileges.CredentialSpec
        }
    }
}