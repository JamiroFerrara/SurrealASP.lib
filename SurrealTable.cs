using RestSharp;

public class SurrealTable
{
    public string? Id { get; set; }
}

public class SurrealS3
{
    public string key { get; set; }
    public string url { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Expiry { get; set; } = DateTime.Now;

    public static async Task<string> GetPresignedUrlAsync(string id)
    {
        if (id == "")
            return "No Id!";

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

public class SurrealTable<T1, T3>
{
    public string? Id { get; set; }
    public string? In { get; set; }
    public string? Out { get; set; }
    public DateTime Created { get; set; }
}

