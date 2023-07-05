public class SurrealTable
{
    public string? Id { get; set; }
}

public class SurrealS3
{
    public string? Id { get; set; }
}

public class SurrealTable<T1, T3>
{
    public string? Id { get; set; }
    public string? In { get; set; }
    public string? Out { get; set; }
    public DateTime Created { get; set; }
}

