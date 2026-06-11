using System.Collections.Generic;
using System.Globalization;
using TnmsPluginFoundation.Utils.UI.Chat;
using Wuling.Abstract.Tianshi.Localizer;

namespace TnmsPluginFoundation.Models.Localization;

/// <summary>
/// IStringLocalizer decorator that applies chat color tag conversion ({RED}, {GOLD}, ...) to resolved values. <br/>
/// The legacy TnmsLocalizationPlatform formatted translations with ChatColorUtil at load time,
/// while Wuling's localizer returns raw values - this decorator keeps lang files written with
/// color tags working unchanged.
/// </summary>
internal sealed class ColorFormattingStringLocalizer(IStringLocalizer inner) : IStringLocalizer
{
    public LocalizedString this[string name] => Format(inner[name]);

    public LocalizedString this[string name, params object[] arguments] => Format(inner[name, arguments]);

    public LocalizedString ForPlayer(ulong steamId, string name) => Format(inner.ForPlayer(steamId, name));

    public LocalizedString ForPlayer(ulong steamId, string name, params object[] arguments)
        => Format(inner.ForPlayer(steamId, name, arguments));

    public LocalizedString ForCulture(string name, CultureInfo culture) => Format(inner.ForCulture(name, culture));

    public LocalizedString ForCulture(string name, CultureInfo culture, params object[] arguments)
        => Format(inner.ForCulture(name, culture, arguments));

    public IEnumerable<KeyValuePair<string, string>> GetAllStrings()
    {
        foreach (var pair in inner.GetAllStrings())
            yield return new KeyValuePair<string, string>(pair.Key, ChatColorUtil.FormatChatMessage(pair.Value));
    }

    public IEnumerable<KeyValuePair<string, string>> GetAllStrings(CultureInfo culture)
    {
        foreach (var pair in inner.GetAllStrings(culture))
            yield return new KeyValuePair<string, string>(pair.Key, ChatColorUtil.FormatChatMessage(pair.Value));
    }

    private static LocalizedString Format(LocalizedString source)
        => new(source.Name, ChatColorUtil.FormatChatMessage(source.Value), source.ResourceNotFound);
}
