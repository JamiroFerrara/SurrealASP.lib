#!/bin/sh

dotnet build --configuration Release
mkdir nuget/output
rsync -a ../bin/Release/net7.0/ ./nuget/output

alias nuget="mono /usr/local/bin/nuget.exe"
nuget pack nuget/SurrealASP.nuspec
