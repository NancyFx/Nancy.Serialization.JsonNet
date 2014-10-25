namespace Nancy.Serialization.JsonNet
{
    using System;

    using Nancy.Json;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal static class Helpers
    {
        public static JsonSerializer DefaultSerializer()
        {
            JsonSerializerSettings settings;

            if (JsonConvert.DefaultSettings != null)
            {
                settings = JsonConvert.DefaultSettings();
            }
            else
            {
                settings = new JsonSerializerSettings();
            }

            settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;

            settings.ContractResolver = JsonSettings.RetainCasing
                ? new DefaultContractResolver()
                : new CamelCasePropertyNamesContractResolver();

            settings.DateFormatHandling = JsonSettings.ISO8601DateFormat
                ? DateFormatHandling.IsoDateFormat
                : DateFormatHandling.MicrosoftDateFormat;

            return JsonSerializer.Create(settings);
        }

        /// <summary>
        /// Attempts to detect if the content type is JSON.
        /// Supports:
        ///   application/json
        ///   text/json
        ///   application/vnd[something]+json
        /// Matches are case insentitive to try and be as "accepting" as possible.
        /// </summary>
        /// <param name="contentType">Request content type</param>
        /// <returns>True if content type is JSON, false otherwise</returns>
        public static bool IsJsonType(string contentType)
        {
            if (String.IsNullOrEmpty(contentType))
            {
                return false;
            }

            var contentMimeType = contentType.Split(';')[0];

            return contentMimeType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase) ||
                   contentMimeType.StartsWith("application/json-", StringComparison.InvariantCultureIgnoreCase) ||
                   contentMimeType.Equals("text/json", StringComparison.InvariantCultureIgnoreCase) ||
                  (contentMimeType.StartsWith("application/vnd", StringComparison.InvariantCultureIgnoreCase) &&
                   contentMimeType.EndsWith("+json", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}