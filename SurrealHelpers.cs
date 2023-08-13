using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;

public static class SurrealHelpers
{
    public static bool ValidateSchema<T>(Dictionary<string, string> item)
    {
        foreach (var prop in item)
        {
            bool exists = false;
            foreach (var propType in GetPublicProperties<T>())
                if (prop.Key == propType.Name)
                    exists = true;

            if (!exists)
                return false;
        }

        return true;
    }

    public static PropertyInfo[] GetPublicProperties<T1>()
    {
        Type derivedType = typeof(T1);
        PropertyInfo[] properties = derivedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Where(p => p.DeclaringType == derivedType).ToArray();
    }

    public static void MapRoutes<T>(WebApplication app, string url) where T : new()
    {
        app.MapPost($"{typeof(T)}/Create", ([FromBody] T item) => SurrealService.Create<T>(item, url)).WithName($"Create{typeof(T)}");
        app.MapPost($"{typeof(T)}/Update/" + "{id}", ([FromBody] T item, string id) => SurrealService.Update<T>(item, id, url)).WithName($"Update{typeof(T)}");
        app.MapPost($"{typeof(T)}/DeleteAll", () => SurrealService.DeleteAll<T>(url)).WithName($"DeleteAll{typeof(T)}");
        app.MapPost($"{typeof(T)}/Delete" + "{id}", (string id) => SurrealService.Delete<T>(id, url)).WithName($"Delete{typeof(T)}");
        app.MapGet($"{typeof(T)}/GetAll", () => SurrealService.SelectAll<T>(url)).WithName($"GetAll{typeof(T)}");
        app.MapGet($"{typeof(T)}/Get" + "{id}", (string id) => SurrealService.Select<T>(id, url)).WithName($"Get{typeof(T)}");

        Console.WriteLine($"{typeof(T)}/Create");
        Console.WriteLine($"{typeof(T)}/Update/" + "{id}");
        Console.WriteLine($"{typeof(T)}/GetAll");
        Console.WriteLine($"{typeof(T)}/Get" + "{id}");
        Console.WriteLine($"{typeof(T)}/DeleteAll");
        Console.WriteLine($"{typeof(T)}/Delete" + "{id}");

        //NOTE: Map additional routes
        Type type = typeof(T); // Replace MyClass with the actual type you want to inspect
        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        foreach (MethodInfo method in methods)
        {
            if (method.IsSpecialName) continue;
            if (method.Name == "GetType") continue;
            if (method.Name == "ToString") continue;
            if (method.Name == "Equals") continue;
            if (method.Name == "GetHashCode") continue;

            //BUG: This works but isn't picked up by swagger..
            var instance = Activator.CreateInstance(typeof(T));
            var requestDelegate = RequestDelegateFactory.Create(method, context => new T()).RequestDelegate;
            app.MapPost($"/{typeof(T).Name}/{method.Name}", requestDelegate)
                .WithName($"{method.Name}");

            Console.WriteLine(($"{typeof(T)}/{method.Name}"));
        }
    }

    public static void MapS3Routes<T>(WebApplication app, string url) where T : SurrealS3
    {
        app.MapPost($"{typeof(T)}/Create", ([FromBody] T item) => SurrealService.Create<T>(item, url)).WithName($"Create{typeof(T)}");
        app.MapPost($"{typeof(T)}/Update/" + "{id}", ([FromBody] T item, string id) => SurrealService.Update<T>(item, id, url)).WithName($"Update{typeof(T)}");
        app.MapGet($"{typeof(T)}/GetAll", () => SurrealService.SelectAllS3<T>(url)).WithName($"GetAll{typeof(T)}");
        app.MapGet($"{typeof(T)}/Get/" + "{id}", (string id) => SurrealService.SelectS3<T>(id, url)).WithName($"Get{typeof(T)}");
        app.MapGet($"{typeof(T)}/DeleteAll", () => SurrealService.DeleteAll<T>(url)).WithName($"DeleteAll{typeof(T)}");
        app.MapGet($"{typeof(T)}/Delete" + "{id}", (string id) => SurrealService.Delete<T>(id, url)).WithName($"Delete{typeof(T)}");

        Console.WriteLine($"{typeof(T)}/Create");
        Console.WriteLine($"{typeof(T)}/Update/" + "{id}");
        Console.WriteLine($"{typeof(T)}/GetAll");
        Console.WriteLine($"{typeof(T)}/Get" + "{id}");
        Console.WriteLine($"{typeof(T)}/DeleteAll");
        Console.WriteLine($"{typeof(T)}/Delete" + "{id}");
    }

    public static void MapRelations<T1, T2, T3>(WebApplication app, string url)
    {
        app.MapPost("{item1Id}" + $"/{typeof(T2)}/" + "{item2Id}", async (string item1Id, string item2Id, string relation, [FromBody] Dictionary<string, string> items) =>
        {
            if (!SurrealHelpers.ValidateSchema<T2>(items) || typeof(T2).Name != relation)
                return default(T2);

            return await SurrealService.Relate<T2>(item1Id, item2Id, relation, items, url);
        }).WithName($"{typeof(T1)}{typeof(T2)}{typeof(T3)}");
        app.MapPost($"{typeof(T1)}/{typeof(T2)}/Update/" + "{id}", ([FromBody] T2 item, string id) => SurrealService.Update<T2>(item, id, url)).WithName($"Update{typeof(T1)}{typeof(T2)}{typeof(T3)}");
        app.MapGet($"{typeof(T1)}/{typeof(T2)}/GetAll", () => SurrealService.SelectAll<T2>(url)).WithName($"GetAll{typeof(T1)}{typeof(T2)}{typeof(T3)}");
        app.MapGet($"{typeof(T1)}/{typeof(T2)}/Get/" + "{id}", (string id) => SurrealService.Select<T2>(id, url)).WithName($"Get{typeof(T1)}{typeof(T2)}{typeof(T3)}");
        app.MapGet($"{typeof(T1)}/{typeof(T2)}/DeleteAll", () => SurrealService.DeleteAll<T2>(url)).WithName($"DeleteAll{typeof(T1)}{typeof(T2)}{typeof(T3)}");
        app.MapGet($"{typeof(T1)}/{typeof(T2)}/Delete" + "{id}", (string id) => SurrealService.Delete<T2>(id, url)).WithName($"Delete{typeof(T1)}{typeof(T2)}{typeof(T3)}");

        Console.WriteLine($"{typeof(T1)}" + "{item1Id}" + $"/{typeof(T2)}/" + "{item2Id}");
        Console.WriteLine($"{typeof(T1)}/{typeof(T2)}/Update/" + "{id}");
        Console.WriteLine($"{typeof(T1)}/{typeof(T2)}/GetAll");
        Console.WriteLine($"{typeof(T1)}/{typeof(T2)}/Get/" + "{id}");
        Console.WriteLine($"{typeof(T1)}/{typeof(T2)}/DeleteAll");
        Console.WriteLine($"{typeof(T1)}/{typeof(T2)}/Delete" + "{id}");
    }

    public static void MapSurreal(this WebApplication app, string url)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Model API:");
        Console.ForegroundColor = ConsoleColor.Gray;
        var c = app.MapSurrealCRUD(url);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Count: {c}");
        Console.WriteLine($"");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Relations API:");
        Console.ForegroundColor = ConsoleColor.Gray;
        var c2 = app.MapSurrealCRUDRelations(url);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Count: {c2}");
        Console.WriteLine($"");

        Console.WriteLine($"S3 API:");
        Console.ForegroundColor = ConsoleColor.Gray;
        var c3 = app.MapSurrealS3CRUD(url);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Count: {c2}");
        Console.WriteLine($"");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Total: {c + c2 + c3}");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    public static int MapSurrealCRUD(this WebApplication app, string url)
    {
        var count = 0;
        var baseType = typeof(SurrealTable);
        var namespaceTypes = Assembly.GetEntryAssembly().GetTypes().Where(t => baseType.IsAssignableFrom(t));
        foreach (var type in namespaceTypes)
        {
            if (type != typeof(SurrealTable))
            {
                //NOTE: All types that inherit from SurrealTable
                var mapRoutesMethod = typeof(SurrealHelpers).GetMethod(nameof(MapRoutes), BindingFlags.Public | BindingFlags.Static);
                var genericMapRoutesMethod = mapRoutesMethod?.MakeGenericMethod(type);
                genericMapRoutesMethod?.Invoke(null, new object[] { app, url });
                count += 6;
            }
        }
        return count;
    }

    public static int MapSurrealS3CRUD(this WebApplication app, string url)
    {
        var count = 0;
        var baseType = typeof(SurrealS3);
        var namespaceTypes = Assembly.GetEntryAssembly().GetTypes().Where(t => baseType.IsAssignableFrom(t));
        foreach (var type in namespaceTypes)
        {
            if (type != typeof(SurrealS3))
            {
                var mapRoutesMethod = typeof(SurrealHelpers).GetMethod(nameof(MapS3Routes), BindingFlags.Public | BindingFlags.Static);
                var genericMapRoutesMethod = mapRoutesMethod?.MakeGenericMethod(type);
                genericMapRoutesMethod?.Invoke(null, new object[] { app, url });
                count += 6;
            }
        }
        return count;
    }

    public static int MapSurrealCRUDRelations(this WebApplication app, string url)
    {
        var count = 0;
        var namespaceTypes = Assembly.GetEntryAssembly().GetTypes().Where(t => typeof(SurrealTable).IsAssignableFrom(t));
        foreach (var type in namespaceTypes)
        {
            foreach (var type2 in namespaceTypes)
            {
                var closedType = typeof(SurrealTable<,>).MakeGenericType(type, type2);
                var relationTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => closedType.IsAssignableFrom(t));
                foreach (var t in relationTypes)
                {
                    var mapRoutesMethod = typeof(SurrealHelpers).GetMethod(nameof(MapRelations), BindingFlags.Public | BindingFlags.Static);
                    var genericMapRoutesMethod = mapRoutesMethod?.MakeGenericMethod(type, t, type2);
                    genericMapRoutesMethod?.Invoke(null, new object[] { app, url });
                    count += 6;
                }
            }
        }
        return count;
    }
}
