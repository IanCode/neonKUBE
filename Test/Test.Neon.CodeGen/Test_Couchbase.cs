﻿//-----------------------------------------------------------------------------
// FILE:	    Test_Couchbase.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using Couchbase;
using Couchbase.Core;
using Couchbase.Linq;

using Neon.CodeGen;
using Neon.Common;
using Neon.Xunit;
using Neon.Xunit.Couchbase;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit;

using Test.Neon.Models;

namespace TestCodeGen.Couchbase
{
    public class PlainOldObject
    {
        public string Foo { get; set; }
    }

    public class Test_Couchbase : IClassFixture<CouchbaseFixture>
    {
        private const string username = "Administrator";
        private const string password = "password";

        private CouchbaseFixture    couchbase;
        private NeonBucket          bucket;

        public Test_Couchbase(CouchbaseFixture couchbase)
        {
            this.couchbase = couchbase;

            couchbase.Start();

            bucket = couchbase.Bucket;
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCodeGen)]
        public async Task WriteReadList()
        {
            // Verify that we can write generated entity models.

            var jack = new Person()
            {
                Id = 0,
                Name = "Jack",
                Age = 10,
                Data = new byte[] { 0, 1, 2, 3, 4 }
            };

            var jill = new Person()
            {
                Id = 1,
                Name = "Jill",
                Age = 11,
                Data = new byte[] { 5, 6, 7, 8, 9 }
            };

            Assert.Equal("0", jack.GetKey());
            Assert.Equal("1", jill.GetKey());

            await bucket.InsertSafeAsync(jack, persistTo: PersistTo.One);
            await bucket.InsertSafeAsync(jill, persistTo: PersistTo.One);

            // Verify that we can read them.

            var jackRead = await bucket.GetSafeAsync<Person>(0.ToString());
            var jillRead = await bucket.GetSafeAsync<Person>(1.ToString());

            Assert.Equal("0", jackRead.GetKey());
            Assert.Equal("1", jillRead.GetKey());
            Assert.True(jack == jackRead);
            Assert.True(jill == jillRead);

            //-----------------------------------------------------------------
            // Persist a [City] entity (which has a different entity type) and then
            // perform a N1QL query to list the Person entities and verify that we
            // get only Jack and Jill back.  This verifies the the [TypeFilter] 
            // attribute is generated and working correctly.

            var city = new City()
            {
                Name = "Woodinville",
                Population = 12345
            };

            var opResult = await bucket.InsertSafeAsync(city, persistTo: PersistTo.One);

            var context     = new BucketContext(bucket);
            var peopleQuery = from doc in context.Query<Person>() select doc;

            // $todo(jeff.lill):
            //
            // I need to figure out how to have the query honor the mutation state
            // returned in [opResult].  I'm going to hack a delay here in the meantime.
            //
            //      https://github.com/nforgeio/neonKUBE/issues/473

            await Task.Delay(TimeSpan.FromSeconds(2));

            var people = peopleQuery.ToList();

            Assert.Equal(2, people.Count);
            Assert.Contains(people, p => p.Name == "Jack");
            Assert.Contains(people, p => p.Name == "Jill");

            //-----------------------------------------------------------------
            // Query for the city and verify.

            var cityQuery = from doc in context.Query<City>() select doc;
            var cities    = cityQuery.ToList();

            Assert.Single(cities);
            Assert.Contains(cities, p => p.Name == "Woodinville");

            //-----------------------------------------------------------------
            // Verify that plain old object serialization still works.

            var poo = new PlainOldObject() { Foo = "bar" };

            await bucket.InsertSafeAsync("poo", poo, persistTo: PersistTo.One);

            poo = await bucket.GetSafeAsync<PlainOldObject>("poo");

            Assert.Equal("bar", poo.Foo);

            //-----------------------------------------------------------------
            // Extra credit #1: Verify that [PersistedEntity.DeepClone()] works.

            var clone = jack.DeepClone();

            Assert.Equal(jack.Name, clone.Name);
            Assert.Equal(jack.Age, clone.Age);
            Assert.NotSame(jack.Data, clone.Data);

            //-----------------------------------------------------------------
            // Extra credit #2: Verify that [SameTypeAs()] works.

            Assert.True(Person.SameTypeAs(jack));
            Assert.False(Person.SameTypeAs(city));
            Assert.False(Person.SameTypeAs(null));
        }
    }
}