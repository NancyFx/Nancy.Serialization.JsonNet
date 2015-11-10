namespace Nancy.Serialization.JsonNet.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Nancy.Json;

    using Newtonsoft.Json;

    using Xunit;
    using Xunit.Extensions;

    public class JsonNetSerializerFixture : IDisposable
    {
        [Fact]
        public void Should_return_valid_extensions()
        {
            // Given
            var sut = new JsonNetSerializer();

            // When
            var result = sut.Extensions.ToArray();

            // Then
            Assert.Equal(1, result.Length);
            Assert.Equal("json", result[0]);
        }

        [Fact]
        public void CanSerialize_should_return_false_when_contenttype_is_null()
        {
            // Given
            var sut = new JsonNetSerializer();

            // When
            var result = sut.CanSerialize(null);

            // Then
            Assert.False(result);
        }

        [Fact]
        public void Should_use_default_global_settings_when_serializing()
        {
            // Given
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var guid = Guid.NewGuid();
            var data = new { SomeString = "some string value", SomeGuid = guid, NullValue = default(Uri) };
            var expected = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\"}}", guid);

            // When
            var result = SutFactory(data);

            // Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, "SomeValue")]
        [InlineData(false, "someValue")]
        public void Should_use_retaincasing_setting_when_serializing(bool retainCasing, string propertyName)
        {
            // Given
            JsonSettings.RetainCasing = retainCasing;
            var data = new { SomeValue = "NancyFX" };
            var expected = string.Format("{{\"{0}\":\"NancyFX\"}}", propertyName);

            // When
            var result = SutFactory(data);

            // Then
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, "2014-10-24T01:02:03.004Z")]
        [InlineData(false, "\\/Date(1414112523004)\\/")]
        public void Should_use_date_format_setting_when_serializing(bool useISO8601DateFormat, string date)
        {
            // Given
            JsonSettings.ISO8601DateFormat = useISO8601DateFormat;
            var data = new { SomeDate = new DateTime(2014, 10, 24, 1, 2, 3, 4, DateTimeKind.Utc) };
            var expected = string.Format("{{\"someDate\":\"{0}\"}}", date);

            // When
            var result = SutFactory(data);

            // Then
            Assert.Equal(expected, result);
        }

        [Fact]
        public void When_serializing()
        {
            // Given
            var guid = Guid.NewGuid();
            var data = new { SomeString = "some string value", SomeGuid = guid, NullValue = default(Uri) };
            var expected = string.Format("{{\"someString\":\"some string value\",\"someGuid\":\"{0}\",\"nullValue\":null}}", guid);

            // When
            var result = SutFactory(data);

            // Then
            Assert.Equal(expected, result);
        }

        public void Dispose()
        {
            JsonConvert.DefaultSettings = null;

            JsonSettings.ISO8601DateFormat = true;
            JsonSettings.RetainCasing = false;
        }

        private string SutFactory(object data)
        {
            string result;

            using (var stream = new MemoryStream())
            {
                ISerializer sut = new JsonNetSerializer();
                sut.Serialize("application/json", data, stream);
                result = Encoding.UTF8.GetString(stream.ToArray());
            }

            return result;
        }
    }
}