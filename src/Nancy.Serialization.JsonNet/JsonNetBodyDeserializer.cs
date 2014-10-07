namespace Nancy.Serialization.JsonNet
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Nancy.Extensions;
    using Nancy.ModelBinding;
    using Newtonsoft.Json;

    public class JsonNetBodyDeserializer : IBodyDeserializer
    {
        private readonly JsonSerializer serializer;

        /// <summary>
        /// Empty constructor if no converters are needed
        /// </summary>
        public JsonNetBodyDeserializer()
        {
            this.serializer = JsonSerializer.CreateDefault();
        }

        /// <summary>
        /// Constructor to use when a custom serializer are needed.
        /// </summary>
        /// <param name="serializer">Json serializer used when deserializing.</param>
        public JsonNetBodyDeserializer(JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        /// <summary>
        /// Whether the deserializer can deserialize the content type
        /// </summary>
        /// <param name="contentType">Content type to deserialize</param>
        /// <param name="context">Current <see cref="BindingContext"/>.</param>
        /// <returns>True if supported, false otherwise</returns>
        public bool CanDeserialize(string contentType, BindingContext context)
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
            var deserializedObject =
                this.serializer.Deserialize(new StreamReader(bodyStream), context.DestinationType);

            if (!context.DestinationType.IsCollection())
            {
                var existingInstance = false;
                foreach (var property in context.ValidModelProperties)
                {
                    var existingValue = property.GetValue(context.Model, null);

                    if (!IsDefaultValue(existingValue, property.PropertyType))
                    {
                        existingInstance = true;
                        break;
                    }
                }

                if (existingInstance)
                {
                    foreach (var property in context.ValidModelProperties)
                    {
                        var existingValue = property.GetValue(context.Model, null);

                        if (IsDefaultValue(existingValue, property.PropertyType))
                        {
                            CopyPropertyValue(property, deserializedObject, context.Model);
                        }
                    }

                    return context.Model;
                }
            }

            if (context.DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Except(context.ValidModelProperties).Any())
            {
                return CreateObjectWithBlacklistExcluded(context, deserializedObject);
            }

            return deserializedObject;
        }

        private static object ConvertCollection(object items, Type destinationType, BindingContext context)
        {
            var returnCollection = Activator.CreateInstance(destinationType);

            var collectionAddMethod =
                destinationType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

            foreach (var item in (IEnumerable)items)
            {
                collectionAddMethod.Invoke(returnCollection, new[] { item });
            }

            return returnCollection;
        }

        private static object CreateObjectWithBlacklistExcluded(BindingContext context, object deserializedObject)
        {
            var returnObject = Activator.CreateInstance(context.DestinationType);

            if (context.DestinationType.IsCollection())
            {
                return ConvertCollection(deserializedObject, context.DestinationType, context);
            }

            foreach (var property in context.ValidModelProperties)
            {
                CopyPropertyValue(property, deserializedObject, returnObject);
            }

            return returnObject;
        }

        private static void CopyPropertyValue(PropertyInfo property, object sourceObject, object destinationObject)
        {
            property.SetValue(destinationObject, property.GetValue(sourceObject, null), null);
        }

        private static bool IsDefaultValue(object existingValue, Type propertyType)
        {
            return propertyType.IsValueType
                ? Equals(existingValue, Activator.CreateInstance(propertyType))
                : existingValue == null;
        }
    }
}