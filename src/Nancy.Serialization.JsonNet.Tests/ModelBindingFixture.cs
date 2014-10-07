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

        [Fact]
        public void Should_BindTo_Existing_Instance_Using_Body_Serializer()
        {
            //Given
            var module = new ConfigurableNancyModule(c => c.Post("/instance", (_, m) =>
            {
                var model = new Stuff() { Id = 1 };
                m.BindTo(model);
                return model;
            }));

            var bootstrapper = new TestBootstrapper(config => config.Module(module));

            var postmodel = new Stuff { Name = "Marsellus Wallace" };

            var browser = new Browser(bootstrapper);
            
            //When
            var result = browser.Post("/instance", with =>
            {
                with.JsonBody(postmodel, new JsonNetSerializer());
                with.Accept("application/json");
            });

            var resultModel = result.Body.DeserializeJson<Stuff>();

            //Then
            Assert.Equal("Marsellus Wallace", resultModel.Name);
            Assert.Equal(1, resultModel.Id);
        }

        [Fact]
        public void Should_BindTo_Existing_Instance_Using_Body_Serializer_And_BlackList()
        {
            //Given
            var module = new ConfigurableNancyModule(c => c.Post("/instance", (_, m) =>
            {
                var model = new Stuff() { Id = 1 };
                m.BindTo(model, new[]{"LastName"});
                return model;
            }));

            var bootstrapper = new TestBootstrapper(config => config.Module(module));

            var postmodel = new Stuff { Name = "Marsellus Wallace", LastName = "Smith"};

            var browser = new Browser(bootstrapper);

            //When
            var result = browser.Post("/instance", with =>
            {
                with.JsonBody(postmodel, new JsonNetSerializer());
                with.Accept("application/json");
            });

            var resultModel = result.Body.DeserializeJson<Stuff>();

            //Then
            Assert.Null(resultModel.LastName);
        }
    }
    public class TestBootstrapper : ConfigurableBootstrapper
    {
        public TestBootstrapper(Action<ConfigurableBootstrapperConfigurator> configuration)
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

        public string Name { get; set; }

        public string LastName { get; set; }

        public Stuff(int id)
        {
            Id = id;
        }
    }
}