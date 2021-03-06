﻿//-----------------------------------------------------------------------------
// FILE:        Test_Messages.Cluster.cs
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
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Neon.Cadence;
using Neon.Cadence.Internal;
using Neon.Common;
using Neon.Cryptography;
using Neon.Data;
using Neon.IO;
using Neon.Xunit;
using Neon.Xunit.Cadence;

using Newtonsoft.Json;
using Test.Neon.Models;
using Xunit;

namespace TestCadence
{
    public partial class Test_Messages
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestInitializeRequest()
        {
            InitializeRequest message;

            using (var stream = new MemoryStream())
            {
                message = new InitializeRequest();

                Assert.Equal(MessageTypes.InitializeReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<InitializeRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.LibraryAddress);
                Assert.Equal(0, message.LibraryPort);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.LibraryAddress = "1.2.3.4";
                Assert.Equal("1.2.3.4", message.LibraryAddress);
                message.LibraryPort = 666;
                Assert.Equal(666, message.LibraryPort);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<InitializeRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("1.2.3.4", message.LibraryAddress);
                Assert.Equal(666, message.LibraryPort);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("1.2.3.4", message.LibraryAddress);
                Assert.Equal(666, message.LibraryPort);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("1.2.3.4", message.LibraryAddress);
                Assert.Equal(666, message.LibraryPort);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestInitializeReply()
        {
            InitializeReply message;

            using (var stream = new MemoryStream())
            {
                message = new InitializeReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<InitializeReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<InitializeReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestConnectRequest()
        {
            ConnectRequest message;

            using (var stream = new MemoryStream())
            {
                message = new ConnectRequest();

                Assert.Equal(MessageTypes.ConnectReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<ConnectRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Endpoints);
                Assert.Null(message.Identity);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Endpoints = "1.1.1.1:555,2.2.2.2:5555";
                Assert.Equal("1.1.1.1:555,2.2.2.2:5555", message.Endpoints);
                message.Identity = "my-identity";
                Assert.Equal("my-identity", message.Identity);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<ConnectRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("1.1.1.1:555,2.2.2.2:5555", message.Endpoints);
                Assert.Equal("my-identity", message.Identity);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("1.1.1.1:555,2.2.2.2:5555", message.Endpoints);
                Assert.Equal("my-identity", message.Identity);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("1.1.1.1:555,2.2.2.2:5555", message.Endpoints);
                Assert.Equal("my-identity", message.Identity);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestConnectReply()
        {
            ConnectReply message;

            using (var stream = new MemoryStream())
            {
                message = new ConnectReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<ConnectReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<ConnectReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestDomainDescribeRequest()
        {
            DomainDescribeRequest message;

            using (var stream = new MemoryStream())
            {
                message = new DomainDescribeRequest();

                Assert.Equal(MessageTypes.DomainDescribeReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainDescribeRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Name);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Name = "my-domain";
                Assert.Equal("my-domain", message.Name);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainDescribeRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Name);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Name);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Name);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestDomainDescribeReply()
        {
            DomainDescribeReply message;

            using (var stream = new MemoryStream())
            {
                message = new DomainDescribeReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainDescribeReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);
                Assert.False(message.ConfigurationEmitMetrics);
                Assert.Equal(0, message.ConfigurationRetentionDays);
                Assert.Null(message.DomainInfoName);
                Assert.Null(message.DomainInfoDescription);
                Assert.Equal(DomainStatus.Unspecified, message.DomainInfoStatus);
                Assert.Null(message.DomainInfoOwnerEmail);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);
                message.ConfigurationEmitMetrics = true;
                Assert.True(message.ConfigurationEmitMetrics);
                message.ConfigurationRetentionDays = 7;
                Assert.Equal(7, message.ConfigurationRetentionDays);
                message.DomainInfoName = "my-name";
                Assert.Equal("my-name", message.DomainInfoName);
                message.DomainInfoDescription = "my-description";
                Assert.Equal("my-description", message.DomainInfoDescription);
                message.DomainInfoStatus = DomainStatus.Deprecated;
                Assert.Equal(DomainStatus.Deprecated, message.DomainInfoStatus);
                message.DomainInfoOwnerEmail = "joe@bloe.com";
                Assert.Equal("joe@bloe.com", message.DomainInfoOwnerEmail);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainDescribeReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
                Assert.True(message.ConfigurationEmitMetrics);
                Assert.Equal(7, message.ConfigurationRetentionDays);
                Assert.Equal("my-name", message.DomainInfoName);
                Assert.Equal("my-description", message.DomainInfoDescription);
                Assert.Equal(DomainStatus.Deprecated, message.DomainInfoStatus);
                Assert.Equal("joe@bloe.com", message.DomainInfoOwnerEmail);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
                Assert.True(message.ConfigurationEmitMetrics);
                Assert.Equal(7, message.ConfigurationRetentionDays);
                Assert.Equal("my-name", message.DomainInfoName);
                Assert.Equal("my-description", message.DomainInfoDescription);
                Assert.Equal(DomainStatus.Deprecated, message.DomainInfoStatus);
                Assert.Equal("joe@bloe.com", message.DomainInfoOwnerEmail);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
                Assert.True(message.ConfigurationEmitMetrics);
                Assert.Equal(7, message.ConfigurationRetentionDays);
                Assert.Equal("my-name", message.DomainInfoName);
                Assert.Equal("my-description", message.DomainInfoDescription);
                Assert.Equal(DomainStatus.Deprecated, message.DomainInfoStatus);
                Assert.Equal("joe@bloe.com", message.DomainInfoOwnerEmail);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestDomainRegisterRequest()
        {
            DomainRegisterRequest message;

            using (var stream = new MemoryStream())
            {
                message = new DomainRegisterRequest();

                Assert.Equal(MessageTypes.DomainRegisterReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainRegisterRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Name);
                Assert.Null(message.Description);
                Assert.Null(message.OwnerEmail);
                Assert.False(message.EmitMetrics);
                Assert.Equal(0, message.RetentionDays);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Name = "my-domain";
                Assert.Equal("my-domain", message.Name);
                message.Description = "my-description";
                Assert.Equal("my-description", message.Description);
                message.OwnerEmail = "my-email";
                Assert.Equal("my-email", message.OwnerEmail);
                message.EmitMetrics = true;
                Assert.True(message.EmitMetrics);
                message.RetentionDays = 14;
                Assert.Equal(14, message.RetentionDays);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainRegisterRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Name);
                Assert.Equal("my-description", message.Description);
                Assert.Equal("my-email", message.OwnerEmail);
                Assert.True(message.EmitMetrics);
                Assert.Equal(14, message.RetentionDays);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Name);
                Assert.Equal("my-description", message.Description);
                Assert.Equal("my-email", message.OwnerEmail);
                Assert.True(message.EmitMetrics);
                Assert.Equal(14, message.RetentionDays);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Name);
                Assert.Equal("my-description", message.Description);
                Assert.Equal("my-email", message.OwnerEmail);
                Assert.True(message.EmitMetrics);
                Assert.Equal(14, message.RetentionDays);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestDomainRegisterReply()
        {
            DomainRegisterReply message;

            using (var stream = new MemoryStream())
            {
                message = new DomainRegisterReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainRegisterReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainRegisterReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestDomainUpdateRequest()
        {
            DomainUpdateRequest message;

            using (var stream = new MemoryStream())
            {
                message = new DomainUpdateRequest();

                Assert.Equal(MessageTypes.DomainUpdateReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainUpdateRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Name);
                Assert.Null(message.UpdatedInfoDescription);
                Assert.Null(message.UpdatedInfoOwnerEmail);
                Assert.False(message.ConfigurationEmitMetrics);
                Assert.Equal(0, message.ConfigurationRetentionDays);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Name = "my-name";
                Assert.Equal("my-name", message.Name);
                message.UpdatedInfoDescription = "my-description";
                Assert.Equal("my-description", message.UpdatedInfoDescription);
                message.UpdatedInfoOwnerEmail = "joe@bloe.com";
                Assert.Equal("joe@bloe.com", message.UpdatedInfoOwnerEmail);
                message.ConfigurationEmitMetrics = true;
                Assert.True(message.ConfigurationEmitMetrics);
                message.ConfigurationRetentionDays = 7;
                Assert.Equal(7, message.ConfigurationRetentionDays);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainUpdateRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-name", message.Name);
                Assert.Equal("my-description", message.UpdatedInfoDescription);
                Assert.Equal("joe@bloe.com", message.UpdatedInfoOwnerEmail);
                Assert.True(message.ConfigurationEmitMetrics);
                Assert.Equal(7, message.ConfigurationRetentionDays);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-name", message.Name);
                Assert.Equal("my-description", message.UpdatedInfoDescription);
                Assert.Equal("joe@bloe.com", message.UpdatedInfoOwnerEmail);
                Assert.True(message.ConfigurationEmitMetrics);
                Assert.Equal(7, message.ConfigurationRetentionDays);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-name", message.Name);
                Assert.Equal("my-description", message.UpdatedInfoDescription);
                Assert.Equal("joe@bloe.com", message.UpdatedInfoOwnerEmail);
                Assert.True(message.ConfigurationEmitMetrics);
                Assert.Equal(7, message.ConfigurationRetentionDays);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestDomainUpdateReply()
        {
            DomainUpdateReply message;

            using (var stream = new MemoryStream())
            {
                message = new DomainUpdateReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainUpdateReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);
                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<DomainUpdateReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestTerminateRequest()
        {
            TerminateRequest message;

            using (var stream = new MemoryStream())
            {
                message = new TerminateRequest();

                Assert.Equal(MessageTypes.TerminateReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<TerminateRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<TerminateRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestTerminateReply()
        {
            TerminateReply message;

            using (var stream = new MemoryStream())
            {
                message = new TerminateReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<TerminateReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<TerminateReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestHeartbeatRequest()
        {
            HeartbeatRequest message;

            using (var stream = new MemoryStream())
            {
                message = new HeartbeatRequest();

                Assert.Equal(MessageTypes.HeartbeatReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<HeartbeatRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<HeartbeatRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestHeartbeatReply()
        {
            HeartbeatReply message;

            using (var stream = new MemoryStream())
            {
                message = new HeartbeatReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<HeartbeatReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<HeartbeatReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestCancelRequest()
        {
            CancelRequest message;

            using (var stream = new MemoryStream())
            {
                message = new CancelRequest();

                Assert.Equal(MessageTypes.CancelReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<CancelRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);

                // Round-trip

                message.RequestId = 555;
                message.TargetRequestId = 666;
                Assert.Equal(555, message.RequestId);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<CancelRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.TargetRequestId);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.TargetRequestId);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.TargetRequestId);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestCancelReply()
        {
            CancelReply message;

            using (var stream = new MemoryStream())
            {
                message = new CancelReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<CancelReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);
                message.WasCancelled = true;
                Assert.True(message.WasCancelled);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<CancelReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
                Assert.True(message.WasCancelled);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("MyError", message.Error.String);
                Assert.True(message.WasCancelled);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.True(message.WasCancelled);
                Assert.Equal("MyError", message.Error.String);
            }
        }
    }
}
