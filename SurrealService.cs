using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using Microsoft.Extensions.Configuration;

public static class SurrealService
{
    public static string? Url { get; set; } = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false).Build()
        .GetConnectionString("SurrealDb");

    public static string Database { get; set; } = "main";
    public static string Namespace { get; set; } = "main";

    public static async Task<T> Create<T>(T item)
    {
        var type = item?.GetType();
        var props = type?.GetProperties();

        var sql = $"CREATE {type?.Name} SET ";
        foreach (var prop in props!)
            sql += $"{prop.Name} = '{prop.GetValue(item)}', ";
        sql = sql.Remove(sql.Length - 2);
        sql += ";";

        var res = await SurrealService.ExecQuery<T>(sql);
        return res![0];
    }

    public static async Task<T?> Update<T>(Dictionary<string, string> props, string id)
    {
        var sql = $"UPDATE {typeof(T).Name} SET ";
        foreach (var prop in props)
            sql += $"{prop.Key} = '{prop.Value}', ";

        sql = sql.Remove(sql.Length - 2);
        sql += $" WHERE id = '{id}' ";
        sql = sql.Remove(sql.Length - 1);
        sql += ";";

        var res = await ExecQuery<T>(sql);
        return res![0];
    }

    public static async Task<T> Select<T>(string id)
    {
        var sql = $"SELECT * FROM {typeof(T).Name} WHERE id = '{id}'";
        var res = await ExecQuery<T>(sql);
        return res![0];
    }

    public static async Task<string?> SelectRelation(string relation, string id)
    {
        var sql = $"SELECT * FROM {relation} WHERE id = '{id}'";
        var res = await ExecQuery(sql);
        return res?.Content;
    }

    public static async Task<T[]?> SelectAll<T>()
    {
        var sql = $"SELECT * FROM {typeof(T).Name}";
        var res = await ExecQuery<T>(sql);
        return res;
    }

    public static async Task<string?> SelectAll(string name)
    {
        var sql = $"SELECT * FROM {name}";
        var res = await ExecQuery(sql);
        return res?.Content;
    }

    public static async Task<T> Delete<T>(string id)
    {
        var sql = $"REMOVE FROM {typeof(T).Name}:{id}";
        var res = await ExecQuery<T>(sql);
        return res![0];
    }

    public static async Task<string?> DeleteRelation<T>(string id, string relation)
    {
        var sql = $"DELETE FROM {relation} WHERE id = '{id}'";
        var res = await ExecQuery(sql);
        return res?.Content;
    }

    public static async Task<bool> DeleteAll<T>()
    {
        var sql = $"DELETE FROM {typeof(T).Name}";
        var res = await ExecQuery<T>(sql);
        return true;
    }

    public static async Task<string?> DeleteAll(string name)
    {
        var sql = $"DELETE FROM {name}";
        var res = await ExecQuery(sql);
        return res?.Content;
    }

    public static async Task<string?> Relate<T1, T2>(string item1Id, string item2Id, string action)
    {
        var sql = $"RELATE {item1Id}->{action}->{item2Id} ";
        sql += $"SET created = time::now()";
        var res = await ExecQuery(sql);
        return res?.Content;
    }

    public static async Task<T1?> Relate<T1>(string item1Id, string item2Id, string action, Dictionary<string, string> props)
    {
        var sql = $"RELATE {item1Id}->{action}->{item2Id} ";
        sql += $"SET created = time::now(), ";
        foreach (var prop in props)
            sql += $"{prop.Key} = {prop.Value}, ";

        sql = sql.Remove(sql.Length - 2);
        sql += ";";

        var res = await ExecQuery<T1>(sql);
        return res![0];
    }

    public static async Task<T[]?> ExecQuery<T>(string query)
    {
        try
        {
            var res = await ExecQuery(query);
            var jsonArray = JArray.Parse(res?.Content!);
            var parsedObj = (JObject)jsonArray[0];
            var parsedRes = (JArray)parsedObj["result"]!;

            return JsonConvert.DeserializeObject<T[]>(parsedRes.ToString());
        }
        catch
        {
            return default(T[]);
        }
    }

    public static async Task<RestResponse?> ExecQuery(string query)
    {
        var req = new RestRequest(Url)
            .AddHeader("Accept", "application/json")
            .AddHeader("DB", Database)
            .AddHeader("NS", Namespace)
            .AddParameter("text/plain", query, ParameterType.RequestBody);

        var options = new RestClientOptions();
        options.Authenticator = new HttpBasicAuthenticator("root", "root");

        return await new RestClient(options).PostAsync(req, CancellationToken.None);
    }
}
