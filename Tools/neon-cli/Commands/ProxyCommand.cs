﻿//-----------------------------------------------------------------------------
// FILE:	    ProxyCommand.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Consul;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft;
using Newtonsoft.Json;

using Neon.Cluster;
using Neon.Common;
using Neon.Cryptography;

namespace NeonCli
{
    /// <summary>
    /// Implements the <b>proxy</b> command.
    /// </summary>
    public class ProxyCommand : CommandBase
    {
        private const string proxyManagerPrefix = "neon/service/neon-proxy-manager";
        private const string vaultCertPrefix    = "neon-secret/cert";

        private const string usage = @"
Manages the cluster's public and private proxies.

USAGE:

    neon proxy help
    neon proxy NAME build
    neon proxy NAME get [--yaml] ROUTE
    neon proxy NAME inspect
    neon proxy NAME list|ls
    neon proxy NAME remove|rm ROUTE
    neon proxy NAME put FILE
    neon proxy NAME put -
    neon proxy NAME settings FILE
    neon proxy NAME settings -
    neon proxy NAME status

ARGUMENTS:

    NAME    - Proxy name: [public] or [private].
    ROUTE   - Route name.
    FILE    - Path to a JSON file.
    -       - Indicates that JSON/YAML is read from standard input.

COMMANDS:

    help            - Prints proxy route details.
    build           - Forces the proxy manager to build the 
                      proxy configuration.
    get             - Output a specific route as JSON by default.
                      Use [--yaml] to return as YAML.
    haproxy         - Outputs the HAProxy configuration.
    inspect         - Displays JSON details for all proxy routes
                      and settings.
    list|ls         - Lists the route names.
    remove|rm       - Removes a named route.
    put             - Adds or updates a route from a file or by
                      reading standard input.  JSON or YAML
                      input is supported.
    settings        - Updates the proxy global settings from a
                      JSON file or by reading standard input.
    status          - Displays the current status for a proxy.
";

        private const string routeHelp =
@"
neonCLUSTER proxies support two types of routes: HTTP/S and TCP.
Each route defines one or more frontend and backends.

HTTP/S frontends handle requests for a hostname for one or more hostname
and port combinations.  HTTPS is enabled by specifying the name of a
certificate loaded into the cluster.  The port defaults to 80 for HTTP
and 443 for HTTPS.   The [https_redirect] option indicates that clients
making HTTP requests should be redirected with the HTTPS scheme.  HTTP/S
routes for the PUBLIC proxy are exposed on the Internet facing load balancer
by default on the standard ports 80/443.  It is possible to change
these public ports or disable exposure of individual routes.

TCP frontends simply specify one of the TCP ports assigned to the proxy
(note that the first two ports are reserved for HTTP and HTTPS).  TCP
routes for the PUBLIC proxy may also be exposed on the Internet facing
load balancer by setting the public port property.

Backends specify one or more target servers by IP address or DNS name
and port number.

Routes are specified using JSON or YAML.  Here's an example HTTP/S route that
accepts HTTP traffic for [foo.com] and [www.foo.com] and redirects it to
HTTPS and then also accepts HTTPS traffic using the [foo.com] certificate.
Traffic is routed to the [foo_service] on port 80 which could be a Docker
swarm mode service or DNS name.

    {
        ""Name"": ""my-http-route"",
        ""Mode"": ""http"",
        ""HttpsRedirect"": true,
        ""Frontends"": [
            { ""Host"": ""foo.com"" },
            { ""Host"": ""www.foo.com"" },
            { ""Host"": ""foo.com"", ""CertName"": ""foo.com"" },
            { ""Host"": ""www.foo.com"", ""CertName"": ""foo.com"" }
        ],
        ""Backends"": [
            { ""Server"": ""foo_service"", ""Port"": 80 }
        ]
    }

Here's an example public TCP route that forwards TCP connections to
port 1000 on the cluster's Internet-facing load balancer to the internal
HAProxy server listening on Docker ingress port 5305 port which then
load balances the traffic to the backend servers listening on port 1000:

    {
        ""Name"": ""my-tcp-route"",
        ""Mode"": ""tcp"",
        ""Frontends"": [
            { ""PublicPort"": 1000, ""ProxyPort"": 5305 }
        ],
        ""Backends"": [
            { ""Server"": ""10.0.1.40"", ""Port"": 1000 },
            { ""Server"": ""10.0.1.41"", ""Port"": 1000 },
            { ""Server"": ""10.0.1.42"", ""Port"": 1000 }
        ]
    }

Here's how this route looks as YAML:

    Name: my-tcp-route
    Mode: tcp
    Frontends:
    - PublicPort: 1000
      ProxyPort: 5305
    Backends:
    - Server: 10.0.1.40
      Port: 1000
    - Server: 10.0.1.41
      Port: 1000
    - Server: 10.0.1.42
      Port:1000

See the documentation for more proxy route and setting details.
";
        /// <inheritdoc/>
        public override string[] Words
        {
            get { return new string[] { "proxy" }; }
        }

        /// <inheritdoc/>
        public override string[] ExtendedOptions
        {
            get { return new string[] { "--yaml" }; }
        }

        /// <inheritdoc/>
        public override void Help()
        {
            Console.WriteLine(usage);
        }

        /// <inheritdoc/>
        public override void Run(CommandLine commandLine)
        {
            if (commandLine.HasHelpOption || commandLine.Arguments.Length == 0)
            {
                Console.WriteLine(usage);
                Program.Exit(0);
            }

            Program.ConnectCluster();

            // Process the command arguments.

            var proxyManager = (ProxyManager)null;
            var yaml         = commandLine.HasOption("--yaml");

            var proxyName = commandLine.Arguments.FirstOrDefault();

            switch (proxyName)
            {
                case "help":

                    // $hack: So this isn't really a proxy name.

                    Console.WriteLine(routeHelp);
                    Program.Exit(0);
                    break;

                case "public":

                    proxyManager = NeonClusterHelper.Cluster.PublicProxy;
                    break;

                case "private":

                    proxyManager = NeonClusterHelper.Cluster.PrivateProxy;
                    break;

                default:

                    Console.WriteLine($"*** ERROR: Proxy name must be one of [public] or [private] ([{proxyName}] is not valid).");
                    Program.Exit(1);
                    break;
            }

            commandLine = commandLine.Shift(1);

            var command = commandLine.Arguments.FirstOrDefault();

            if (command == null)
            {
                Console.WriteLine(usage);
                Program.Exit(1);
            }

            commandLine = commandLine.Shift(1);

            string routeName;

            switch (command)
            {
                case "get":

                    routeName = commandLine.Arguments.FirstOrDefault();

                    if (string.IsNullOrEmpty(routeName))
                    {
                        Console.Error.WriteLine("*** ERROR: [ROUTE] argument expected.");
                        Program.Exit(1);
                    }

                    if (!ClusterDefinition.IsValidName(routeName))
                    {
                        Console.WriteLine($"*** ERROR: [{routeName}] is not a valid route name.");
                        Program.Exit(1);
                    }

                    // Fetch a specific proxy route and output it.

                    var route = proxyManager.GetRoute(routeName);

                    if (route == null)
                    {
                        Console.WriteLine($"*** ERROR: Proxy [{proxyName}] route [{routeName}] does not exist.");
                        Program.Exit(1);
                    }

                    Console.WriteLine(yaml ? route.ToYaml() : route.ToJson());
                    break;

                case "haproxy":

                    // We're going to download the proxy's ZIP archive containing the [haproxy.cfg]
                    // file, extract the file, and write it to the console.

                    using (var consul = NeonClusterHelper.OpenConsul())
                    {
                        var confKey = $"neon/service/neon-proxy-manager/proxies/{proxyName}/conf";

                        try
                        {
                            var confZipBytes = consul.KV.GetBytes(confKey).Result;

                            using (var msZipData = new MemoryStream(confZipBytes))
                            {
                                using (var zip = new ZipFile(msZipData))
                                {
                                    var entry = zip.GetEntry("haproxy.cfg");

                                    if (entry == null || !entry.IsFile)
                                    {
                                        Console.WriteLine($"*** ERROR: HAProxy ZIP configuration in Consul at [{confKey}] appears to be corrupt.  Cannot locate the [haproxy.cfg] entry.");
                                        Program.Exit(1);
                                    }

                                    using (var entryStream = zip.GetInputStream(entry))
                                    {
                                        using (var reader = new StreamReader(entryStream))
                                        {
                                            foreach (var line in reader.Lines())
                                            {
                                                Console.WriteLine(line);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            Console.WriteLine($"*** ERROR: HAProxy ZIP configuration was not found in Consul at [{confKey}].");
                            Program.Exit(1);
                        }
                    }
                    break;

                case "inspect":

                    Console.WriteLine(NeonHelper.JsonSerialize(proxyManager.GetDefinition(), Formatting.Indented));
                    break;

                case "list":
                case "ls":

                    var nameList = proxyManager.ListRoutes().ToArray();

                    if (nameList.Length == 0)
                    {
                        Console.WriteLine("* No routes");
                    }
                    else
                    {
                        foreach (var name in proxyManager.ListRoutes())
                        {
                            Console.WriteLine(name);
                        }
                    }
                    break;

                case "build":

                    proxyManager.Build();
                    break;

                case "remove":
                case "rm":

                    routeName = commandLine.Arguments.FirstOrDefault();

                    if (string.IsNullOrEmpty(routeName))
                    {
                        Console.Error.WriteLine("*** ERROR: [ROUTE] argument expected.");
                        Program.Exit(1);
                    }

                    if (!ClusterDefinition.IsValidName(routeName))
                    {
                        Console.WriteLine($"*** ERROR: [{routeName}] is not a valid route name.");
                        Program.Exit(1);
                    }

                    if (proxyManager.RemoveRoute(routeName))
                    {
                        Console.WriteLine($"Deleted proxy [{proxyName}] route [{routeName}].");
                    }
                    else
                    {
                        Console.WriteLine($"*** ERROR: Proxy [{proxyName}] route [{routeName}] does not exist.");
                        Program.Exit(1);
                    }
                    break;

                case "put":

                    // $todo(jeff.lill):
                    //
                    // It would be really nice to download the existing routes and verify that
                    // adding the new route won't cause conflicts.  Currently errors will be
                    // detected only by the [neon-proxy-manager] which will log them and cease
                    // updating the cluster until the errors are corrected.

                    if (commandLine.Arguments.Length != 1)
                    {
                        Console.Error.WriteLine("*** ERROR: FILE or [-] argument expected.");
                        Program.Exit(1);
                    }

                    // Load the route.  Note that we support reading routes as JSON or
                    // YAML, automatcially detecting thew format.  We always persist
                    // routes as JSON though.

                    var routeFile = commandLine.Arguments[0];

                    string routeText;

                    if (routeFile == "-")
                    {
                        using (var input = Console.OpenStandardInput())
                        {
                            using (var reader = new StreamReader(input, detectEncodingFromByteOrderMarks: true))
                            {
                                routeText = reader.ReadToEnd();
                            }
                        }
                    }
                    else
                    {
                        routeText = File.ReadAllText(routeFile);
                    }

                    var proxyRoute = ProxyRoute.Parse(routeText, strict: true);

                    routeName = proxyRoute.Name;

                    if (!ClusterDefinition.IsValidName(routeName))
                    {
                        Console.WriteLine($"*** ERROR: [{routeName}] is not a valid route name.");
                        Program.Exit(1);
                    }

                    if (proxyManager.PutRoute(proxyRoute))
                    {
                        Console.WriteLine($"Proxy [{proxyName}] route [{routeName}] has been updated.");
                    }
                    else
                    {
                        Console.WriteLine($"Proxy [{proxyName}] route [{routeName}] has been added.");
                    }
                    break;

                case "settings":

                    var settingsFile = commandLine.Arguments.FirstOrDefault();

                    if (string.IsNullOrEmpty(settingsFile))
                    {
                        Console.Error.WriteLine("*** ERROR: [-] or FILE argument expected.");
                        Program.Exit(1);
                    }

                    string settingsText;

                    if (settingsFile == "-")
                    {
                        settingsText = NeonHelper.ReadStandardInputText();
                    }
                    else
                    {
                        settingsText = File.ReadAllText(settingsFile);
                    }

                    var proxySettings = ProxySettings.Parse(settingsText, strict: true);

                    proxyManager.UpdateSettings(proxySettings);
                    Console.WriteLine($"Proxy [{proxyName}] settings have been updated.");
                    break;

                case "status":

                    using (var consul = NeonClusterHelper.OpenConsul())
                    {
                        try
                        {
                            var statusJson  = consul.KV.GetString($"neon/service/neon-proxy-manager/status/{proxyName}").Result;
                            var proxyStatus = NeonHelper.JsonDeserialize<ProxyStatus>(statusJson);

                            Console.WriteLine();
                            Console.WriteLine($"Snapshot Time: {proxyStatus.TimestampUtc} (UTC)");
                            Console.WriteLine();

                            using (var reader = new StringReader(proxyStatus.Status))
                            {
                                foreach (var line in reader.Lines())
                                {
                                    Console.WriteLine(line);
                                }
                            }
                        }
                        catch (KeyNotFoundException)
                        {
                            Console.WriteLine($"*** ERROR: Status for proxy [{proxyName}] is not currently available.");
                            Program.Exit(1);
                        }
                    }
                    break;

                default:

                    Console.Error.WriteLine($"*** ERROR: Unknown [{command}] subcommand.");
                    Program.Exit(1);
                    break;
            }
        }

        /// <inheritdoc/>
        public override DockerShimInfo Shim(DockerShim shim)
        {
            var commandLine = shim.CommandLine;

            if (commandLine.Arguments.LastOrDefault() == "-")
            {
                shim.AddStdin(text: true);
            }
            else if (commandLine.Arguments.Length == 4)
            {
                switch (commandLine.Arguments[2])
                {
                    case "put":
                    case "settings":

                        shim.AddFile(commandLine.Arguments[3]);
                        break;
                }
            }

            return new DockerShimInfo(isShimmed: true, ensureConnection: true);
        }
    }
}
