namespace Nancy.Serialization.JsonNet.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Nancy.Json;
    using Nancy.ModelBinding;

    using Xunit;
    using Xunit.Extensions;

    public class JsonNetBodyDeserializerFixture : IDisposable
    {
        [Fact]
        public void CanDeserialize_should_return_false_when_contenttype_is_null()
        {
            // Given
            var deserializer = new JsonNetBodyDeserializer();

            // When
            var result = deserializer.CanDeserialize(null, new BindingContext());

            // Then
            Assert.False(result);
        }

        [Fact]
        public void Should_not_deserialize_excluded_properties()
        {
            // Given
            var guid = Guid.NewGuid();
            string source = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\",\"nullValue\":null}}", guid);

            // When
            object actual = SutFilteredPropertyFactory(source);

            // Then
            var actualData = Assert.IsType<TestData>(actual);
            Assert.Null(actualData.SomeString);
            Assert.Equal(guid, actualData.SomeGuid);
            Assert.Null(actualData.NullValue);
            Assert.Equal(DateTime.MinValue, actualData.SomeDate);
        }

        [Theory]
        [InlineData(true, "2014-10-24T01:02:03.004Z")]
        [InlineData(false, "\\/Date(1414112523004)\\/")]
        public void When_deserializing(bool useISO8601DateFormat, string date)
        {
            // Given
            var guid = Guid.NewGuid();
            string source = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\",\"nullValue\":null,\"someDate\":\"{1}\"}}", guid, date);

            // When
            object actual = SutFactory(source);

            // Then
            var actualData = Assert.IsType<TestData>(actual);
            Assert.Equal("some string value", actualData.SomeString);
            Assert.Equal(guid, actualData.SomeGuid);
            Assert.Null(actualData.NullValue);
            Assert.Equal(new DateTime(2014, 10, 24, 1, 2, 3, 4, DateTimeKind.Utc), actualData.SomeDate);
        }

        public void Dispose()
        {
            JsonSettings.ISO8601DateFormat = true;
        }

        private object SutFilteredPropertyFactory(string json)
        {
            return SutFactory(json, p => p.PropertyType != typeof (string));
        }

        private object SutFactory(string json, Func<PropertyInfo, bool> propertyFilter = null)
        {
            var validPropperties = typeof(TestData).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (propertyFilter != null)
            {
                validPropperties = validPropperties.Where(propertyFilter).ToArray();
            }

            var context = new BindingContext
            {
                DestinationType = typeof (TestData),
                ValidModelBindingMembers = validPropperties.Select(p => new BindingMemberInfo(p)),
            };

            object actual;
            using (var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                IBodyDeserializer sut = new JsonNetBodyDeserializer();
                actual = sut.Deserialize("application/json", bodyStream, context);
            }

            return actual;
        }

        private class TestData
        {
            public TestData()
            {
            }

            public TestData(string randomStuff)
            {
                // Should never get here as it should use the DefaultConstructor first.
                if (randomStuff == null)
                {
                    throw new ArgumentNullException("randomStuff");
                }
            }

            public string SomeString { get; set; }

            public Guid SomeGuid { get; set; }

            public Uri NullValue { get; set; }

            public DateTime SomeDate { get; set; }
        }
    }
}
