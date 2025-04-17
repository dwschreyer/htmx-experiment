namespace HtmxWebApplication.Data;

public static class DataServices
{
    public static People GetPeople()
    {
        return new People();
    }
}


public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class People
{
    public List<Person> Items { get; set; } =
        [
            new() { Id = 1, Name = "dwschreyer"},
            new() { Id = 2, Name = "aaschreyer"},
            new() { Id = 3, Name = "cwschreyer"}
        ];
}
