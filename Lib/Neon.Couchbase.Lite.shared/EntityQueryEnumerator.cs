﻿//-----------------------------------------------------------------------------
// FILE:	    EntityQueryEnumerator.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Couchbase.Dynamic;

namespace Couchbase.Lite
{
    /// <summary>
    /// An entity implementation of <see cref="QueryEnumerator"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class enumerates the document query results as <see cref="EntityQueryRow{TEntity}"/>
    /// instances that expose read-only entity documents.  The lower-level <see cref="QueryEnumerator"/>
    /// can be accessed via the <see cref="Base"/> property.
    /// </para>
    /// </remarks>
    /// <threadsafety instance="false"/>
    public sealed class EntityQueryEnumerator<TEntity> : IDisposable, IEnumerable<EntityQueryRow<TEntity>>
        where TEntity : class, IDynamicEntity, new()
    {
        private Func<EntityQueryRow<TEntity>, bool>   postFilter;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="queryEnumerator">The base query enumerator.</param>
        /// <param name="postFilter">The post row folder or <c>null</c>.</param>
        internal EntityQueryEnumerator(QueryEnumerator queryEnumerator, Func<EntityQueryRow<TEntity>, bool> postFilter)
        {
            Covenant.Requires<ArgumentNullException>(queryEnumerator != null);

            this.Base       = queryEnumerator;
            this.postFilter = postFilter;
        }

        /// <summary>
        /// Returns the wrapped <see cref="QueryEnumerator"/>.
        /// </summary>
        public QueryEnumerator Base { get; private set; }

        //---------------------------------------------------------------------
        // IDisposable implementation

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Base != null)
            {
                Base.Dispose();
                Base = null;
            }
        }

        //---------------------------------------------------------------------
        // IEnumerable<T> implementation.

        /// <inheritdoc/>
        public IEnumerator<EntityQueryRow<TEntity>> GetEnumerator()
        {
            foreach (var row in Base)
            {
                var entityRowRow = new EntityQueryRow<TEntity>(row);

                if (postFilter != null && !postFilter(entityRowRow))
                {
                    continue;
                }

                yield return entityRowRow;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var row in Base)
            {
                var entityRow = new EntityQueryRow<TEntity>(row);

                if (postFilter != null && !postFilter(entityRow))
                {
                    continue;
                }

                yield return entityRow;
            }
        }
    }
}
