namespace Nancy.Serialization.JsonNet
{
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;

    public class JsonNetJsonSerializer : ISerializer
    {
        private readonly JsonSerializer serializer = new JsonSerializer();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetJsonSerializer"/> class.
        /// </summary>
        public JsonNetJsonSerializer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetJsonSerializer"/> class,
        /// with the provided <paramref name="converters"/>.
        /// </summary>
        /// <param name="converters">Json converters used when serializing.</param>
        public JsonNetJsonSerializer(IEnumerable<JsonConverter> converters)
        {
            foreach (var converter in converters)
            {
                this.serializer.Converters.Add(converter);
            }
        }

        /// <summary>
        /// Whether the serializer can serialize the content type
        /// </summary>
        /// <param name="contentType">Content type to serialise</param>
        /// <returns>True if supported, false otherwise</returns>
        public bool CanSerialize(string contentType)
        {
            return Helpers.IsJsonType(contentType);
        }

        /// <summary>
        /// Gets the list of extensions that the serializer can handle.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> of extensions if any are available, otherwise an empty enumerable.</value>
        public IEnumerable<string> Extensions
        {
            get { yield return "json"; }
        }

        /// <summary>
        /// Serialize the given model with the given contentType
        /// </summary>
        /// <param name="contentType">Content type to serialize into</param>
        /// <param name="model">Model to serialize</param>
        /// <param name="outputStream">Output stream to serialize to</param>
        /// <returns>Serialised object</returns>
        public void Serialize<TModel>(string contentType, TModel model, Stream outputStream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(outputStream)))
            {
                this.serializer.Serialize(writer, model);
                writer.Flush();
            }
        }
    }
}