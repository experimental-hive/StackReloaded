# Testing

## Structuur
- De test projecten dienen zich allemaal te bevinden onder folder `test` naast folder `src`.
- De projectnaam van een test project moet eindigen met `Tests`.
  - Een unit test project moet eindigen met `.UnitTests`
  - Een integration test project moet eindigen met `.IntegrationTests`

## Packages
Geen NuGet packages manueel toevoegen aan het test project.
Volgende packages worden automatisch toegevoegd (zie `/eng/CSharp.Common.props`) voor test projecten gekenmerkt door de projectnaam te laten eindigen met `Tests`.

- [FluentAssertions](https://fluentassertions.com/) - An assertions framework.
- [Microsoft.AspNetCore.Mvc.Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.0) - Support for writing functional tests for MVC applications.
- [Moq](https://github.com/Moq/moq4/wiki/Quickstart) - An mocking framework.
- [xunit](https://xunit.net/) - A testrunner.

## Unit Test Code Guidelines in a nutshell

```csharp
using System;
using FluentAssertions;
using Xunit;

namespace XeriusGroup.Example.Tests
{
    // Test class naming: [ClassAsUnitUnderTest]Tests
    public class PalindromeDetectorTests
    {
        // Test method naming: [MethodUnderTest]_[Context01]_[Context02]..._[Expectation]
        // Async suffix dropped
        // No naming like Given...When...Then...
        // No naming like (It)Should...When...
        [Fact]
        public void IsPalindrome_ForPalindromeString_ReturnsTrue()
        {
            // In the Arrange phase, we create and set up a system under test.
            // A system under test could be a method, a single object, or a graph of connected objects.
            // It is OK to have an empty Arrange phase, for example if we are testing a static method -
            // in this case SUT already exists in a static form and we don't have to initialize anything explicitly.
            PalindromeDetector detector = new PalindromeDetector();

            // The Act phase is where we poke the system under test, usually by invoking a method.
            // If this method returns something back to us, we want to collect the result to ensure it was correct.
            // Or, if method doesn't return anything, we want to check whether it produced the expected side effects.
            bool isPalindrome = detector.IsPalindrome("kayak");

            // The Assert phase makes our unit test pass or fail.
            // Here we check that the method's behavior is consistent with expectations.
            isPalindrome.Should().BeTrue();
        }
    }
}
```

## Resources
- [Unit Tests, How to Write Testable Code and Why it Matters](https://www.toptal.com/qa/how-to-write-testable-code-and-why-it-matters)


