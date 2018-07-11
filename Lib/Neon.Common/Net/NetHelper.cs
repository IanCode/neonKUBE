﻿//-----------------------------------------------------------------------------
// FILE:	    NetHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Retry;

namespace Neon.Net
{
    /// <summary>
    /// Useful network related utilities.
    /// </summary>
    public static class NetHelper
    {
        /// <summary>
        /// Determines whether two IP addresses are equal.
        /// </summary>
        /// <param name="address1">Address 1.</param>
        /// <param name="address2">Address 2.</param>
        /// <returns><c>true</c> if the addresses are equal.</returns>
        public static bool AddressEquals(IPAddress address1, IPAddress address2)
        {
            if (address1.AddressFamily != address2.AddressFamily)
            {
                return false;
            }

            var bytes1 = address1.GetAddressBytes();
            var bytes2 = address2.GetAddressBytes();

            if (bytes1.Length != bytes2.Length)
            {
                return false;
            }

            for (int i = 0; i < bytes1.Length; i++)
            {
                if (bytes1[i] != bytes2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Increments an IPv4 address by adding an integer value.
        /// </summary>
        /// <param name="address">The input IPv4 address.</param>
        /// <param name="incrementBy">The increment value (defaults to <b>+1</b>).</param>
        /// <returns>The next address or <b>0.0.0.0</b> when we wrap-around the address space.</returns>
        /// <exception cref="NotSupportedException">Thrown for non-IPv4 addresses.</exception>
        public static IPAddress AddressIncrement(IPAddress address, int incrementBy = 1)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new NotSupportedException("Only IPv4 addresses are supported.");
            }

            var     addressBytes = address.GetAddressBytes();
            uint    addressValue;

            addressValue  = (uint)addressBytes[0] << 24;
            addressValue |= (uint)addressBytes[1] << 16;
            addressValue |= (uint)addressBytes[2] << 8;
            addressValue |= (uint)addressBytes[3];

            addressValue += (uint)incrementBy;

            addressBytes[0] = (byte)(addressValue >> 24);
            addressBytes[1] = (byte)(addressValue >> 16);
            addressBytes[2] = (byte)(addressValue >> 8);
            addressBytes[3] = (byte)(addressValue);

            return IPAddress.Parse($"{addressBytes[0]}.{addressBytes[1]}.{addressBytes[2]}.{addressBytes[3]}");
        }

        /// <summary>
        /// Converts an IPv4 address into a32-bit unsigned integer equivalent.
        /// </summary>
        /// <param name="address">The input IPv4 address.</param>
        /// <returns>The 32-bit unsigned integer equivalent.</returns>
        public static uint AddressToUint(IPAddress address)
        {
            var     addressBytes = address.GetAddressBytes();
            uint    addressValue;

            addressValue  = (uint)addressBytes[0] << 24;
            addressValue |= (uint)addressBytes[1] << 16;
            addressValue |= (uint)addressBytes[2] << 8;
            addressValue |= (uint)addressBytes[3];

            return addressValue;
        }

        /// <summary>
        /// Converts an unsigned 32-bit integer into an IPv4 address.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <returns>The <see cref="IPAddress"/>.</returns>
        public static IPAddress UintToAddress(uint value)
        {
            var addressBytes = new byte[4];

            addressBytes[0] = (byte)(value >> 24);
            addressBytes[1] = (byte)(value >> 16);
            addressBytes[2] = (byte)(value >> 8);
            addressBytes[3] = (byte)(value);

            return new IPAddress(addressBytes);
        }

        /// <summary>
        /// Determines whether an integer is a valid network port number.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns><c>true</c> if the port is valid.</returns>
        public static bool IsValidPort(int port)
        {
            return 0 < port && port <= ushort.MaxValue;
        }

        /// <summary>
        /// Used to temporarily modify the <b>hosts</b> file used by the DNS resolver
        /// for debugging purposes.
        /// </summary>
        /// <param name="hostEntries">A dictionary mapping the hostnames to an IP address or <c>null</c>.</param>
        /// <remarks>
        /// <note>
        /// This requires elevated administrative privileges.  You'll need to launch Visual Studio
        /// or favorite development envirnment with these.
        /// </note>
        /// <para>
        /// This method adds or removes a temporary section of host entry definitions
        /// delimited by special comment lines.  When <paramref name="hostEntries"/> is 
        /// non-null or empty, the section will be added or updated.  Otherwise, the
        /// section will be removed.
        /// </para>
        /// </remarks>
        public static void ModifyLocalHosts(Dictionary<string, IPAddress> hostEntries = null)
        {
#if XAMARIN
            throw new NotSupportedException();
#else
            // We're seeing transient file locked errors when trying to open the [hosts] file.
            // My guess is that this is cause by the Window DNS resolver opening the file as
            // READ/WRITE to prevent it from being modified while the resolver is reading any
            // changes.
            //
            // We're going to mitigate this by retrying a few times.
            //
            // It can take a bit of time for the Windows DNS resolver to pick up the change.
            //
            //      https://github.com/jefflill/NeonForge/issues/244
            //
            // We're going to mitigate this by writing a [neon-modify-local-hosts.hive] record with
            // a random IP address and then wait for [ipconfig /displaydns] to report the 
            // correct address below.

            var retryWrite    = new LinearRetryPolicy(typeof(IOException), maxAttempts: 10, retryInterval: TimeSpan.FromMilliseconds(500));
            var updateHost    = "neon-modify-local-hosts.hive";
            var updateAddress = new IPAddress(NeonHelper.Rand(int.MaxValue));

            retryWrite.InvokeAsync(
                async () =>
                {
                    string hostsPath;

                    if (NeonHelper.IsWindows)
                    {
                        hostsPath = Path.Combine(Environment.GetEnvironmentVariable("windir"), "System32", "drivers", "etc", "hosts");
                    }
                    else if (NeonHelper.IsLinux || NeonHelper.IsOSX)
                    {
                        hostsPath = "/etc/hosts";
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    const string beginMarker = "# BEGIN-NEON-MODIFY";
                    const string endMarker   = "# END-NEON-MODIFY";

                    var inputLines  = File.ReadAllLines(hostsPath);
                    var lines       = new List<string>();
                    var tempSection = false;

                    // Strip out any existing temporary sections.

                    foreach (var line in inputLines)
                    {
                        switch (line.Trim())
                        {
                            case beginMarker:

                                tempSection = true;
                                break;

                            case endMarker:

                                tempSection = false;
                                break;

                            default:

                                if (!tempSection)
                                {
                                    lines.Add(line);
                                }
                                break;
                        }
                    }

                    if (hostEntries?.Count > 0)
                    {
                        lines.Add(beginMarker);

                        // Append the special update host with random IP address.

                        var address = updateAddress.ToString();

                        lines.Add($"        {address}{new string(' ', 16 - address.Length)}    {updateHost}");

                        // Append the new entries.

                        foreach (var item in hostEntries)
                        {
                            address = item.Value.ToString();

                            lines.Add($"        {address}{new string(' ', 16 - address.Length)}    {item.Key}");
                        }

                        lines.Add(endMarker);
                    }

                    File.WriteAllLines(hostsPath, lines.ToArray());
                    await Task.CompletedTask;
                    
                }).Wait();

            if (NeonHelper.IsWindows)
            {
                // Poll [ipconfig /displaydns] until it reports the correct address for the
                // [neon-modify-local-hosts.hive].  The command output will contain a series of 
                // records foreach cached DNS entry including those loaded from [hosts]
                // that will look like:
                //
                //      neon-modify-local-hosts.hive                                
                //      ----------------------------------------       
                //      No records of type AAAA
                //
                //
                //      neon-modify-local-hosts.hive
                //      ----------------------------------------       
                //      Record Name . . . . . : www.pearl2o.com
                //      Record Type. . . . .  : 1                      
                //      Time To Live. . . .   : 86400                  
                //      Data Length . . . . . : 4                      
                //      Section. . . . . . .  : Answer
                //      A (Host) Record . . . : 10.100.16.0
                //
                // If [hostEntries] is not null and contains at least one entry, we'll look 
                // for [neon-modify-local-hosts.hive] and then the first instance of "A (Host) Record"
                // afterwards and then extract and compare the IP address to ensure that the 
                // resolver has loaded the new entries.
                //
                // If [hostEntries] is null or empty, we'll wait until there are no records
                // for [neon-modify-local-hosts.hive] to ensure that the resolver has reloaded the
                // hosts file after we removed the entries.

                var retryReady = new LinearRetryPolicy(typeof(KeyNotFoundException), maxAttempts: 20, retryInterval: TimeSpan.FromMilliseconds(500));

                retryReady.InvokeAsync(
                    async () =>
                    {
                        var response = NeonHelper.ExecuteCaptureStreams("ipconfig", "/displaydns");

                        if (response.ExitCode != 0)
                        {
                            throw new Exception($"DNS hosts modification failed because [ipconfig /displaydns] returned [exitcode={response.ExitCode}].");
                        }

                        var output  = response.OutputText;
                        var posHost = output.IndexOf(updateHost + "\r\n    ----");  // Ensure that the DNS domain is underlined.

                        if (hostEntries?.Count > 0)
                        {
                            // Ensure that new records have been loaded by the resolver.

                            if (posHost == -1)
                            {
                                throw new KeyNotFoundException($"[ipconfig /displaydns] is not reporting a record for [{updateHost}].");
                            }

                            var posARecord = output.IndexOf("A (Host) Record", posHost);

                            if (posARecord == -1)
                            {
                                throw new KeyNotFoundException($"[ipconfig /displaydns] is not reporting an A record for [{updateHost}].");
                            }

                            var posStart = output.IndexOf(':', posARecord);

                            if (posStart == -1)
                            {
                                throw new KeyNotFoundException($"[ipconfig /displaydns] is not reporting an A record for [{updateHost}].");
                            }

                            posStart += 2;

                            var posEnd = posStart;

                            while (true)
                            {
                                var ch = output[posEnd];

                                if (char.IsDigit(ch) || ch == '.')
                                {
                                    posEnd++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            var address = output.Substring(posStart, posEnd - posStart).Trim();

                            if (address != updateAddress.ToString())
                            {
                                throw new KeyNotFoundException($"[ipconfig /displaydns] is reporting A record [{updateHost}={address}] rather than [{updateAddress}].");
                            }
                        }
                        else
                        {
                            // Ensure that the resolver recognizes that we removed the records.

                            if (posHost != -1)
                            {
                                throw new KeyNotFoundException($"[ipconfig /displaydns] is still reporting an A record for [{updateHost}].");
                            }
                        }

                        await Task.CompletedTask;

                    }).Wait();
            }
#endif
        }
    }
}


