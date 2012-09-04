namespace Demo
{
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