using System;
using Microsoft.Extensions.DependencyInjection;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Interfaces;
using TnmsPluginFoundation.Models.Command;

namespace TnmsPluginFoundation.Models.Plugin;

/// <summary>
/// This is a base class for all plugin modules. you can make custom module class from this.
/// </summary>
/// <param name="serviceProvider">Microsoft.Extensions.DependencyInjection</param>
/// <param name="hotReload">Is this module being loaded during a hot reload?</param>
public abstract class PluginModuleBase(IServiceProvider serviceProvider, bool hotReload) : PluginTranslatableFeatureBase(serviceProvider), IPluginModule
{
    /// <summary>
    /// Indicates whether this module was loaded during a hot reload
    /// </summary>
    protected bool HotReload { get; } = hotReload;

    /// <summary>
    /// This string is used to saving config and internal management.<br/>
    /// When we specify HelloModule as name, then ConVar config name become HelloModule.cfg
    /// </summary>
    public abstract string PluginModuleName { get; }
    
    /// <summary>
    /// This string is used as a prefix for printing to the in-game chat.
    /// </summary>
    public abstract string ModuleChatPrefix { get; } 
    
    /// <summary>
    /// Is ModuleChatPrefix is translation key?
    /// </summary>
    protected abstract bool UseTranslationKeyInModuleChatPrefix { get; }


    /// <summary>
    /// ConVarConfigurationService
    /// </summary>
    private ConVarConfigurationService ConVarConfigurationService => Plugin.ConVarConfigurationService;
    
    /// <summary>
    /// This method will call while registering module, and module registration is called in plugins Load method.
    /// Also, we can get ConVarConfigurationService and AbstractTunaPluginBase from DI container from this method.
    /// </summary>
    /// <param name="services">ServiceCollection</param>
    public virtual void RegisterServices(IServiceCollection services) {}

    /// <summary>
    /// This method will call while BasePlugin's OnAllPluginsLoaded.
    /// This serviceProvider should contain latest and all module dependency.
    /// </summary>
    /// <param name="services">Latest DI container</param>
    public virtual void UpdateServices(IServiceProvider services)
    {
        ServiceProvider = services;
    }

    /// <summary>
    /// Module initialization method (Internal)
    /// </summary>
    public void Initialize()
    {
        OnInitialize();
    }

    /// <summary>
    /// This method will call while registering module, and module registration is called from plugin's Load method.
    /// Also, this time you can call TrackConVar() method to add specific ConVar to ConVar config file automatic generation.
    /// </summary>
    protected virtual void OnInitialize()
    {
        // For instance
        // TrackConVar(ConVarVariableName);
    }


    /// <summary>
    /// Called when all plugins loaded (Internal)
    /// </summary>
    public void AllPluginsLoaded()
    {
        OnAllModulesLoaded();
    }
    
    /// <summary>
    /// Called when all plugins loaded, so you can late initialize your module.<br/>
    /// For instance, obtaining the ModSharp module, or registered self services.
    /// </summary>
    protected virtual void OnAllModulesLoaded()
    {
        
    }

    /// <summary>
    /// Called when unloading module (Internal)
    /// </summary>
    public void UnloadModule()
    {
        OnUnloadModule();
        ConVarConfigurationService.UntrackModule(PluginModuleName);
    }

    
    /// <summary>
    /// Called when unloading module
    /// </summary>
    protected virtual void OnUnloadModule(){}

    /// <summary>
    /// Register command that inherited TnmsAbstractCommandBase
    /// </summary>
    /// <typeparam name="T"></typeparam>
    protected void RegisterTnmsCommand<T>() where T : TnmsAbstractCommandBase
    {
        var module = (T)Activator.CreateInstance(typeof(T), ServiceProvider)!;
        Plugin.AddTnmsCommand(module);
    }

    /// <summary>
    /// Automatically discovers and registers all TnmsAbstractCommandBase-derived classes under the specified namespace.
    /// </summary>
    /// <param name="nameSpace">The namespace to search for commands</param>
    /// <param name="includeSubNamespaces">If true, includes classes from sub-namespaces. Default is false (only direct namespace).</param>
    protected void AddCommandsUnderNamespace(string nameSpace, bool includeSubNamespaces = false)
    {
        Plugin.AddTnmsCommandsUnderNamespace(nameSpace, includeSubNamespaces);
    }
    
    /// <summary>
    /// Add ConVar to tracking list. if you want to generate config automatically, then call this method with ConVar that you wanted to track.
    /// </summary>
    /// <param name="conVar">Any FakeConVar</param>
    protected void TrackConVar(IConVar conVar)
    {
        ConVarConfigurationService.TrackConVar(conVar, PluginModuleName);
    }

    /// <summary>
    /// Create and track convar automatically
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <param name="helpString"></param>
    /// <param name="cvarFlags"></param>
    /// <returns>returns null if failed to create, otherwise its instance of ConVar</returns>
    protected IConVar? CreateAndTrackConVar(string name, string defaultValue, string? helpString = null, ConVarFlags? cvarFlags = null)
    {
        var newCvar = SharedSystem.GetConVarManager().CreateConVar(name, defaultValue, helpString, cvarFlags);
        if (newCvar != null)
        {
            TrackConVar(newCvar);
        }
        
        return newCvar;
    }


    /// <summary>
    /// Removes all module ConVar from tracking list.
    /// </summary>
    protected void UnTrackConVar()
    {
        ConVarConfigurationService.UntrackModule(PluginModuleName);
    }


    /// <summary>
    /// Helper method for sending localized text with module prefix to all players.
    /// </summary>
    /// <param name="localizationKey">Language localization key</param>
    /// <param name="args">Any args that can be use ToString()</param>
    protected void PrintLocalizedChatToAllWithModulePrefix(string localizationKey)
    {
        
        foreach (var client in Plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true, true))
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            var playerController = client.GetPlayerController();
            
            if (playerController == null)
                continue;
            
            playerController.PrintToChat(GetTextWithModulePrefix(client, LocalizeString(client, localizationKey)));
        }
    }
    
    /// <summary>
    /// Helper method for sending localized text with module prefix to all players.
    /// </summary>
    /// <param name="localizationKey">Language localization key</param>
    /// <param name="args">Any args that can be use ToString()</param>
    protected void PrintLocalizedChatToAllWithModulePrefix(string localizationKey, params object[] args)
    {
        
        foreach (var client in Plugin.SharedSystem.GetModSharp().GetIServer().GetGameClients(true, true))
        {
            if (client.IsFakeClient || client.IsHltv)
                continue;

            var playerController = client.GetPlayerController();
            
            if (playerController == null)
                continue;
            
            playerController.PrintToChat(GetTextWithModulePrefix(client, LocalizeString(client, localizationKey, args)));
        }
    }
    

    /// <summary>
    /// Helper method for obtain the localized text.
    /// </summary>
    /// <param name="player">Player instance, If null it will use server language</param>
    /// <param name="localizationKey">Language localization key</param>
    /// <returns></returns>
    protected string LocalizeWithModulePrefix(IGameClient? player, string localizationKey)
    {
        return GetTextWithModulePrefix(player, LocalizeString(player, localizationKey));
    }
    
    /// <summary>
    /// Get text with module prefix.
    /// </summary>
    /// <param name="player">Player Instance</param>
    /// <param name="text">original text</param>
    /// <returns>Text combined with original text and prefix, returns translated module prefix if UseTranslationKeyInModuleChatPrefix is true</returns>
    public string GetTextWithModulePrefix(IGameClient? player, string text)
    {
        if (!UseTranslationKeyInModuleChatPrefix)
            return $" {ModuleChatPrefix} {text}";

        return $" {LocalizeString(player, ModuleChatPrefix)} {text}";
    }

    /// <summary>
    /// Helper method for obtain the localized text.
    /// </summary>
    /// <param name="player">Player instance, If null it will use server language</param>
    /// <param name="localizationKey">Language localization key</param>
    /// <param name="args">Any args that can be use ToString()</param>
    /// <returns></returns>
    protected string LocalizeWithModulePrefix(IGameClient? player, string localizationKey, params object[] args)
    {
        return GetTextWithModulePrefix(player, LocalizeString(player, localizationKey, args));
    }
}