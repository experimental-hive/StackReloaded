# Performance

## Structuur
- De performance projecten dienen zich allemaal te bevinden onder folder `perf` naast folder `src`.
- De projectnaam van een performance project moet eindigen met `Performance`.

## Packages
Geen NuGet packages manueel toevoegen aan het performance project.
Volgende packages worden automatisch toegevoegd (zie `/eng/CSharp.Common.props`) voor performance projecten gekenmerkt door de projectnaam te laten eindigen met `Performance`.

- [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html) - An micro benchmark framework.



