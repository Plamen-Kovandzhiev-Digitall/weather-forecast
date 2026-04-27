using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WeatherForecast.Utilities;

/// <summary>
/// Provides deterministic methods to shorten arbitrary strings to a maximum of 64 characters,
/// suitable for use as unique identifiers or keys.
/// </summary>
public static partial class StringShortener
{
    private const int MaxLength = 64;
    private const int DefaultHashLength = 16;
    private const char Separator = '_';

    /// <summary>
    /// Converts input to a unique 64-character hex key using SHA-256.
    /// Returns the original string unchanged if it is already ≤ 64 characters.
    /// </summary>
    public static string ToUniqueKey(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length <= MaxLength)
            return input;

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Converts input to a readable unique key: a human-readable prefix from the original text
    /// followed by a SHA-256 hex hash suffix. Returns the original string if ≤ 64 characters.
    /// </summary>
    /// <param name="input">The input string to shorten.</param>
    /// <param name="hashLength">
    /// Length of the hex hash suffix (default 16 = 64 bits).
    /// Longer hash = fewer collisions but shorter readable prefix.
    /// </param>
    public static string ToReadableUniqueKey(string input, int hashLength = DefaultHashLength)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfLessThan(hashLength, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hashLength, MaxLength - 2); // at least 1 char prefix + separator

        if (input.Length <= MaxLength)
            return input;

        var prefixLength = MaxLength - 1 - hashLength;
        var hash = ComputeHexHash(input, hashLength);
        var prefix = BuildPrefix(input, prefixLength);

        return $"{prefix}{Separator}{hash}";
    }

    private static string BuildPrefix(string input, int length)
    {
        var cleaned = SeparatorPattern().Replace(input, "-").Trim('-');

        return cleaned.Length <= length
            ? cleaned.PadRight(length, '-')
            : cleaned[..length];
    }

    private static string ComputeHexHash(string input, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant()[..length];
    }

    [GeneratedRegex(@"[\s:;,./\\]+")]
    private static partial Regex SeparatorPattern();
}
