namespace Demo
{
    using Nancy;
    using Nancy.Bootstrapper;
    using Nancy.Serialization.JsonNet;

    public class DemoBootstrapper : DefaultNancyBootstrapper
    {
        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                // Insert at position 0 so it takes precedence over the built in one.
                return NancyInternalConfiguration.WithOverrides(
                        c => c.Serializers.Insert(0, typeof(JsonNetJsonSerializer)));
            }
        }
    }
}