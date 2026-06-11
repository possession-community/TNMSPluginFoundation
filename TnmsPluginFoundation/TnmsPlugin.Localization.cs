using Sharp.Shared.Objects;

namespace TnmsPluginFoundation;

public abstract partial class TnmsPlugin
{
    /// <summary>
    /// Same as Plugin.Localizer[langaugeKey]
    /// </summary>
    /// <param name="localizationKey">Localization Key</param>
    /// <returns>Translated result</returns>
    public string LocalizeString(string localizationKey)
    {
        return Localizer[localizationKey];
    }

    /// <summary>
    /// Same as Plugin.Localizer[langaugeKey, args]
    /// </summary>
    /// <param name="localizationKey">Localization Key</param>
    /// <param name="args">Any args that can be use ToString()</param>
    /// <returns>Translated result</returns>
    public string LocalizeString(string localizationKey, params object[] args)
    {
        return Localizer[localizationKey, args];
    }
    
    /// <summary>
    /// Same as Plugin.Localizer.ForPlayer(player, localizationKey, args)
    /// </summary>
    /// <param name="client">Player instance</param>
    /// <param name="localizationKey">Localization Key</param>
    /// <returns>Translated result as player's language</returns>
    public string LocalizeStringForPlayer(IGameClient? client, string localizationKey)
    {
        if  (client == null)
            return Localizer[localizationKey];

        return Localizer.ForPlayer(client.SteamId, localizationKey);
    }
    
    /// <summary>
    /// Same as Plugin.Localizer.ForPlayer(player, localizationKey, args)
    /// </summary>
    /// <param name="client">Player instance</param>
    /// <param name="localizationKey">Localization Key</param>
    /// <param name="args">Any args that can be use ToString()</param>
    /// <returns>Translated result as player's language</returns>
    public string LocalizeStringForPlayer(IGameClient? client, string localizationKey, params object[] args)
    {
        if  (client == null)
            return Localizer[localizationKey, args];

        return Localizer.ForPlayer(client.SteamId, localizationKey, args);
    }
}