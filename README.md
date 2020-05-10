# StackReloaded
De repo bevat verschillende oplossingen voor bepaalde topics.
De verschillende oplossingen vormen samen het StackReloaded Platform.

# Minimum Software Requirements
- [Visual Studio 16.5.4](https://visualstudio.microsoft.com/vs/)
- [.NET Core SDK 3.1.2](https://dotnet.microsoft.com/download/)

# Getting Started
1. Kloon deze repo in `C:\Git\StackReloaded`
2. Voer `devtools\ReconcileAndBuild.bat` uit om de volledige repo te builden en eventueel om alle testen uit te voeren

# Solutions
- Docs.sln (in docs)
  - Via dit solution zijn alle *.md (Markdown) bestanden automatisch ter beschikking om eenvoudig de documentatie te kunnen onderhouden.

- StackReloaded.BuildingBlocks.sln (in src\All)
  - Dit solution bevat alle projecten onder de folder src, inclusief de voorbeeld projecten onder folder Examples.
  - Dit solution wordt gebruikt om alle projecten te builden en om alle testen uit te voeren.

# Topics (in alfabetische volgorde)
- [DataStore](src/DataStore/README)