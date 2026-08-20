using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Propyka.Api.Common;

public static partial class SlugGenerator
{
    /// <summary>
    /// "3 BHK Villa, Anna Nagar!" -> "3-bhk-villa-anna-nagar-a1b2c3".
    /// The suffix guarantees uniqueness without a database round-trip.
    /// </summary>
    public static string Create(string title, int maxLength = 150)
    {
        var normalised = title.Normalize(NormalizationForm.FormD);

        var withoutAccents = new string(normalised
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        var slug = NonSlugCharacters().Replace(withoutAccents.ToLowerInvariant(), "-");
        slug = MultipleDashes().Replace(slug, "-").Trim('-');

        var suffix = Guid.NewGuid().ToString("N")[..6];

        if (slug.Length > maxLength - 7)
        {
            slug = slug[..(maxLength - 7)].Trim('-');
        }

        return string.IsNullOrEmpty(slug) ? suffix : $"{slug}-{suffix}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleDashes();
}