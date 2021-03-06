﻿//-----------------------------------------------------------------------------
// FILE:	    WorkflowExecuteReply.cs
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

namespace Neon.Cadence.Internal
{
    /// <summary>
    /// <b>proxy --> library:</b> Answers a <see cref="WorkflowExecuteRequest"/>
    /// </summary>
    [ProxyMessage(MessageTypes.WorkflowExecuteReply)]
    internal class WorkflowExecuteReply : ProxyReply
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public WorkflowExecuteReply()
        {
            Type = MessageTypes.WorkflowExecuteReply;
        }

        /// <summary>
        /// Returns the workflow's <b>TaskStartToCloseTimeout</b>.
        /// </summary>
        public TimeSpan DecisionTimeout
        {
            get => GetTimeSpanProperty("DecisionTimeout");
            set => SetTimeSpanProperty("DecisionTimeout", value);
        }

        /// <inheritdoc/>
        internal override ProxyMessage Clone()
        {
            var clone = new WorkflowExecuteReply();

            CopyTo(clone);

            return clone;
        }

        /// <inheritdoc/>
        protected override void CopyTo(ProxyMessage target)
        {
            base.CopyTo(target);

            var typedTarget = (WorkflowExecuteReply)target;

            typedTarget.DecisionTimeout = this.DecisionTimeout;
        }
    }
}
