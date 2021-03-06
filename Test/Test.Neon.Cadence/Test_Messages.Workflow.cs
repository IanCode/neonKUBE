﻿//-----------------------------------------------------------------------------
// FILE:        Test_Messages.Workflow.cs
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
        public void TestWorkflowRegisterRequest()
        {
            WorkflowRegisterRequest message;

            using (var stream = new MemoryStream())
            {
                message = new WorkflowRegisterRequest();

                Assert.Equal(MessageTypes.WorkflowRegisterReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowRegisterRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);

                // Round-trip

                message.RequestId = 555;
                message.Name = "Foo";
                Assert.Equal(555, message.RequestId);
                Assert.Equal("Foo", message.Name);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowRegisterRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("Foo", message.Name);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("Foo", message.Name);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("Foo", message.Name);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestWorkflowRegisterReply()
        {
            WorkflowRegisterReply message;

            using (var stream = new MemoryStream())
            {
                message = new WorkflowRegisterReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowRegisterReply>(stream, ignoreTypeCode: true);
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

                message = ProxyMessage.Deserialize<WorkflowRegisterReply>(stream, ignoreTypeCode: true);
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
        public void TestWorkflowExecuteRequest()
        {
            WorkflowExecuteRequest message;

            using (var stream = new MemoryStream())
            {
                message = new WorkflowExecuteRequest();

                Assert.Equal(MessageTypes.WorkflowExecuteReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowExecuteRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Domain);
                Assert.Null(message.Name);
                Assert.Null(message.Args);
                Assert.Null(message.Options);

                // Round-trip

                message.RequestId = 555;
                message.Domain = "my-domain";
                message.Name = "Foo";
                message.Args = new Dictionary<string, object>() { { "hello", "world!" } };
                message.Options = new StartWorkflowOptions() { TaskList = "my-list", ExecutionStartToCloseTimeout = "100s" };
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Domain);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);
                Assert.Equal("my-list", message.Options.TaskList);
                Assert.Equal("100s", message.Options.ExecutionStartToCloseTimeout);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowExecuteRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Domain);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);
                Assert.Equal("my-list", message.Options.TaskList);
                Assert.Equal("100s", message.Options.ExecutionStartToCloseTimeout);
                Assert.Equal("world!", message.Args["hello"]);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Domain);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);
                Assert.Equal("my-list", message.Options.TaskList);
                Assert.Equal("100s", message.Options.ExecutionStartToCloseTimeout);
                Assert.Equal("world!", message.Args["hello"]);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal("my-domain", message.Domain);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);
                Assert.Equal("my-list", message.Options.TaskList);
                Assert.Equal("100s", message.Options.ExecutionStartToCloseTimeout);
                Assert.Equal("world!", message.Args["hello"]);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestWorkflowExecuteReply()
        {
            WorkflowExecuteReply message;

            using (var stream = new MemoryStream())
            {
                message = new WorkflowExecuteReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowExecuteReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Equal(TimeSpan.Zero, message.DecisionTimeout);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.DecisionTimeout = TimeSpan.FromMinutes(120);
                Assert.Equal(TimeSpan.FromMinutes(120), message.DecisionTimeout);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowExecuteReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(TimeSpan.FromMinutes(120), message.DecisionTimeout);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(TimeSpan.FromMinutes(120), message.DecisionTimeout);
                Assert.Equal("MyError", message.Error.String);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(TimeSpan.FromMinutes(120), message.DecisionTimeout);
                Assert.Equal("MyError", message.Error.String);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestWorkflowInvokeRequest()
        {
            WorkflowInvokeRequest message;

            using (var stream = new MemoryStream())
            {
                message = new WorkflowInvokeRequest();

                Assert.Equal(MessageTypes.WorkflowExecuteReply, message.ReplyType);

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowInvokeRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Equal(0, message.WorkflowContextId);

                // Round-trip

                message.RequestId = 555;
                message.WorkflowContextId = 666;
                message.Name = "Foo";
                message.Args = new Dictionary<string, object>() { { "hello", "world!" } };
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.WorkflowContextId);
                Assert.Equal("Foo", message.Name);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowInvokeRequest>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.WorkflowContextId);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);

                // Echo the message via the connection's web server and verify.

                message = EchoToConnection(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.WorkflowContextId);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);

                // Echo the message via the associated [cadence-proxy] and verify.

                message = EchoToProxy(message);
                Assert.NotNull(message);
                Assert.Equal(555, message.RequestId);
                Assert.Equal(666, message.WorkflowContextId);
                Assert.Equal("Foo", message.Name);
                Assert.Equal("world!", message.Args["hello"]);
            }
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCadence)]
        public void TestWorkflowInvokeReply()
        {
            WorkflowInvokeReply message;

            using (var stream = new MemoryStream())
            {
                message = new WorkflowInvokeReply();

                // Empty message.

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowInvokeReply>(stream, ignoreTypeCode: true);
                Assert.NotNull(message);
                Assert.Equal(0, message.RequestId);
                Assert.Null(message.Result);
                Assert.Null(message.Error);

                // Round-trip

                message.RequestId = 555;
                Assert.Equal(555, message.RequestId);
                message.Error = new CadenceError("MyError");
                Assert.Equal("MyError", message.Error.String);

                stream.SetLength(0);
                stream.Write(message.Serialize(ignoreTypeCode: true));
                stream.Seek(0, SeekOrigin.Begin);

                message = ProxyMessage.Deserialize<WorkflowInvokeReply>(stream, ignoreTypeCode: true);
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
    }
}
