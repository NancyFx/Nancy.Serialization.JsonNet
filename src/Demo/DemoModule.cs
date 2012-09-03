using Nancy;
using Nancy.ModelBinding;

namespace Demo
{
    public class DemoModule : NancyModule
    {
        public DemoModule()
        {
            Get["/"] = _ => View["index"];

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

    public class DemoModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Location { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}, Age: {2}, Location: {3}", this.Id, this.Name, this.Age, this.Location);
        }
    }
}