@ECHO OFF
cd ..

echo Nieuwe bestanden zullen verwijderd worden, gewijzigde bestanden zullen ongedaan worden gemaakt!
pause

echo Te verwijderen bestanden bepalen...
"C:\Program Files\Git\cmd\git.exe" clean -d -x -f

echo Build...
eng\nuget.exe restore src\All\StackReloaded.BuildingBlocks.sln
dotnet.exe build src\All\StackReloaded.BuildingBlocks.sln --configuration Debug

echo --------------------------------------------------------------------------------
echo Druk op een toets om de testen te starten of sluit het venster om ze te skippen.
pause

echo Run Tests...
dotnet.exe test src\All\StackReloaded.BuildingBlocks.sln --no-build --no-restore --verbosity quiet --filter FullyQualifiedName!~ContractTests --logger trx --results-directory testresults /p:CollectCoverage=true "--collect:Code Coverage"
pause