﻿//-----------------------------------------------------------------------------
// FILE:	    KubeException.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2019 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Neon.Common;

namespace Neon.Kube
{
    /// <summary>
    /// Describes a cluster related related error.
    /// </summary>
    public class KubeException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public KubeException()
        {
        }

        /// <summary>
        /// Constructs and instance with a message and an optional inner exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The optional inner exception.</param>
        public KubeException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}