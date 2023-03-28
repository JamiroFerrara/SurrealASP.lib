# SurrealAPI.lib

A SurrealDB + ASP.net wrapper to make building persistent REST api's a breeze! ☼

## Quickstart
Create a folder with you API project and initialize an empty ASP web api using:
**dotnet new web** 

Install the Nuget package using: 
**dotnet nuget install**

Once the package is installed you are ready to connect to a SurrealDb instance.
I personally recommend using a docker-compose file with the necessary image.

```docker-compose
version: "3"
services:
  # myapp:
  #   build: .
  #   ports:
  #     - "5000:80"
  #   environment:
  #     - ConnectionStrings:SurrealDB=Server=surrealdb:27017
  #     - ASPNETCORE_ENVIRONMENT=Production
  #   volumes:
  #     - myapp_data:/app/data/
  #   networks:
  #     - common-net
  #   links:
  #     - surrealdb
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

Once done you have to create a connection string in the appsettings.json file to
tell the library where our database living: 

```json
  "ConnectionStrings": {
    "SurrealDb": "http://localhost:8000/sql"
  }
```

Of course you can replace the localhost:8000 with whatever your connection
string is. 

Now add this line of code in Program.cs

```csharp	
app.MapSurreal();
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
