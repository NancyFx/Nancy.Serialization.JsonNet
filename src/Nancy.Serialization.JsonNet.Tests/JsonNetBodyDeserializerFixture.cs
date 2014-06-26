namespace Nancy.Serialization.JsonNet.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Nancy.ModelBinding;
    using Newtonsoft.Json;
    using Xunit;

    public class JsonNetBodyDeserializerFixture
    {
        public class TestData
        {
            private TestData()
            {
            }

            public TestData(string randomStuff)
            {
                // Should never get here as it should use the NonPublicDefaultConstructor first.
                if (randomStuff == null)
                    throw new ArgumentNullException("randomStuff");
            }

            public string SomeString { get; set; }

            public Guid SomeGuid { get; set; }
        }

        [Fact]
        public void when_deserializing()
        {
            // Given
            JsonConvert.DefaultSettings = JsonNetSerializerFixture.GetJsonSerializerSettings;

            var guid = Guid.NewGuid();
            string source = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\"}}", guid);

            var context = new BindingContext
            {
                DestinationType = typeof (TestData),
                ValidModelProperties = typeof (TestData).GetProperties(BindingFlags.Public | BindingFlags.Instance),
            };

            // When
            object actual;
            using (var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            {
                IBodyDeserializer sut = new JsonNetBodyDeserializer();
                actual = sut.Deserialize("application/json", bodyStream, context);
            }

            // Then
            var actualData = Assert.IsType<TestData>(actual);
            Assert.Equal("some string value", actualData.SomeString);
            Assert.Equal(guid, actualData.SomeGuid);
        }
    }
}
