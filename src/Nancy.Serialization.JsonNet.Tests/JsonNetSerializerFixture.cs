namespace Nancy.Serialization.JsonNet.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Xunit;

    public class JsonNetSerializerFixture
    {
        [Fact]
        public void when_serializing()
        {
            // Given
            JsonConvert.DefaultSettings = GetJsonSerializerSettings;

            var guid = Guid.NewGuid();
            var data = new { SomeString = "some string value", SomeGuid = guid, NullValue = default(Uri) };
            string expected = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\"}}", guid);

            // When
            string actual;
            using (var stream = new MemoryStream())
            {
                ISerializer sut = new JsonNetSerializer();
                sut.Serialize("application/json", data, stream);
                actual = Encoding.UTF8.GetString(stream.ToArray());
            }

            // Then
            Assert.Equal(expected, actual);
        }

        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            };
        }
    }
}
