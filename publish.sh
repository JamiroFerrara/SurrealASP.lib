#!/bin/sh

dotnet build --configuration Release
rsync -a bin/Release/net7.0/ ./bin/Nuget/

alias nuget="mono /usr/local/bin/nuget.exe"
nuget pack SurrealASP.nuspec -OutputDirectory ./bin/Nuget
dotnet nuget push ./bin/Nuget/SurrealASP.lib.1.0.0.nupkg --api-key oy2cry7gqsqbffqh6k4qlhkuf3mk2uzergd5wpt2ukmrqm --source https://api.nuget.org/v3/index.json
