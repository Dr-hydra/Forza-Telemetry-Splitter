using System.Globalization;

namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// UI language. Auto follows the Windows display language; the rest force a specific language.
/// </summary>
public enum AppLanguage
{
    Auto,
    English,
    Japanese,
    French,
    German,
    Spanish,
}

public static class AppLanguageExtensions
{
    /// <summary>
    /// Resolve the culture to use for the UI. Auto reads the OS UI language and maps it to one of the
    /// supported languages, falling back to English if it isn't one we ship.
    /// </summary>
    public static CultureInfo ToCulture(this AppLanguage language)
    {
        string code = language switch
        {
            AppLanguage.English => "en",
            AppLanguage.Japanese => "ja",
            AppLanguage.French => "fr",
            AppLanguage.German => "de",
            AppLanguage.Spanish => "es",
            _ => ResolveOsLanguage(),
        };
        return CultureInfo.GetCultureInfo(code);
    }

    /// <summary>The two-letter code of the OS UI language if supported, else "en".</summary>
    private static string ResolveOsLanguage()
    {
        string two = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return two is "ja" or "fr" or "de" or "es" or "en" ? two : "en";
    }
}
