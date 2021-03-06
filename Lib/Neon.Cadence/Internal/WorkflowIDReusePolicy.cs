﻿//-----------------------------------------------------------------------------
// FILE:	    WorkflowIDReusePolicy.cs
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
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Neon.Cadence;
using Neon.Common;
using Neon.Retry;
using Neon.Time;

namespace Neon.Cadence.Internal
{
    /// <summary>
    /// Enumerates the workflow ID reuse policies.
    /// </summary>
    internal static class WorkflowIDReusePolicy
    {
        /// <summary>
        /// WorkflowIDReusePolicyAllowDuplicateFailedOnly allow start a workflow execution
        /// when workflow not running, and the last execution close state is in
        /// [terminated, cancelled, timeouted, failed].
        /// </summary>
        public const int WorkflowIDReusePolicyAllowDuplicateFailedOnly = 0;

        /// <summary>
        /// WorkflowIDReusePolicyAllowDuplicate allow start a workflow execution using
        /// the same workflow ID,when workflow not running.
        /// </summary>
        public const int WorkflowIDReusePolicyAllowDuplicate = 1;

        /// <summary>
        /// WorkflowIDReusePolicyRejectDuplicate do not allow start a workflow execution
        /// using the same workflow ID at all.
        /// </summary>
        public const int orkflowIDReusePolicyRejectDuplicate = 2;
    }
}
