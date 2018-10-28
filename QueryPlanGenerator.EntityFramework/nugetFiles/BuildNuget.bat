set nugetpath="..\..\..\..\packages\NuGet.CommandLine.4.7.1\tools\nuget.exe"

set path=%~dp0..\

del /q /f *.nupkg 

%nugetpath% pack QueryPlanGenerator.EntityFramework.nuspec 

@pause