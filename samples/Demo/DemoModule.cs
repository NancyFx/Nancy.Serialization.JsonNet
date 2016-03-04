namespace Demo
{
    using Nancy;
    using Nancy.ModelBinding;

    public class DemoModule : NancyModule
    {
        public DemoModule()
        {
            Get["/"] = _ =>
            {
                return View["index"];
            };

            Get["/json"] = _ =>
            {
                var model = new[]
                {
                    new DemoModel
                    {
                        Id = 1,
                        Name = "Fenella",
                        Age = 87,
                        Location = "Spout Hall"
                    },
                    new DemoModel
                    {
                        Id = 2,
                        Name = "Chorlton",
                        Age = 2,
                        Location = "Wheelie World"
                    },
                };

                return Response.AsJson(model);
            };

            Post["/json/{id}"] = x =>
            { 
                DemoModel model = this.Bind("Id");

                model.Id = x.id;

                return model.ToString();
            };
        }
    }
}