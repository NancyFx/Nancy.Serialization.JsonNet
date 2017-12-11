namespace Nancy.Serialization.JsonNet.Tests
{
    using System;
    using System.IO;
    using System.Linq;
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
                ValidModelBindingMembers = typeof (TestData).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => new BindingMemberInfo(p)),
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

        [Fact]
        public void when_deserializing_while_the_body_stream_was_not_at_position_zero()
        {
            // Repro of https://github.com/NancyFx/Nancy.Serialization.JsonNet/issues/22

            // Given
            JsonConvert.DefaultSettings = JsonNetSerializerFixture.GetJsonSerializerSettings;

            var guid = Guid.NewGuid();
            string source = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\"}}", guid);

            var context = new BindingContext
            {
                DestinationType = typeof(TestData),
                ValidModelBindingMembers = typeof(TestData).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => new BindingMemberInfo(p)),
            };

            // When
            object actual;
            using (var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            {
                IBodyDeserializer sut = new JsonNetBodyDeserializer();
                bodyStream.Position = 1;
                actual = sut.Deserialize("application/json", bodyStream, context);
            }

            // Then
            var actualData = Assert.IsType<TestData>(actual);
            Assert.Equal("some string value", actualData.SomeString);
            Assert.Equal(guid, actualData.SomeGuid);
        }

        [Fact]
        public void when_deserializing_directly_to_array_of_string()
        {
            // Given
            string source = "['first', 'second', 'third']";

            var context = new BindingContext
            {
                DestinationType = typeof(string[]),
                ValidModelBindingMembers = new BindingMemberInfo[0],
            };

            // When
            object actual;
            using (var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            {
                IBodyDeserializer sut = new JsonNetBodyDeserializer();
                actual = sut.Deserialize("application/json", bodyStream, context);
            }

            // Then
            var actualData = Assert.IsType<string[]>(actual);
            Assert.Equal(3, actualData.Length);
            Assert.Equal("first", actualData[0]);
            Assert.Equal("second", actualData[1]);
            Assert.Equal("third", actualData[2]);
        }

        [Fact]
        public void when_deserializing_directly_to_array_of_tuple()
        {
            // Given
            string source = "[{'Item1': 'first', 'Item2': 'second'}, {'Item1': 'third', 'Item2': 'fourth'}]";

            var context = new BindingContext
            {
                DestinationType = typeof(Tuple<string, string>[]),
                ValidModelBindingMembers = typeof(Tuple<string, string>).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => new BindingMemberInfo(p)),
            };

            // When
            object actual;
            using (var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            {
                IBodyDeserializer sut = new JsonNetBodyDeserializer();
                actual = sut.Deserialize("application/json", bodyStream, context);
            }

            // Then
            var actualData = Assert.IsType<Tuple<string, string>[]>(actual);
            Assert.Equal(2, actualData.Length);
            Assert.Equal("first", actualData[0].Item1);
            Assert.Equal("second", actualData[0].Item2);
            Assert.Equal("third", actualData[1].Item1);
            Assert.Equal("fourth", actualData[1].Item2);
        }

        [Fact]
        public void when_deserializing_directly_to_array_of_pod_type()
        {
            // Given
            string source = "[{'someString': 'first', 'someGuid': '<GUID>'}, {'someString': 'second', 'someGuid': '<GUID>'}]";

            source = System.Text.RegularExpressions.Regex.Replace(
                source,
                @"\<GUID\>",
                (match) => Guid.NewGuid().ToString());

            var context = new BindingContext
            {
                DestinationType = typeof(TestData[]),
                ValidModelBindingMembers = typeof(TestData).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => new BindingMemberInfo(p)),
            };

            // When
            object actual;
            using (var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            {
                IBodyDeserializer sut = new JsonNetBodyDeserializer();
                actual = sut.Deserialize("application/json", bodyStream, context);
            }

            // Then
            var actualData = Assert.IsType<TestData[]>(actual);
            Assert.Equal(2, actualData.Length);
            Assert.Equal("first", actualData[0].SomeString);
            Assert.NotEqual(Guid.Empty, actualData[0].SomeGuid);
            Assert.Equal("second", actualData[1].SomeString);
            Assert.NotEqual(Guid.Empty, actualData[1].SomeGuid);
        }
    }
}
