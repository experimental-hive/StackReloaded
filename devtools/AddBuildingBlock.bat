@ECHO OFF
cd..
dotnet.exe run --project ./eng/TemplateEngine/TemplateEngine.Cli/TemplateEngine.Cli.csproj --configuration Debug --verbosity minimal
pause
