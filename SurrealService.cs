using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Microsoft.Extensions.Configuration;

public static class SurrealService
{
    public static string Database { get; set; } = "main";
    public static string Namespace { get; set; } = "main";

    public static async Task<T> Create<T>(T item, string url)
    {
        var type = item?.GetType();
        var props = type?.GetProperties();

        var sql = $"CREATE {type?.Name} CONTENT ";
        var json = JsonConvert.SerializeObject(item);
        sql = sql + json;
        sql += ";";

        var res = await SurrealService.ExecQuery<T>(sql, url);
        return res![0];
    }

    public static async Task<T[]?> Update<T>(T item, string id, string url)
    {
        var sql = $"UPDATE {typeof(T).Name} CONTENT ";
        var json = JsonConvert.SerializeObject(item);
        sql = sql + json;
        sql += $" WHERE id = '{id}' ";
        sql = sql.Remove(sql.Length - 1);
        sql += ";";
        Console.WriteLine(sql);

        var res = await ExecQuery<T>(sql, url);
        return res;
    }

    public static async Task<T[]> Select<T>(string id, string url)
    {
        var sql = $"SELECT * FROM {typeof(T).Name} WHERE id = '{id}'";
        var res = await ExecQuery<T>(sql, url);
        return res;
    }

    public static async Task<T> SelectS3<T>(string id, string url) where T : SurrealS3
    {
        var sql = $"SELECT * FROM {typeof(T).Name} WHERE id = '{id}'";
        var results = await ExecQuery<T>(sql, url);
        foreach (var res in results)
        {
            if (res.Expiry > DateTime.Now)
            {
                var presignUrl = await SurrealS3.GetPresignedUrlAsync(res.id.Split('(')[0].Split('\'')[0]);
                res.Expiry = DateTime.Now.AddDays(7);
                res.url = presignUrl;

                await Update<T>(res, id, url);
            }
        }

        return results![0];
    }

    public static async Task<string?> SelectRelation(string relation, string id, string url)
    {
        var sql = $"SELECT * FROM {relation} WHERE id = '{id}'";
        var res = await ExecQuery(sql, url);
        return res?.Content;
    }

    public static async Task<T[]?> SelectAll<T>(string url)
    {
        Console.WriteLine($"Url: {url}");
        Console.WriteLine($"Url: {typeof(T)}");
        var sql = $"SELECT * FROM {typeof(T).Name}";
        Console.WriteLine($"{sql}");
        var res = await ExecQuery<T>(sql, url);
        return res;
    }

    public static async Task<T[]?> SelectAllS3<T>(string url) where T : SurrealS3
    {
        Console.WriteLine($"Url: {url}");
        Console.WriteLine($"Url: {typeof(T)}");
        var sql = $"SELECT * FROM {typeof(T).Name}";
        Console.WriteLine($"{sql}");
        var res = await ExecQuery<T>(sql, url);
        foreach (var r in res)
        {
            Console.WriteLine($"Key to presign: {r.id}");
            if (r.Expiry < DateTime.Now)
            {
                Console.WriteLine("Expiry..");
                //FIX: This is hardcoded BAD
                var presignUrl = await SurrealS3.GetPresignedUrlAsync(r.id.Split('(')[0].Split('\'')[0]);
                r.Expiry = DateTime.Now.AddDays(7);
                r.url = presignUrl;

                await Update<T>(r, r.id, url);
            }
        };
        return res;
    }

    public static async Task<string?> SelectAll(string name, string url)
    {
        var sql = $"SELECT * FROM {name}";
        var res = await ExecQuery(sql, url);
        return res?.Content;
    }

    public static async Task<T> Delete<T>(string id, string url)
    {
        var sql = $"REMOVE FROM {typeof(T).Name}:{id}";
        var res = await ExecQuery<T>(sql, url);
        return res![0];
    }

    public static async Task<string?> DeleteRelation<T>(string id, string relation, string url)
    {
        var sql = $"DELETE FROM {relation} WHERE id = '{id}'";
        var res = await ExecQuery(sql, url);
        return res?.Content;
    }

    public static async Task<bool> DeleteAll<T>(string url)
    {
        var sql = $"DELETE FROM {typeof(T).Name}";
        var res = await ExecQuery<T>(sql, url);
        return true;
    }

    public static async Task<string?> DeleteAll(string name, string url)
    {
        var sql = $"DELETE FROM {name}";
        var res = await ExecQuery(sql, url);
        return res?.Content;
    }

    public static async Task<string?> Relate<T1, T2>(string item1Id, string item2Id, string action, string url)
    {
        var sql = $"RELATE {item1Id}->{action}->{item2Id} ";
        sql += $"SET created = time::now()";
        var res = await ExecQuery(sql, url);
        return res?.Content;
    }

    public static async Task<T1?> Relate<T1>(string item1Id, string item2Id, string action, Dictionary<string, string> props, string url)
    {
        var sql = $"RELATE {item1Id}->{action}->{item2Id} ";
        sql += $"SET created = time::now(), ";
        foreach (var prop in props)
            sql += $"{prop.Key} = {prop.Value}, ";

        sql = sql.Remove(sql.Length - 2);
        sql += ";";

        var res = await ExecQuery<T1>(sql, url);
        return res![0];
    }

    public static async Task<T[]?> ExecQuery<T>(string query, string url)
    {
        var res = await ExecQuery(query, url);
        var jsonArray = JArray.Parse(res?.Content!);
        var parsedObj = (JObject)jsonArray[0];
        var parsedRes = (JArray)parsedObj["result"]!;

        return JsonConvert.DeserializeObject<T[]>(parsedRes.ToString());
    }

    public static async Task<RestResponse?> ExecQuery(string query, string url)
    {
        var req = new RestRequest(url)
            .AddHeader("Accept", "application/json")
            .AddHeader("DB", Database)
            .AddHeader("NS", Namespace)
            .AddParameter("text/plain", query, ParameterType.RequestBody);

        var options = new RestClientOptions();
        options.Authenticator = new HttpBasicAuthenticator("root", "root");

        return await new RestClient(options).PostAsync(req, CancellationToken.None);
    }
}
