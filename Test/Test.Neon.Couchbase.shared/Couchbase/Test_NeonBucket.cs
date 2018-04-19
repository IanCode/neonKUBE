﻿//-----------------------------------------------------------------------------
// FILE:	    Test_CbHelper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Couchbase;

using Neon.Common;

using Xunit;

namespace TestCouchbase
{
    public class Test_NeonBucket : IClassFixture<CouchbaseFixture>
    {
        private CouchbaseFixture    fixture;
        private NeonBucket          bucket;

        public Test_NeonBucket(CouchbaseFixture fixture)
        {
            this.fixture = fixture;

            fixture.Initialize(
                () =>
                {
                    bucket = fixture.Start();
                });
        }

        /// <summary>
        /// Verify that we can access the Couchbase bucket.
        /// </summary>
        [Fact]
        public async Task BasicAsync()
        {
            // Basic test to verify that we can put/get/remove a document.

            await bucket.UpsertSafeAsync("hello", "world!");
            Assert.Equal("world!", await bucket.GetSafeAsync<string>("hello"));
            await bucket.RemoveSafeAsync("hello");
            Assert.Null(await bucket.FindSafeAsync<string>("hello"));
        }
    }
}