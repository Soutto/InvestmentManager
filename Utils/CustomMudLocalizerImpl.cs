using Microsoft.Extensions.Localization;
using MudBlazor;

namespace InvestmentManager.Utils
{
    /// <summary>
    /// A custom implementation of the <see cref="MudLocalizer"/> class for managing localized strings in MudBlazor components.
    /// This class is designed to handle translations for specific terms and phrases used in MudBlazor components, with
    /// a primary focus on supporting the Portuguese language.
    /// </summary>
    /// <remarks>
    /// - By default, this implementation checks the current UI culture of the application.
    /// - If the culture is Portuguese ("pt"), it maps predefined keys to their respective translations.
    /// - If no match is found or the culture is not Portuguese, it falls back to the default key value.
    /// 
    /// Example usage:
    /// ```csharp
    /// var localizer = new CustomMudBlazorLocalizer();
    /// var localizedString = localizer["Rows per page"];
    /// Console.WriteLine(localizedString); // Output: "Itens por página" (if culture is Portuguese)
    /// ```
    /// 
    /// This implementation can be extended by adding more keys and translations to the `_localization` dictionary.
    /// </remarks>
    internal class CustomMudBlazorLocalizer : MudLocalizer
    {
        /// <summary>
        /// A dictionary containing the key-value pairs for localized strings, 
        /// where the key represents the term in English and the value is its translation in Portuguese.
        /// </summary>
        private readonly Dictionary<string, string> _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMudBlazorLocalizer"/> class.
        /// The constructor preloads a dictionary with translations for common MudBlazor terms.
        /// </summary>
        public CustomMudBlazorLocalizer()
        {
            _localization = new()
            {
                { "Rows per page", "Itens por página" }
            };
        }

        /// <summary>
        /// Retrieves a localized string based on the current UI culture.
        /// </summary>
        /// <param name="key">The key for the term to be localized.</param>
        /// <returns>
        /// A <see cref="LocalizedString"/> object representing the localized string. 
        /// If the key is found in the localization dictionary and the culture is Portuguese, the translation is returned.
        /// Otherwise, the key itself is returned as the fallback.
        /// </returns>
        public override LocalizedString this[string key]
        {
            get
            {
                var currentCulture = Thread.CurrentThread.CurrentUICulture.Parent.TwoLetterISOLanguageName;
                if (currentCulture.Equals("pt", StringComparison.InvariantCultureIgnoreCase)
                    && _localization.TryGetValue(key, out var res))
                {
                    return new LocalizedString(key, res);
                }
                else
                {
                    return new LocalizedString(key, key, true);
                }
            }
        }
    }
}