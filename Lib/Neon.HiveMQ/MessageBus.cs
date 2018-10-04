﻿//-----------------------------------------------------------------------------
// FILE:	    MessageBus.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;

using EasyNetQ;
using EasyNetQ.DI;
using EasyNetQ.Logging;
using EasyNetQ.Management.Client;

using RabbitMQ;
using RabbitMQ.Client;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Common;
using Neon.Diagnostics;
using Neon.Net;

// $note(jeff.lill):
//
// The link below is to a series of great articles from 2017 comparing RabbitMQ
// to Kafka.  It's probably the best overview of RabbitMQ that I've found so far.
//
//      https://jack-vanlightly.com/blog/2017/12/3/rabbitmq-vs-kafka-series-introduction

namespace Neon.HiveMQ
{
    /// <summary>
    /// A thin layer over EasyNetQ and RabbitMQ that provides a more flexibility than the
    /// EasyNetQ basic API and is easier to use than the EasyNetQ advanced API or the
    /// native RabbitMQ API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a relatively thin layer over the <a href="https://github.com/EasyNetQ/EasyNetQ">EasyNetQ</a>
    /// <a href="https://github.com/EasyNetQ/EasyNetQ/wiki/The-Advanced-API">Advanced API</a>.  This
    /// interface is designed to be a little more flexible than the simple EasyNetQ API while still
    /// being very easy to use.
    /// </para>
    /// <para>
    /// The main issue I have with the simple EasyNetQ API is that hides the concept of queues from
    /// the application by implicitly creating a queue for every message type (based on its name).
    /// This doesn't cleanly support the ability to have generic messages that can targeted at
    /// specific consumers.
    /// </para>
    /// <para>
    /// For example, say I have two separate services, <b>S1</b> and <b>S2</b> and I want to be
    /// able to send a <b>RESET</b> message to <b>S1</b> and another <b>RESET</b> message to <b>S2</b>.
    /// This could be accomplished with EasyNetQ by defining a separate message for each target
    /// like <b>RESET1</b> and <b>RESET2</b> and having the service subscribe to the corresponding
    /// message.  While this works, it's not super scalable; you'd have to define 10 <b>RESET</b>
    /// classes to handle 10 services.
    /// </para>
    /// <para>
    /// Another approach would be define a single <b>RESET</b> message that includes a field
    /// identifing the target service and then broadcast these to both <b>S1</b> and <b>S2</b>
    /// and then have each service ignore messages that weren't targeted at it.  This is messy
    /// because it depends on custom service code to reject messages which will consume some 
    /// netork and CPU for no real reason.
    /// </para>
    /// <para>
    /// It may be possible to implement another solution using topic routing, but that's
    /// somewhat more complex than is really necessary for a typical user.
    /// </para>
    /// <para>
    /// Message buses are typically constructed by instantiating a <see cref="HiveMQSettings"/> 
    /// instance and then calling its <see cref="HiveMQSettings.ConnectBus(string, string, string, BusSettings)"/>
    /// method.  The instance returned is thread-safe and generally most applications will instantiate
    /// a single <see cref="MessageBus"/> instance, use it throughout the lifespan of the application
    /// and then dispose the bus when it's done.
    /// </para>
    /// <para>
    /// <see cref="MessageBus"/> implementations combine the RabbitMQ concepts of exchanges and
    /// queues into a <see cref="BasicChannel"/>, <see cref="BroadcastChannel"/>, or <see cref="QueryChannel"/>.  
    /// The bus transparently creates and manages the underlying  queues and exchanges.
    /// </para>
    /// <para>
    /// Three types of channels are currently implemented:
    /// </para>
    /// <list type="table">
    /// <item>
    ///     <term><see cref="BasicChannel"/></term>
    ///     <description>
    ///     Basic channels deliver each message to a <b>single consumer</b>.  A typical use is
    ///     to load balance work across multiple consumers.  Call <see cref="CreateBasicChannel(string, bool, bool, bool, TimeSpan?, int?, int?)"/>
    ///     to create a basic channel.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><see cref="BroadcastChannel"/></term>
    ///     <description>
    ///     Broadcast channels deliver each message to <b>all consumers</b>.  Call <see cref="CreateBroadcastChannel(string, bool, bool, TimeSpan?, int?, int?)"/> 
    ///     to create a broadcast channel.
    ///     </description>
    /// </item>
    /// <item>
    ///     <term><see cref="QueryChannel"/></term>
    ///     <description>
    ///     Query channels deliver a message to a <b>consumer</b> and then waits for a reply.  
    ///     Call <see cref="CreateQueryChannel(string, bool, bool, bool, TimeSpan?, int?, int?)"/> 
    ///     to create a query channel.
    ///     </description>
    /// </item>
    /// </list>
    /// <note>
    /// A <c>channel</c> in this context has nothing to do with an underlying
    /// RabbitMQ channel.  These are two entirely different concepts.
    /// </note>
    /// <note>
    /// <b>FYI:</b> I found this <a href="https://jack-vanlightly.com/blog/2017/12/3/rabbitmq-vs-kafka-series-introduction">link</a> 
    /// to be a useful overview of RabbitMQ features and how they compare to Kafka.
    /// </note>
    /// </remarks>
    public class MessageBus : IDisposable
    {
        private object          syncLock     = new object();
        private bool            isDisposed   = false;
        private bool            disposingNow = false;
        private List<Channel>   channels     = new List<Channel>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">The message queue cluster settings.</param>
        /// <param name="username">Optional username (overrides <see cref="HiveMQSettings.Username"/>).</param>
        /// <param name="password">Optional password (overrides <see cref="HiveMQSettings.Password"/>).</param>
        /// <param name="virtualHost">Optional target virtual host (overrides <see cref="HiveMQSettings.VirtualHost"/>).</param>
        public MessageBus(HiveMQSettings settings, string username = null, string password = null, string virtualHost = null)
        {
            Covenant.Requires<ArgumentNullException>(settings != null);

            this.EasyBus = settings.ConnectEasyNetQ(username, password, virtualHost);
        }

        /// <summary>
        /// Releases all associated resources.
        /// </summary>
        /// <param name="disposing">Pass <c>true</c> if we're disposing, <c>false</c> if we're finalizing.</param>
        protected virtual void Dispose(bool disposing)
        {
            lock (syncLock)
            {
                if (!isDisposed)
                {
                    if (disposing)
                    {
                        try
                        {
                            disposingNow = true;

                            foreach (var channel in channels)
                            {
                                try
                                {
                                    channel.Dispose();
                                }
                                catch
                                {
                                    // Intentionally ignoring these.
                                }
                            }

                            channels.Clear();
                            channels = null;

                            EasyBus.Dispose();
                            EasyBus = null;
                        }
                        finally
                        {
                            disposingNow = false;
                        }
                    }

                    isDisposed = true;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Ensures that the instance has not been disposed.
        /// </summary>
        private void CheckDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(MessageBus));
            }
        }

        /// <summary>
        /// Returns the associated mid-level EasyNetQ <see cref="IBus"/>.
        /// </summary>
        internal IBus EasyBus { get; private set; }

        /// <summary>
        /// Creates a basic message channel if it doesn't already exist.  Basic message channels
        /// are used to forward messages to one or more consumers such that each message
        /// is delivered to a <b>single consumer</b>.  This is typically used for load balancing
        /// work across multiple consumers.
        /// </summary>
        /// <param name="name">The channel name.  This can be a maximum of 250 characters.</param>
        /// <param name="durable">
        /// Optionally specifies that the channel should survive message cluster restarts.  
        /// This defaults to <c>false</c>.
        /// </param>
        /// <param name="autoDelete">
        /// Optionally specifies that the channel should be deleted once all consumers have 
        /// disconnected.  This defaults to <c>false</c>.
        /// </param>
        /// <param name="exclusive">
        /// Optionally specifies that this channel instance will exclusively receive
        /// messages from the queue.  This defaults to <c>false</c>.
        /// </param>
        /// <param name="messageTTL">
        /// Optionally specifies the maximum time a message can remain in the channel before 
        /// being deleted.  This defaults to <c>null</c> which disables this feature.
        /// </param>
        /// <param name="maxLength">
        /// Optionally specifies the maximum number of messages that can be waiting in the 
        /// channel before messages at the front of the channel will be deleted.  This 
        /// defaults to unconstrained.
        /// </param>
        /// <param name="maxLengthBytes">
        /// Optionally specifies the maximum total bytes of messages that can be waiting in 
        /// the channel before messages at the front of the channel will be deleted.  This 
        /// defaults to unconstrained.
        /// </param>
        /// <returns>The <see cref="BasicChannel"/> created.</returns>
        /// <remarks>
        /// <note>
        /// The instance returned should be disposed when you're done with it.
        /// </note>
        /// <note>
        /// The maximum possible <paramref name="messageTTL"/> is <see cref="int.MaxValue"/> or just
        /// under 24 days.  An <see cref="ArgumentException"/> will be thrown if this is exceeded.
        /// </note>
        /// </remarks>
        public BasicChannel CreateBasicChannel(
            string      name, 
            bool        durable = false, 
            bool        exclusive = false,
            bool        autoDelete = false, 
            TimeSpan?   messageTTL = null, 
            int?        maxLength = null, 
            int?        maxLengthBytes = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
            Covenant.Requires<ArgumentException>(name.Length <= 250);
            Covenant.Requires<ArgumentException>(!messageTTL.HasValue || messageTTL.Value >= TimeSpan.Zero);
            Covenant.Requires<ArgumentException>(!maxLength.HasValue || maxLength.Value > 0);
            Covenant.Requires<ArgumentException>(!maxLengthBytes.HasValue || maxLengthBytes.Value > 0);

            lock (syncLock)
            {
                CheckDisposed();

                if (messageTTL.HasValue && messageTTL.Value.TotalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentException($"[{nameof(messageTTL)}={messageTTL}] cannot exceed about 24.85 days (2^31-1 milliseconds).");
                }

                var channel = new BasicChannel(
                    this, 
                    name,
                    durable: durable,
                    exclusive: exclusive,
                    autoDelete: autoDelete,
                    messageTTL: messageTTL,
                    maxLength: maxLength,
                    maxLengthBytes: maxLengthBytes);

                lock (syncLock)
                {
                    channels.Add(channel);
                }

                return channel;
            }
        }

        /// <summary>
        /// Creates a broadcast message channel if it doesn't already exist.  Broadcast message channels
        /// are used to forward messages to one or more consumers such that each message
        /// is delivered to <b>all consumers</b>.
        /// </summary>
        /// <param name="name">The channel name.  This can be a maximum of 250 characters.</param>
        /// <param name="durable">
        /// Optionally specifies that the channel should survive message cluster restarts.  
        /// This defaults to <c>false</c>.
        /// </param>
        /// <param name="autoDelete">
        /// Optionally specifies that the channel should be deleted once all consumers have 
        /// disconnected.  This defaults to <c>false</c>.
        /// </param>
        /// <param name="messageTTL">
        /// Optionally specifies the maximum time a message can remain in the channel before 
        /// being deleted.  This defaults to <c>null</c> which disables this feature.
        /// </param>
        /// <param name="maxLength">
        /// Optionally specifies the maximum number of messages that can be waiting in the channel
        /// before messages at the front of the channel will be deleted.  This defaults 
        /// to unconstrained.
        /// </param>
        /// <param name="maxLengthBytes">
        /// Optionally specifies the maximum total bytes of messages that can be waiting in 
        /// the channel before messages at the front of the channel will be deleted.  This 
        /// defaults to unconstrained.
        /// </param>
        /// <returns>The <see cref="BroadcastChannel"/> created.</returns>
        /// <remarks>
        /// <note>
        /// The instance returned should be disposed when you're done with it.
        /// </note>
        /// <note>
        /// The maximum possible <paramref name="messageTTL"/> is <see cref="int.MaxValue"/> or just
        /// under 24 days.  An <see cref="ArgumentException"/> will be thrown if this is exceeded.
        /// </note>
        /// </remarks>
        public BroadcastChannel CreateBroadcastChannel(
            string      name,
            bool        durable = false,
            bool        autoDelete = false,
            TimeSpan?   messageTTL = null,
            int?        maxLength = null,
            int?        maxLengthBytes = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
            Covenant.Requires<ArgumentException>(name.Length <= 250);
            Covenant.Requires<ArgumentException>(!messageTTL.HasValue || messageTTL.Value >= TimeSpan.Zero);
            Covenant.Requires<ArgumentException>(!maxLength.HasValue || maxLength.Value > 0);
            Covenant.Requires<ArgumentException>(!maxLengthBytes.HasValue || maxLengthBytes.Value > 0);

            lock (syncLock)
            {
                CheckDisposed();

                if (messageTTL.HasValue && messageTTL.Value.TotalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentException($"[{nameof(messageTTL)}={messageTTL}] cannot exceed about 24.85 days (2^31-1 milliseconds).");
                }

                var channel = new BroadcastChannel(
                    this,
                    name,
                    durable: durable,
                    autoDelete: autoDelete,
                    messageTTL: messageTTL,
                    maxLength: maxLength,
                    maxLengthBytes: maxLengthBytes);

                lock (syncLock)
                {
                    channels.Add(channel);
                }

                return channel;
            }
        }

        /// <summary>
        /// Creates a query message channel if it doesn't already exist.  Query message channels
        /// are used to implement a query/response pattern by sending a message to a consumer and
        /// then waiting for it to send a reply message.
        /// </summary>
        /// <param name="name">The channel name.  This can be a maximum of 250 characters.</param>
        /// <param name="durable">
        /// Optionally specifies that the channel should survive message cluster restarts.  
        /// This defaults to <c>false</c>.
        /// </param>
        /// <param name="autoDelete">
        /// Optionally specifies that the channel should be deleted once all consumers have 
        /// disconnected.  This defaults to <c>false</c>.
        /// </param>
        /// <param name="exclusive">
        /// Optionally specifies that this channel instance will exclusively receive
        /// messages from the queue.  This defaults to <c>false</c>.
        /// </param>
        /// <param name="messageTTL">
        /// Optionally specifies the maximum time a message can remain in the channel before 
        /// being deleted.  This defaults to <c>null</c> which disables this feature.
        /// </param>
        /// <param name="maxLength">
        /// Optionally specifies the maximum number of messages that can be waiting in the channel
        /// before messages at the front of the channel will be deleted.  This defaults 
        /// to unconstrained.
        /// </param>
        /// <param name="maxLengthBytes">
        /// Optionally specifies the maximum total bytes of messages that can be waiting in 
        /// the channel before messages at the front of the channel will be deleted.  This 
        /// defaults to unconstrained.
        /// </param>
        /// <returns>The <see cref="QueryChannel"/> created.</returns>
        /// <remarks>
        /// <note>
        /// The instance returned should be disposed when you're done with it.
        /// </note>
        /// <note>
        /// The maximum possible <paramref name="messageTTL"/> is <see cref="int.MaxValue"/> or just
        /// under 24 days.  An <see cref="ArgumentException"/> will be thrown if this is exceeded.
        /// </note>
        /// </remarks>
        public QueryChannel CreateQueryChannel(
            string      name,
            bool        durable = false,
            bool        autoDelete = false,
            bool        exclusive = false,
            TimeSpan?   messageTTL = null,
            int?        maxLength = null,
            int?        maxLengthBytes = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name));
            Covenant.Requires<ArgumentException>(name.Length <= 250);
            Covenant.Requires<ArgumentException>(!messageTTL.HasValue || messageTTL.Value >= TimeSpan.Zero);
            Covenant.Requires<ArgumentException>(!maxLength.HasValue || maxLength.Value > 0);
            Covenant.Requires<ArgumentException>(!maxLengthBytes.HasValue || maxLengthBytes.Value > 0);

#if TODO
            lock (syncLock)
            {
                CheckDisposed();

                if (messageTTL.HasValue && messageTTL.Value.TotalMilliseconds > int.MaxValue)
                {
                    throw new ArgumentException($"[{nameof(messageTTL)}={messageTTL}] cannot exceed about 24.85 days (2^31-1 milliseconds).");
                }

                var channel = new QueryChannel(
                    this, 
                    name, 
                    durable: durable,
                    exclusive: exclusive,
                    autoDelete: autoDelete,
                    messageTTL: messageTTL,
                    maxLength: maxLength,
                    maxLengthBytes: maxLengthBytes);

                lock (syncLock)
                {
                    channels.Add(channel);
                }

                return channel;
            }
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Removes a disposed channel from the bus.
        /// </summary>
        /// <param name="channel">The channel to be removed.</param>
        internal void RemoveChannel(Channel channel)
        {
            lock (syncLock)
            {
                if (!disposingNow)
                {
                    channels.Remove(channel);
                }
            }
        }
    }
}
