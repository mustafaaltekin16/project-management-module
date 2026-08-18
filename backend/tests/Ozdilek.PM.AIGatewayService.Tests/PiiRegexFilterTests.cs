using FluentAssertions;
using Ozdilek.PM.SharedKernel.Security;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class PiiRegexFilterTests
{
    // 10000000146 is a well-known checksum-valid test TCKN (not a real person's ID) — used because
    // the filter now validates the official checksum, not just "11 digits".
    [Theory]
    [InlineData("Müşteri TCKN: 10000000146 numaralı kişi", "TCKN")]
    [InlineData("İletişim: ahmet.gorur@example.com adresinden ulaşılabilir", "EMAIL")]
    [InlineData("Telefon: 0532 123 45 67", "PHONE")]
    [InlineData("IBAN: TR33 0006 1005 1978 6457 8413 26", "IBAN")]
    public void Detect_FindsExpectedCategory(string input, string expectedCategory)
    {
        var matches = PiiRegexFilter.Detect(input);

        matches.Should().Contain(m => m.Category == expectedCategory);
    }

    [Fact]
    public void Detect_OnCleanText_ReturnsEmpty()
    {
        var matches = PiiRegexFilter.Detect("Proje açıklaması: yeni mağaza açılışı için fizibilite çalışması.");

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Detect_DoesNotFlagArbitraryElevenDigitRun()
    {
        // Regression test: an 11-digit substring that isn't a checksum-valid TCKN (e.g. the tail of a
        // GUID segment) must not be flagged — this exact shape corrupted a real projectId in production.
        var matches = PiiRegexFilter.Detect("projectId 90a49419-045f-43b9-9e5e-54740359167c");

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Redact_ReplacesMatchWithCategoryPlaceholder()
    {
        var redacted = PiiRegexFilter.Redact("TCKN 10000000146 ile kayıtlı.");

        redacted.Should().Contain("[REDACTED:TCKN]");
        redacted.Should().NotContain("10000000146");
    }

    [Fact]
    public void Redact_OnCleanText_ReturnsUnchanged()
    {
        const string text = "Proje açıklaması hassas veri içermiyor.";

        var redacted = PiiRegexFilter.Redact(text);

        redacted.Should().Be(text);
    }
}
