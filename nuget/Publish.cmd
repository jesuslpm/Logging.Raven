%~dp0.\NuGet.exe Update -self
%~dp0.\NuGet.exe Push Logging.RavenDB.1.0.2.nupkg -source https://www.nuget.org
pause