# SurrealAPI.lib 🔮

A SurrealDB + ASP.net wrapper to make building persistent REST api's a breeze!
https://surrealdb.com/docs

## Quickstart
Create a folder with you API project and initialize an empty ASP web api using:
**dotnet new web** 

Install the Nuget package using: 
**dotnet nuget install**

Once the package is installed you are ready to connect to a SurrealDb instance.
I personally recommend using docker with the necessary image.

```docker-compose
docker run --rm -p 8000:8000 surrealdb/surrealdb:latest start
```

or

```docker-compose
version: "3"
services:
  surrealdb:
    image: surrealdb/surrealdb:latest
    volumes:
      - db_data:/data/db
    ports:
      - "8000:8000"
    command: start --user root --pass root file://database.db
    networks:
      - common-net
volumes:
  db_data:
  myapp_data:

networks:
  common-net: {}
```

Of course you can replace the localhost:8000 with whatever your connection
string is. 

Now add this line of code in Program.cs

```csharp	
app.MapSurreal("http://localhost:8000/sql");
```

Once done you are all set to start building entities.

**All you need to do** is create a class that extends the **SurrealTable**
class like so:

```csharp	
public class User : SurrealTable
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

And just like that you have created a persistent CRUD api!
You now have access to the following endpoints: 

POST -> http://localhost:8000/User/Create
POST -> http://localhost:8000/User/Update
GET ->  http://localhost:8000/User/GetAll
GET ->  http://localhost:8000/User/DeleteAll
GET ->  http://localhost:8000/User/Get/{id}
GET ->  http://localhost:8000/User/Delete/{id}

You can even define relations in this manner:
We generate another table: 

```csharp	
public class ShopItem : SurrealTable
{
    public string Name { get; set; }
    public int Price { get; set; }
}
```

And then define a intermediate table with extra information about the relationship:

```csharp	
public class WantsToBuy : SurrealTable<User, ShopItem>
{
    public int Amount { get; set; }
}
```

POST -> http://localhost:8000/{item1Id}/WantsToBuy/{item2Id}
POST -> http://localhost:8000/User/WantsToBuy/Update/{id}
GET ->  http://localhost:8000/User/WantsToBuy/GetAll
GET ->  http://localhost:8000/User/WantsToBuy/DeleteAll
GET ->  http://localhost:8000/User/WantsToBuy/Get/{id}
GET ->  http://localhost:8000/User/WantsToBuy/Delete/{id}

👌 Happy coding!