using System.Text.RegularExpressions;

namespace Ozdilek.PM.SharedKernel.Security;

public sealed record PiiMatch(string Category, string MatchedText);

/// <summary>
/// Regex-based KVKK (Turkish personal data protection law, 6698) filter. Applied to any text sent to
/// an LLM provider — both to redact it before the call and to record what was redacted in the prompt
/// audit log. This is a defense-in-depth guard, not a substitute for the source system also avoiding
/// sending sensitive fields in the first place. Lives in SharedKernel (zero external dependencies) so
/// both the Application layer (redacting the assembled prompt) and the ASP.NET Core middleware
/// (redacting the raw inbound request body) can use the exact same rules.
///
/// TCKN and credit-card candidates are checksum-validated (not just length-matched): an 11-digit
/// substring appears constantly in ordinary data that is not a Turkish ID number — notably inside
/// GUIDs, which routinely contain an 11-digit run purely by chance. Without the checksum, redacting
/// "any 11 digits" corrupted request payloads that legitimately carry a GUID (e.g. a projectId),
/// turning a valid request body into invalid JSON. The checksum cuts the false-positive rate from
/// "matches routinely" to "matches only real, validly-structured numbers".
/// </summary>
public static class PiiRegexFilter
{
    private static readonly Regex TcknCandidate = new(@"(?<!\d)[1-9]\d{10}(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Email = new(@"[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+", RegexOptions.Compiled);
    private static readonly Regex TurkishPhone = new(@"(?<!\d)(\+?90|0)?\s?5\d{2}[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Iban = new(@"\bTR\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{2}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CreditCardCandidate = new(@"\b(?:\d[ -]?){13,16}\b", RegexOptions.Compiled);

    private static readonly (string Category, Regex Pattern, Func<string, bool>? Validate)[] Patterns =
    [
        ("TCKN", TcknCandidate, IsValidTckn),
        ("EMAIL", Email, null),
        ("PHONE", TurkishPhone, null),
        ("IBAN", Iban, null),
        ("CREDIT_CARD", CreditCardCandidate, IsValidLuhn)
    ];

    public static IReadOnlyList<PiiMatch> Detect(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var matches = new List<PiiMatch>();
        foreach (var (category, pattern, validate) in Patterns)
        {
            foreach (Match match in pattern.Matches(input))
            {
                if (validate is null || validate(match.Value))
                {
                    matches.Add(new PiiMatch(category, match.Value));
                }
            }
        }

        return matches;
    }

    /// <summary>Returns the input with every detected match replaced by a category placeholder, e.g. "[REDACTED:TCKN]".</summary>
    public static string Redact(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var redacted = input;
        foreach (var (category, pattern, validate) in Patterns)
        {
            redacted = validate is null
                ? pattern.Replace(redacted, $"[REDACTED:{category}]")
                : pattern.Replace(redacted, m => validate(m.Value) ? $"[REDACTED:{category}]" : m.Value);
        }

        return redacted;
    }

    /// <summary>Official Turkish TCKN checksum (not just "11 digits") — see the class remarks for why this matters.</summary>
    private static bool IsValidTckn(string candidate)
    {
        if (candidate.Length != 11 || candidate[0] == '0')
        {
            return false;
        }

        Span<int> digits = stackalloc int[11];
        for (var i = 0; i < 11; i++)
        {
            if (!char.IsDigit(candidate[i]))
            {
                return false;
            }
            digits[i] = candidate[i] - '0';
        }

        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var digit10 = ((oddSum * 7) - evenSum) % 10;
        if (digit10 < 0)
        {
            digit10 += 10;
        }

        if (digit10 != digits[9])
        {
            return false;
        }

        var sumFirstTen = 0;
        for (var i = 0; i < 10; i++)
        {
            sumFirstTen += digits[i];
        }

        return sumFirstTen % 10 == digits[10];
    }

    /// <summary>Standard Luhn checksum, so an arbitrary 13-16 digit run (e.g. inside a GUID) isn't flagged as a card number.</summary>
    private static bool IsValidLuhn(string candidate)
    {
        Span<int> digits = stackalloc int[candidate.Length];
        var count = 0;
        foreach (var c in candidate)
        {
            if (char.IsDigit(c))
            {
                digits[count++] = c - '0';
            }
        }

        if (count is < 13 or > 19)
        {
            return false;
        }

        var sum = 0;
        var alternate = false;
        for (var i = count - 1; i >= 0; i--)
        {
            var value = digits[i];
            if (alternate)
            {
                value *= 2;
                if (value > 9)
                {
                    value -= 9;
                }
            }
            sum += value;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}
