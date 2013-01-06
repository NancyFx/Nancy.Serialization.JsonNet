namespace Nancy.Serialization.JsonNet.Tests
{
    using System;
    using System.Collections.Generic;
    using Nancy.ModelBinding;
    using Nancy.Testing;
    using Xunit;

    public class ModelBindingFixture
    {
        [Fact]
        public void when_binding_to_a_class()
        {
            // Given
            var module = new ConfigurableNancyModule(c => c.Post("/stuff", (_, m) =>
            {
                var stuff = m.Bind<Stuff>();
                return stuff.Id.ToString();
            }));
            var bootstrapper = new TestBootstrapper(config => config.Module(module));
            
            // When
            var browser = new Browser(bootstrapper);
            var result = browser.Post("/stuff", with =>
            {
                with.HttpRequest();
                with.JsonBody(new Stuff(1), new JsonNetSerializer());
            });

            // Then
            Assert.Equal(1, int.Parse(result.Body.AsString()));
        }
        
        [Fact]
        public void when_binding_to_a_collection()
        {
            // Given
            var module = new ConfigurableNancyModule(c => c.Post("/stuff", (_, m) =>
            {
                var stuff = m.Bind<List<Stuff>>();
                return stuff.Count.ToString();
            }));
            var bootstrapper = new TestBootstrapper(config => config.Module(module));

            // When
            var browser = new Browser(bootstrapper);
            var result = browser.Post("/stuff", with =>
            {
                with.HttpRequest();
                with.JsonBody(new List<Stuff> {new Stuff(1), new Stuff(2)}, new JsonNetSerializer());
            });

            // Then
            Assert.Equal(2, int.Parse(result.Body.AsString()));
        }
    }
    public class TestBootstrapper : ConfigurableBootstrapper
    {
        public TestBootstrapper(Action<ConfigurableBoostrapperConfigurator> configuration)
            : base(configuration)
        {
        }

        public TestBootstrapper()
        {
        }

        protected override IEnumerable<Type> BodyDeserializers
        {
            get
            {
                yield return typeof(JsonNetBodyDeserializer);
            }
        }
    }

    public class Stuff
    {
        public Stuff()
        {
        }

        public int Id { get; set; }

        public Stuff(int id)
        {
            Id = id;
        }
    }
}