using TaxFiler.Service;

namespace TaxFiler.Service.Test;

[TestFixture]
public class StringSimilarityTests
{
    [TestCase("hello", "hello", 1.0)]
    [TestCase("", "", 1.0)]
    [TestCase("hello", "", 0.0)]
    [TestCase("", "hello", 0.0)]
    [TestCase("hello", "world", 0.2)] // 4 out of 5 characters different
    [TestCase("kitten", "sitting", 0.571)] // Classic Levenshtein example
    [TestCase("Saturday", "Sunday", 0.625)] // 3 edits out of 8 characters
    public void LevenshteinSimilarity_ReturnsExpectedSimilarity(string source, string target, double expected)
    {
        var result = StringSimilarity.LevenshteinSimilarity(source, target);
        Assert.That(result, Is.EqualTo(expected).Within(0.001));
    }

    [Test]
    public void LevenshteinSimilarity_HandlesNullInputs()
    {
        Assert.That(StringSimilarity.LevenshteinSimilarity(null!, null!), Is.EqualTo(1.0));
        Assert.That(StringSimilarity.LevenshteinSimilarity("test", null!), Is.EqualTo(0.0));
        Assert.That(StringSimilarity.LevenshteinSimilarity(null!, "test"), Is.EqualTo(0.0));
    }

    [Test]
    public void LevenshteinSimilarity_IsCaseInsensitive()
    {
        var result = StringSimilarity.LevenshteinSimilarity("Hello", "HELLO");
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void LevenshteinSimilarity_HandlesGermanCharacters()
    {
        var result = StringSimilarity.LevenshteinSimilarity("Müller", "Mueller");
        Assert.That(result, Is.GreaterThan(0.8)); // Should be high similarity after normalization
    }

    [TestCase("Hello World", "world", true)]
    [TestCase("Hello World", "WORLD", true)]
    [TestCase("Hello World", "xyz", false)]
    [TestCase("", "test", false)]
    [TestCase("test", "", false)]
    public void ContainsIgnoreCase_ReturnsExpectedResult(string source, string target, bool expected)
    {
        var result = StringSimilarity.ContainsIgnoreCase(source, target);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ContainsIgnoreCase_HandlesNullInputs()
    {
        Assert.That(StringSimilarity.ContainsIgnoreCase(null!, "test"), Is.False);
        Assert.That(StringSimilarity.ContainsIgnoreCase("test", null!), Is.False);
        Assert.That(StringSimilarity.ContainsIgnoreCase(null!, null!), Is.False);
    }

    [TestCase("  Hello   World  ", "hello world")]
    [TestCase("HELLO WORLD", "hello world")]
    [TestCase("Hello, World!", "hello world")]
    [TestCase("Müller & Co.", "mueller co")]
    [TestCase("", "")]
    public void NormalizeForMatching_ReturnsExpectedResult(string input, string expected)
    {
        var result = StringSimilarity.NormalizeForMatching(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NormalizeForMatching_HandlesNullInput()
    {
        var result = StringSimilarity.NormalizeForMatching(null!);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeForMatching_RemovesDiacritics()
    {
        var result = StringSimilarity.NormalizeForMatching("Café Müller Straße");
        Assert.That(result, Is.EqualTo("cafe muller strasse"));
    }

    [Test]
    public void NormalizeForMatching_RemovesPunctuation()
    {
        var result = StringSimilarity.NormalizeForMatching("Hello, World! (Test)");
        Assert.That(result, Is.EqualTo("hello world test"));
    }

    [Test]
    public void NormalizeForMatching_HandlesMultipleSpaces()
    {
        var result = StringSimilarity.NormalizeForMatching("Hello    World   Test");
        Assert.That(result, Is.EqualTo("hello world test"));
    }

    // Integration tests for realistic German business scenarios
    [TestCase("REWE Markt GmbH", "rewe markt", 1.0)] // Exact match after normalization
    [TestCase("Müller & Co. KG", "Mueller Co", 1.0)] // Diacritics and punctuation
    [TestCase("Deutsche Bahn AG", "DB AG", 0.0)] // Different abbreviations - should not match exactly
    public void LevenshteinSimilarity_GermanBusinessNames(string source, string target, double expectedMinimum)
    {
        var result = StringSimilarity.LevenshteinSimilarity(source, target);
        if (expectedMinimum == 1.0)
        {
            Assert.That(result, Is.EqualTo(1.0));
        }
        else
        {
            Assert.That(result, Is.GreaterThanOrEqualTo(expectedMinimum));
        }
    }

    [Test]
    public void ContainsIgnoreCase_GermanBusinessScenarios()
    {
        Assert.That(StringSimilarity.ContainsIgnoreCase("REWE Markt GmbH & Co. KG", "REWE"), Is.True);
        Assert.That(StringSimilarity.ContainsIgnoreCase("Müller Drogeriemarkt", "müller"), Is.True);
        Assert.That(StringSimilarity.ContainsIgnoreCase("Deutsche Post DHL", "post"), Is.True);
    }
}