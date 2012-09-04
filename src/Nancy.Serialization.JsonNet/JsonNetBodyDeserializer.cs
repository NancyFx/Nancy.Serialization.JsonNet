namespace Nancy.Serialization.JsonNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Nancy.ModelBinding;
    using Newtonsoft.Json;

    public class JsonNetBodyDeserializer : IBodyDeserializer
    {
        private readonly JsonSerializer _jsonSerializer = new JsonSerializer();
        
        /// <summary>
        /// Empty constructor if no converters are needed
        /// </summary>
        public JsonNetBodyDeserializer(){}

        /// <summary>
        /// Constructor to use when json converters are needed.
        /// </summary>
        /// <param name="converters">Json converters used when deserializing.</param>
        public JsonNetBodyDeserializer(IEnumerable<JsonConverter> converters)
        {
            foreach (var converter in converters)
                _jsonSerializer.Converters.Add(converter);
        }

        /// <summary>
        /// Whether the deserializer can deserialize the content type
        /// </summary>
        /// <param name="contentType">Content type to deserialize</param>
        /// <returns>True if supported, false otherwise</returns>
        public bool CanDeserialize(string contentType)
        {
            return Helpers.IsJsonType(contentType);
        }

        /// <summary>
        /// Deserialize the request body to a model
        /// </summary>
        /// <param name="contentType">Content type to deserialize</param>
        /// <param name="bodyStream">Request body stream</param>
        /// <param name="context">Current context</param>
        /// <returns>Model instance</returns>
        public object Deserialize(string contentType, Stream bodyStream, BindingContext context)
        {
            var deserializedObject = _jsonSerializer.Deserialize(new StreamReader(bodyStream), context.DestinationType);
            
            if (context.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Except(context.ValidModelProperties).Any())
            {
                return this.CreateObjectWithBlacklistExcluded(context, deserializedObject);
            }

            return deserializedObject;
        }

        private object CreateObjectWithBlacklistExcluded(BindingContext context, object deserializedObject)
        {
            var returnObject = Activator.CreateInstance(context.DestinationType);

            foreach (var property in context.ValidModelProperties)
            {
                this.CopyPropertyValue(property, deserializedObject, returnObject);
            }

            return returnObject;
        }

        private void CopyPropertyValue(PropertyInfo property, object sourceObject, object destinationObject)
        {
            property.SetValue(destinationObject, property.GetValue(sourceObject, null), null);
        }
    }
}