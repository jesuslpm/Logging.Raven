dotnet pack -c Release -p:PackageVersion=1.0.3 -p:ContinuousIntegrationBuild=true --output nupkgs
..\nuget\NuGet.exe Push nupkgs\Logging.RavenDB.1.0.3.nupkg -source https://www.nuget.org