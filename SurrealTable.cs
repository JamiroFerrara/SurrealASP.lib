using System.Linq.Expressions;
using RestSharp;

public class Table
{
    public string? Id { get; set; }

    public static async Task<T[]?> Where<T>(Expression<Func<T, bool>> action) where T : new()
    {
        var expression = action.Simplify<T>().Body.ToString();
        expression = expression.Parse<T>();
        var sql = $"SELECT * FROM {typeof(T).Name} WHERE {expression}";
        //
        //FIX: The url must be fed in from appsettings.. otherwise it's not flexible
        //TODO: Replace url constructor with appsettings value.
        var res = await SurrealService.ExecQuery<T>(sql, "http://localhost:8000/sql");
        return res;
    }
}

public class S3Table
{
    public string id { get; set; }
    public string url { get; set; }
    public DateTime created { get; set; } = DateTime.Now;
    public DateTime expiry { get; set; }

    public static async Task<string> GetPresignedUrlAsync(string id)
    {
        if (id == "")
            return "No Id!";

        //FIX: The url must be fed in from appsettings.. otherwise it's not flexible
        var client = new RestClient("https://wasabi-uploader.fly.dev/presign");
        var request = new RestRequest();
        request.AddBody(new { key = id });
        var res = await client.PostAsync<S3Response>(request, CancellationToken.None);
        return res.url;
    }
}

public class S3Response
{
    public string url { get; set; }
}

public class Table<T1, T3>
{
    public string? Id { get; set; }
    public string? In { get; set; }
    public string? Out { get; set; }
    public DateTime Created { get; set; }
}

