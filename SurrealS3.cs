using RestSharp;

public class S3
{
    public string key { get; set; }
    public string url { get; set; }

    public static async Task<string> GetPresignedUrlAsync(string id)
    {
        if (id == "")
            return "No Id!";

        var client = new RestClient("https://wasabi-uploader.fly.dev");
        var request = new RestRequest();
        request.AddBody(new { key = id });
        return (await client.PostAsync<string>(request, CancellationToken.None))!;
    }

}
