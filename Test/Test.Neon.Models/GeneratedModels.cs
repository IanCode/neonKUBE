//-----------------------------------------------------------------------------
// This file was generated by the [Neon.CodeGen] library.  Any
// manual changes will be lost when the file is regenerated.

#pragma warning disable 0108     // Disable property overrides without new warnings
#pragma warning disable 0168     // Disable declared but never used warnings
#pragma warning disable 1591     // Disable missing comment warnings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Neon.Collections;
using Neon.Common;
using Neon.Diagnostics;
using Neon.Net;
using Neon.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Test.Models
{

    //-------------------------------------------------------------------------
    // From: Test.Neon.Models.Definitions.BaseModel

    public partial class BaseModel : IGeneratedDataModel
    {
        //---------------------------------------------------------------------
        // Static members:

        /// <summary>
        /// Deserializes an instance from JSON text.
        /// </summary>
        /// <param name="jsonText">The JSON text input.</param>
        /// <returns>The deserialized <see cref="BaseModel"/>.</returns>
        public static BaseModel CreateFrom(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                throw new ArgumentNullException(nameof(jsonText));
            }

            var model = new BaseModel(SerializationHelper.Deserialize<JObject>(jsonText));

            model.__Load();
            return model;
        }

        /// <summary>
        /// Deserializes an instance from a <see cref="JObject"/>.
        /// </summary>
        /// <param name="jObject">The input <see cref="JObject"/>.</param>
        /// <returns>The deserialized <see cref="BaseModel"/>.</returns>
        public static BaseModel CreateFrom(JObject jObject)
        {
            if (jObject == null)
            {
                throw new ArgumentNullException(nameof(jObject));
            }

            var model = new BaseModel(jObject);

            model.__Load();
            return model;
        }

        /// <summary>
        /// Deserializes an instance from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The input <see cref="Stream"/>.</param>
        /// <param name="encoding">Optionally specifies the inout encoding.  This defaults to <see cref="Encoding.UTF8"/>.</param>
        /// <returns>The deserialized <see cref="BaseModel"/>.</returns>
        public static BaseModel CreateFrom(Stream stream, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            BaseModel model;

            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true))
            {
                model = BaseModel.CreateFrom(reader.ReadToEnd());
            }

            model.__Load();
            return model;
        }


        /// <summary>
        /// Deserializes an instance from a <see cref="JsonResponse"/>.
        /// </summary>
        /// <param name="response">The input <see cref="JsonResponse"/>.</param>
        /// <returns>The deserialized <see cref="BaseModel"/>.</returns>
        public static BaseModel CreateFrom(JsonResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return CreateFrom(response.JsonText);
        }

        /// <summary>
        /// Compares two instances for equality by performing a deep comparision of all object
        /// properties including any hidden properties.  Note that you may pass <c>null</c>.
        /// </summary>
        /// <param name="value1">The first value or <c>null</c>.</param>
        /// <param name="value2">The second value or <c>null</c>.</param>
        /// <returns><c>true</c> if the values are equal.</returns>
        public static bool operator ==(BaseModel value1, BaseModel value2)
        {
            var value1IsNull = object.ReferenceEquals(value1, null);
            var value2IsNull = object.ReferenceEquals(value2, null);

            if (value1IsNull == value2IsNull)
            {
                if (value1IsNull)
                {
                    return true;
                }
                else
                {
                    return value1.Equals(value2);
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two instances for inequality by performing a deep comparision of all object
        /// properties including any hidden properties.  Note that you may pass <c>null</c>.
        /// </summary>
        /// <param name="value1">The first value or <c>null</c>.</param>
        /// <param name="value2">The second value or <c>null</c>.</param>
        /// <returns><c>true</c> if the values are not equal.</returns>
        public static bool operator !=(BaseModel value1, BaseModel value2)
        {
            return !(value1 == value2);
        }

        //---------------------------------------------------------------------
        // Instance members:

        protected JObject __JObject { get; set; }

        /// <summary>
        /// Constructs an uninitialized instance.
        /// </summary>
        public BaseModel()
        {
            __JObject = new JObject();
        }

        protected BaseModel(JObject jObject)
        {
            __JObject = jObject;
        }

        [JsonProperty(PropertyName = "ParentProperty", DefaultValueHandling = DefaultValueHandling.Include, Order = 2147483647)]
        public string ParentProperty { get; set; }

        protected virtual void __Load()
        {
            JProperty property;

            lock (__JObject)
            {
                property = this.__JObject.Property("ParentProperty");
                if (property != null)
                {
                    this.ParentProperty = (string)property.Value;
                }
            }
        }

        protected virtual void __Save()
        {
            JProperty property;

            lock (__JObject)
            {
                this.__JObject["ParentProperty"] = this.ParentProperty;
            }
        }

        /// <summary>
        /// Renders the instance as JSON text.
        /// </summary>
        /// <returns>The serialized JSON string.</returns>
        public override string ToString()
        {
            __Save();
            return SerializationHelper.Serialize(__JObject, Formatting.None);
        }

        /// <summary>
        /// Renders the instance as JSON text, optionally formatting the output.
        /// </summary>
        /// <param name="indent">Optionally pass <c>true</c> to format the output.</param>
        /// <returns>The serialized JSON string.</returns>
        public string ToString(bool indented)
        {
            __Save();
            return SerializationHelper.Serialize(__JObject, indented ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Renders the instances as a <see cref="JObject"/>.
        /// </summary>
        /// <returns>The cloned <see cref="JObject"/>.</returns>
        public JObject ToJObject()
        {
            __Save();
            return (JObject)__JObject.DeepClone();
        }

        /// <summary>
        /// Returns a deep clone of the instance.
        /// </summary>
        /// <returns>The cloned instance.</returns>
        public BaseModel DeepClone()
        {
            lock (__JObject)
            {
                __Save();
                return CreateFrom((JObject)__JObject.DeepClone());
            }
        }

        /// <summary>
        /// Used to convert a base data model class into a derived class.
        /// </summary>
        /// <typeparam name="T">The desired derived type.</typeparam>
        /// <param name="noClone">
        /// By default, this method will create a deep clone of the underlying <see cref="JObject"/>
        /// and use this new instance when constructing the new object.  This is the safest
        /// approach but will cause a performance hit.  You can pass <paramref name="noClone"/><c>=true</c>
        /// to reuse the existing <see cref="JObject"/> for the new instance if you're sure that the
        /// original instance will no longer be accessed.
        /// </param>
        /// <returns>The converted instance of type <typeparamref name="T"/>.</returns>
        public BaseModel ToDerived<T>(bool noClone = false)
           where T : BaseModel, IGeneratedDataModel
        {
            lock (__JObject)
            {
                __Save();
                return GeneratedClassFactory.CreateFrom<T>(noClone ? __JObject : (JObject)__JObject.DeepClone());
            }
        }

        /// <summary>
        /// Determines whether the current instance equals another object.
        /// </summary>
        /// <param name="obj">The other object instance or <c>null</c>.</param>
        /// <returns><c>true</c> if the object reference equals the current instance.</returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as BaseModel;

            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            lock (this.__JObject)
            {
                lock (other.__JObject)
                {
                    this.__Save();
                    other.__Save();
                    return JObject.DeepEquals(this.__JObject, other.__JObject);
                }
            }
        }

        /// <summary>
        /// Calculates the hash code for the instance.
        /// <note>
        /// At least one of the class properties must be tagged with a <b>[HashSource]</b>
        /// for this to work.
        /// </note>
        /// </summary>
        /// <returns>The calculated hash code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no class properties are tagged with <see cref="HashSourceAttribute"/>.</exception>
        public override int GetHashCode()
        {
            var hashCode = 0;

            if (this.ParentProperty != null) { hashCode ^= this.ParentProperty.GetHashCode(); }

            return hashCode;
        }
    }

    //-------------------------------------------------------------------------
    // From: Test.Neon.Models.Definitions.DerivedModel

    public partial class DerivedModel : BaseModel, IGeneratedDataModel
    {
        //---------------------------------------------------------------------
        // Static members:

        /// <summary>
        /// Deserializes an instance from JSON text.
        /// </summary>
        /// <param name="jsonText">The JSON text input.</param>
        /// <returns>The deserialized <see cref="DerivedModel"/>.</returns>
        public static DerivedModel CreateFrom(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                throw new ArgumentNullException(nameof(jsonText));
            }

            var model = new DerivedModel(SerializationHelper.Deserialize<JObject>(jsonText));

            model.__Load();
            return model;
        }

        /// <summary>
        /// Deserializes an instance from a <see cref="JObject"/>.
        /// </summary>
        /// <param name="jObject">The input <see cref="JObject"/>.</param>
        /// <returns>The deserialized <see cref="DerivedModel"/>.</returns>
        public static DerivedModel CreateFrom(JObject jObject)
        {
            if (jObject == null)
            {
                throw new ArgumentNullException(nameof(jObject));
            }

            var model = new DerivedModel(jObject);

            model.__Load();
            return model;
        }

        /// <summary>
        /// Deserializes an instance from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The input <see cref="Stream"/>.</param>
        /// <param name="encoding">Optionally specifies the inout encoding.  This defaults to <see cref="Encoding.UTF8"/>.</param>
        /// <returns>The deserialized <see cref="DerivedModel"/>.</returns>
        public static DerivedModel CreateFrom(Stream stream, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            DerivedModel model;

            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true))
            {
                model = DerivedModel.CreateFrom(reader.ReadToEnd());
            }

            model.__Load();
            return model;
        }


        /// <summary>
        /// Deserializes an instance from a <see cref="JsonResponse"/>.
        /// </summary>
        /// <param name="response">The input <see cref="JsonResponse"/>.</param>
        /// <returns>The deserialized <see cref="DerivedModel"/>.</returns>
        public static DerivedModel CreateFrom(JsonResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return CreateFrom(response.JsonText);
        }

        /// <summary>
        /// Compares two instances for equality by performing a deep comparision of all object
        /// properties including any hidden properties.  Note that you may pass <c>null</c>.
        /// </summary>
        /// <param name="value1">The first value or <c>null</c>.</param>
        /// <param name="value2">The second value or <c>null</c>.</param>
        /// <returns><c>true</c> if the values are equal.</returns>
        public static bool operator ==(DerivedModel value1, DerivedModel value2)
        {
            var value1IsNull = object.ReferenceEquals(value1, null);
            var value2IsNull = object.ReferenceEquals(value2, null);

            if (value1IsNull == value2IsNull)
            {
                if (value1IsNull)
                {
                    return true;
                }
                else
                {
                    return value1.Equals(value2);
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two instances for inequality by performing a deep comparision of all object
        /// properties including any hidden properties.  Note that you may pass <c>null</c>.
        /// </summary>
        /// <param name="value1">The first value or <c>null</c>.</param>
        /// <param name="value2">The second value or <c>null</c>.</param>
        /// <returns><c>true</c> if the values are not equal.</returns>
        public static bool operator !=(DerivedModel value1, DerivedModel value2)
        {
            return !(value1 == value2);
        }

        //---------------------------------------------------------------------
        // Instance members:

        /// <summary>
        /// Constructs an uninitialized instance.
        /// </summary>
        public DerivedModel() : base()
        {
        }

        protected DerivedModel(JObject jObject) : base(jObject)
        {
        }

        [JsonProperty(PropertyName = "ChildProperty", DefaultValueHandling = DefaultValueHandling.Include, Order = 2147483647)]
        public string ChildProperty { get; set; }

        protected override void __Load()
        {
            JProperty property;

            lock (__JObject)
            {
                base.__Load();

                property = this.__JObject.Property("ChildProperty");
                if (property != null)
                {
                    this.ChildProperty = (string)property.Value;
                }
            }
        }

        protected override void __Save()
        {
            JProperty property;

            lock (__JObject)
            {
                base.__Save();

                this.__JObject["ChildProperty"] = this.ChildProperty;
            }
        }

        /// <summary>
        /// Renders the instance as JSON text.
        /// </summary>
        /// <returns>The serialized JSON string.</returns>
        public override string ToString()
        {
            __Save();
            return SerializationHelper.Serialize(__JObject, Formatting.None);
        }

        /// <summary>
        /// Renders the instance as JSON text, optionally formatting the output.
        /// </summary>
        /// <param name="indent">Optionally pass <c>true</c> to format the output.</param>
        /// <returns>The serialized JSON string.</returns>
        public string ToString(bool indented)
        {
            __Save();
            return SerializationHelper.Serialize(__JObject, indented ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Returns a deep clone of the instance.
        /// </summary>
        /// <returns>The cloned instance.</returns>
        public DerivedModel DeepClone()
        {
            lock (__JObject)
            {
                __Save();
                return CreateFrom((JObject)__JObject.DeepClone());
            }
        }

        /// <summary>
        /// Used to convert a base data model class into a derived class.
        /// </summary>
        /// <typeparam name="T">The desired derived type.</typeparam>
        /// <param name="noClone">
        /// By default, this method will create a deep clone of the underlying <see cref="JObject"/>
        /// and use this new instance when constructing the new object.  This is the safest
        /// approach but will cause a performance hit.  You can pass <paramref name="noClone"/><c>=true</c>
        /// to reuse the existing <see cref="JObject"/> for the new instance if you're sure that the
        /// original instance will no longer be accessed.
        /// </param>
        /// <returns>The converted instance of type <typeparamref name="T"/>.</returns>
        public DerivedModel ToDerived<T>(bool noClone = false)
           where T : DerivedModel, IGeneratedDataModel
        {
            lock (__JObject)
            {
                __Save();
                return GeneratedClassFactory.CreateFrom<T>(noClone ? __JObject : (JObject)__JObject.DeepClone());
            }
        }

        /// <summary>
        /// Determines whether the current instance equals another object.
        /// </summary>
        /// <param name="obj">The other object instance or <c>null</c>.</param>
        /// <returns><c>true</c> if the object reference equals the current instance.</returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as DerivedModel;

            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            lock (this.__JObject)
            {
                lock (other.__JObject)
                {
                    this.__Save();
                    other.__Save();
                    return JObject.DeepEquals(this.__JObject, other.__JObject);
                }
            }
        }

        /// <summary>
        /// Calculates the hash code for the instance.
        /// <note>
        /// At least one of the class properties must be tagged with a <b>[HashSource]</b>
        /// for this to work.
        /// </note>
        /// </summary>
        /// <returns>The calculated hash code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no class properties are tagged with <see cref="HashSourceAttribute"/>.</exception>
        public override int GetHashCode()
        {
            var hashCode = 0;

            if (this.ChildProperty != null) { hashCode ^= this.ChildProperty.GetHashCode(); }
            if (this.ParentProperty != null) { hashCode ^= this.ParentProperty.GetHashCode(); }

            return hashCode;
        }
    }
}
