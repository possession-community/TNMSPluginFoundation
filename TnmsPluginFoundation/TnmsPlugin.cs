using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.TargetingManager.Shared;
using Sharp.Shared;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using TnmsPluginFoundation.Interfaces;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Localization;
using TnmsPluginFoundation.Models.Logger;
using TnmsPluginFoundation.Models.Plugin;
using Wuling.Abstract;
using Wuling.Abstract.Tianshi.Authority;
using Wuling.Abstract.Tianshi.Localizer;

namespace TnmsPluginFoundation;

public abstract partial class TnmsPlugin: IModSharpModule
{
    protected TnmsPlugin(ISharedSystem  sharedSystem,
        string         dllPath,
        string         sharpPath,
        Version?       version,
        IConfiguration coreConfiguration,
        bool           hotReload)
    {
        ArgumentNullException.ThrowIfNull(dllPath);
        ArgumentNullException.ThrowIfNull(sharpPath);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(coreConfiguration);
        ArgumentNullException.ThrowIfNull(sharedSystem);
        _sharedSystem = sharedSystem;
        _hotReload = hotReload;
        ModuleDirectory = dllPath;

        var   factory = _sharedSystem.GetLoggerFactory();

        // ReSharper disable VirtualMemberCallInConstructor
        var   logger  = factory.CreateLogger(DisplayName);
        // ReSharper restore VirtualMemberCallInConstructor

        Logger = logger;
        GameData = _sharedSystem.GetModSharp().GetGameData();
        AssetPath = Path.Combine(sharpPath, "assets");
    }

    private readonly bool _hotReload;

    /// <summary>
    /// ModSharp Shared system / Shared Modules
    /// </summary>
    internal static ISharedSystem StaticSharedSystem => _sharedSystem;

    /// <summary>
    /// Wuling framework facade. (Authority / Localizer / Registry / EventBus / etc.)
    /// </summary>
    public static IWuling Wuling { get; private set; } = null!;

    /// <summary>
    /// ModSharp official targeting module. (Sharp.Modules.TargetingManager)
    /// </summary>
    public static ITargetingManager TargetingManager { get; private set; } = null!;

    /// <summary>
    /// Wuling Authority. (permission / can-target / immunity / group management)
    /// </summary>
    public static IAuthority AdminManager { get; private set; } = null!;


    public IStringLocalizer Localizer { get; private set; } = null!;
    public ILocalizer LocalizationPlatform { get; private set; } = null!;
    public string ModuleDirectory { get; }


    /*
     * Mod Sharp Related
     */
    public ISharedSystem SharedSystem => _sharedSystem;
    private static ISharedSystem _sharedSystem = null!;

    public IConVarManager ConVarManager => _sharedSystem.GetConVarManager();
    
    
    public abstract string DisplayName { get; }
    public abstract string DisplayAuthor { get; }
    
    
    /// <summary>
    /// ConVarConfigurationService for managing ConVar config.
    /// </summary>
    public ConVarConfigurationService ConVarConfigurationService { get; private set; } = null!;
    
    /// <summary>
    /// DebugLogger instance
    /// </summary>
    public IDebugLogger? DebugLogger { get; protected set; }

    /// <summary>
    /// Plugin Logger
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Tnms Logger
    /// </summary>
    public TnmsLogger TnmsLogger { get; private set; } = null!;

    /// <summary>
    /// ModSharp shared gamedata system
    /// </summary>
    public IGameData GameData { get; }
    
    /// <summary>
    /// Asset path
    /// </summary>
    public string AssetPath { get; }
    
    /// <summary>
    /// Base config directory path you can use whatever.
    /// </summary>
    public abstract string BaseCfgDirectoryPath { get; }
    
    /// <summary>
    /// ConVar configuration path, this path is used for saving All ConVar config. <br/>
    /// Relative path from game/csgo/cfg/ <br/>
    /// Also if this path defined a specific file, then module config is not generated. <br/>
    /// If your plugin doesn't use any ConVar, you can return empty string. <br/>
    /// </summary>
    public abstract string ConVarConfigPath { get; }

    private ServiceCollection ServiceCollection { get; } = new();
    
    /// <summary>
    /// DI Container
    /// </summary>
    private ServiceProvider ServiceProvider { get; set; } = null!;
    
    /// <summary>
    /// This prefix used for printing to chat.
    /// </summary>
    public abstract string PluginPrefix { get; }
    
    /// <summary>
    /// Is PluginPrefix is translation key?
    /// </summary>
    public abstract bool UseTranslationKeyInPluginPrefix { get; }

    public string GetPluginPrefix(IGameClient? client = null)
    {
        if (UseTranslationKeyInPluginPrefix)
            return LocalizeStringForPlayer(client, PluginPrefix);
        
        return PluginPrefix;
    }


    /// <summary>
    /// We can register required services that use entire plugin or modules.
    /// At this time, we can get ConVarConfigurationService and AbstractTnmsPluginBase instance from DI container in this method.
    /// </summary>
    protected virtual void RegisterRequiredPluginServices(IServiceCollection collection ,IServiceProvider provider){}
    
    /// <summary>
    /// You can register any services. for instance: external feature obtained from ModSharp's ModuleManager.<br/>
    /// This is a final chance of registering services to DI container.
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="provider"></param>
    protected virtual void LateRegisterPluginServices(IServiceCollection collection, IServiceProvider provider){}

    private void UpdateServices()
    {
        foreach (PluginModuleBase module in _loadedModules)
        {
            module.UpdateServices(ServiceProvider);
        }
    }

    /// <summary>
    /// ModShaprp's module initialization
    /// </summary>
    /// <returns></returns>
    public bool Init()
    {
        ConVarConfigurationService = new(this);
        // Add self and core service to DI Container
        ServiceCollection.AddSingleton(this);
        ServiceCollection.AddSingleton(ConVarConfigurationService);
        
        // Build first ServiceProvider, because we need a plugin instance to initialize modules
        RebuildServiceProvider();
        
        // Then call register required plugin services
        RegisterRequiredPluginServices(ServiceCollection, ServiceProvider);

        DebugLogger ??= new IgnoredLogger();

        RegisterDebugLogger(DebugLogger);
        
        // And build again
        RebuildServiceProvider();

        TnmsLogger = new TnmsLogger(this);
        
        // Call customizable OnLoad method
        TnmsOnPluginLoad(_hotReload);
        return true;
    }

    /// <summary>
    /// This method is can be used to initialize plugin feature.
    /// </summary>
    /// <param name="hotReload">Is hot reload?</param>
    protected virtual void TnmsOnPluginLoad(bool hotReload){}


    /// <summary>
    /// ModSharp's module initialization after all plugins loaded
    /// </summary>
    public void PostInit()
    {
        TnmsLateOnPluginLoad(ServiceProvider);
    }

    /// <summary>
    /// You can register custom module here.
    /// All modules registered DI dependency is accessible in here.
    /// </summary>
    protected virtual void TnmsLateOnPluginLoad(ServiceProvider provider){}



    /// <summary>
    /// ModSharp's module initialization after all plugins loaded.
    /// </summary>
    /// <returns></returns>
    public void OnAllModulesLoaded()
    {
        LateRegisterPluginServices(ServiceCollection, ServiceProvider);
        RebuildServiceProvider();
        UpdateServices();
        
        TnmsAllPluginsLoaded(_hotReload);
        CallModulesAllPluginsLoaded();
        ConVarConfigurationService.SaveAllConfigToFile();
        ConVarConfigurationService.ExecuteConfigs();

        var wuling = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<IWuling>(IWuling.Identity).Instance;
        Wuling = wuling ?? throw new InvalidOperationException("Wuling is not found! Make sure Wuling is installed!");

        AdminManager = Wuling.Authority;
        LocalizationPlatform = Wuling.Localizer;
        // Wrap with color tag conversion to keep parity with the legacy
        // TnmsLocalizationPlatform, which formatted translations at load time.
        Localizer = new ColorFormattingStringLocalizer(LocalizationPlatform.CreateStringLocalizer(ModuleDirectory));

        var targetingManager = _sharedSystem.GetSharpModuleManager()
            .GetRequiredSharpModuleInterface<ITargetingManager>(ITargetingManager.Identity).Instance;
        TargetingManager = targetingManager ?? throw new InvalidOperationException("TargetingManager is not found! Make sure Sharp.Modules.TargetingManager is installed!");
    }

    /// <summary>
    /// This method is can be used to late initialize plugin feature.
    /// </summary>
    /// <param name="hotReload">Is hot reload?</param>
    protected virtual void TnmsAllPluginsLoaded(bool hotReload){}


    /// <summary>
    /// ModSharp's module shutdown.
    /// </summary>
    /// <returns></returns>
    public void Shutdown()
    {
        TnmsOnPluginUnload(_hotReload);
        StopAllTimers();
        UnloadAllModules();
        
        // Use reverse iteration to avoid collection modification issues
        foreach (var tnmsAbstractedClientCommand in TnmsAbstractedClientCommands)
        {
            RemoveTnmsCommand(tnmsAbstractedClientCommand.Value);
        }
        TnmsAbstractedClientCommands.Clear();
        
        foreach (var tnmsAbstractedServerCommand in TnmsAbstractedServerCommands)
        {
            RemoveTnmsCommand(tnmsAbstractedServerCommand.Value);
        }
        TnmsAbstractedServerCommands.Clear();
        
        ServiceProvider.Dispose();
    }

    /// <summary>
    /// This method is can be used to unload plugin feature.
    /// </summary>
    /// <param name="hotReload">Is hot reload?</param>
    protected virtual void TnmsOnPluginUnload(bool hotReload){}


    private void RebuildServiceProvider()
    {
        ServiceProvider = ServiceCollection.BuildServiceProvider();
    }
    

    private readonly HashSet<PluginModuleBase> _loadedModules = [];

    /// <summary>
    /// Register module.
    /// </summary>
    /// <typeparam name="T">Any classes that inherited a PluginModuleBase</typeparam>
    protected T RegisterModule<T>() where T : PluginModuleBase
    {
        var moduleType = typeof(T);

        T module = (T)ActivatorUtilities.CreateInstance(ServiceProvider, moduleType, ServiceProvider, _hotReload);

        _loadedModules.Add(module);
        module.Initialize();
        module.RegisterServices(ServiceCollection);
        RebuildServiceProvider();
        Logger.LogInformation($"{module.PluginModuleName} has been initialized");
        return module;
    }

    /// <summary>
    /// Helper method to discover types under a specific namespace that inherit from a base type.
    /// </summary>
    /// <typeparam name="TBase">The base type to search for</typeparam>
    /// <param name="assembly">The assembly to search in</param>
    /// <param name="nameSpace">The namespace to search</param>
    /// <param name="includeSubNamespaces">If true, includes classes from sub-namespaces</param>
    /// <returns>List of types that match the criteria</returns>
    private List<Type> GetTypesUnderNamespace<TBase>(Assembly assembly, string nameSpace, bool includeSubNamespaces)
    {
        if (string.IsNullOrWhiteSpace(nameSpace))
            throw new ArgumentException("Namespace cannot be null or whitespace.", nameof(nameSpace));

        if (nameSpace.EndsWith(".", StringComparison.Ordinal))
            throw new ArgumentException("Namespace should not end with a period.", nameof(nameSpace));

        try
        {
            return assembly.GetTypes()
                .Where(t => t.Namespace != null &&
                            (includeSubNamespaces
                                ? t.Namespace == nameSpace || t.Namespace.StartsWith(nameSpace + ".", StringComparison.Ordinal)
                                : t.Namespace == nameSpace) &&
                            t.IsClass &&
                            !t.IsAbstract &&
                            typeof(TBase).IsAssignableFrom(t))
                .ToList();
        }
        catch (ReflectionTypeLoadException ex)
        {
            Logger.LogError(ex, "Failed to load types from assembly {assemblyName} while searching for {baseType} under namespace {namespace}",
                assembly.FullName, typeof(TBase).Name, nameSpace);

            foreach (var loaderException in ex.LoaderExceptions.Where(e => e != null))
            {
                Logger.LogError(loaderException, "Loader exception: {loaderExceptionMessage}", loaderException!.Message);
            }

            return new List<Type>();
        }
    }

    /// <summary>
    /// Helper method to discover and process types under a namespace.
    /// </summary>
    /// <typeparam name="TBase">The base type to search for</typeparam>
    /// <param name="nameSpace">The namespace to search</param>
    /// <param name="includeSubNamespaces">If true, includes classes from sub-namespaces</param>
    /// <param name="typeDisplayName">Display name for logging (e.g., "module", "command")</param>
    /// <param name="processType">Action to process each discovered type</param>
    private void DiscoverAndProcessTypes<TBase>(string nameSpace, bool includeSubNamespaces, string typeDisplayName, Action<Type> processType)
    {
        var assembly = GetType().Assembly;
        var types = GetTypesUnderNamespace<TBase>(assembly, nameSpace, includeSubNamespaces);

        if (types.Count == 0)
        {
            Logger.LogWarning($"No {typeDisplayName}s found under namespace '{nameSpace}'{(includeSubNamespaces ? " (including sub-namespaces)" : "")}");
            return;
        }

        Logger.LogInformation($"Found {types.Count} {typeDisplayName}(s) under namespace '{nameSpace}'{(includeSubNamespaces ? " (including sub-namespaces)" : "")}");

        foreach (var type in types)
        {
            try
            {
                processType(type);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException { InnerException: { } innerException })
                {
                    ex = innerException;
                }
                Logger.LogError(ex, $"Failed to register {typeDisplayName} '{type.Name}'");
            }
        }
    }

    /// <summary>
    /// Automatically discovers and registers all PluginModuleBase-derived classes under the specified namespace.
    /// </summary>
    /// <param name="nameSpace">The namespace to search for modules</param>
    /// <param name="includeSubNamespaces">If true, includes classes from sub-namespaces. Default is false (only direct namespace).</param>
    protected void RegisterModulesUnderNamespace(string nameSpace, bool includeSubNamespaces = false)
    {
        DiscoverAndProcessTypes<PluginModuleBase>(nameSpace, includeSubNamespaces, "module", moduleType =>
        {
            var registerMethod = GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .SingleOrDefault(m => m.Name == nameof(RegisterModule) &&
                                     m.IsGenericMethodDefinition &&
                                     m.GetParameters().Length == 0)
                ?.MakeGenericMethod(moduleType);

            if (registerMethod == null)
            {
                Logger.LogError($"Failed to get RegisterModule method for type '{moduleType.Name}'");
                return;
            }

            registerMethod.Invoke(this, null);
        });
    }

    private void CallModulesAllPluginsLoaded()
    {
        foreach (IPluginModule loadedModule in _loadedModules)
        {
            loadedModule.AllPluginsLoaded();
        }
    }


    private void UnloadAllModules()
    {
        foreach (PluginModuleBase loadedModule in _loadedModules)
        {
            loadedModule.UnloadModule();
            Logger.LogInformation($"{loadedModule.PluginModuleName} has been unloaded.");
        }
        _loadedModules.Clear();
    }

    private void RegisterDebugLogger(IDebugLogger logger)
    {
        ServiceCollection.AddSingleton(logger);
    }
    
    
    private Dictionary<string, TnmsAbstractCommandBase> TnmsAbstractedClientCommands { get; } = new();
    private Dictionary<string, TnmsAbstractCommandBase> TnmsAbstractedServerCommands { get; } = new();

    /// <summary>
    /// Add TnmsAbstracted command to ModSharp
    /// </summary>
    /// <param name="command">Classes that inherited TnmsAbstractCommandBase</param>
    public void AddTnmsCommand(TnmsAbstractCommandBase command)
    {
        if (command.CommandRegistrationType == 0)
            throw new ArgumentException("Command registration type should have at least 1 flag!");

        if (command.CommandRegistrationType.HasFlag(TnmsCommandRegistrationType.Client))
        {
            if (TnmsAbstractedClientCommands.Any(c => c.Key == command.CommandName))
            {
                Logger.LogWarning("Command alias '{alias}' is already registered, skipping alias registration.", command.CommandName);
            }
            
            SharedSystem.GetClientManager().InstallCommandCallback(command.CommandName, command.Execute);
            TnmsAbstractedClientCommands.Add(command.CommandName, command);

            if (command.CommandAliases.Any())
            {
                foreach (var commandCommandAlias in command.CommandAliases)
                {
                    if (TnmsAbstractedClientCommands.Any(c => c.Key == commandCommandAlias))
                    {
                        Logger.LogWarning("Command alias '{alias}' is already registered, skipping alias registration.", commandCommandAlias);
                        continue;
                    }
                    
                    SharedSystem.GetClientManager().InstallCommandCallback(commandCommandAlias, command.Execute);
                    TnmsAbstractedClientCommands.Add(commandCommandAlias, command);
                }
            }
        }

        if (command.CommandRegistrationType.HasFlag(TnmsCommandRegistrationType.Server))
        {
            if (TnmsAbstractedServerCommands.Any(c => c.Key == command.CommandName))
            {
                Logger.LogWarning("Command for server 'ms_{cmdName}' is already registered.", command.CommandName);
            }
            
            SharedSystem.GetConVarManager().CreateConsoleCommand("ms_" + command.CommandName, command.Execute, command.CommandDescription, command.ConVarFlags);
            TnmsAbstractedServerCommands.Add(command.CommandName, command);
            
            if (command.CommandAliases.Any())
            {
                foreach (var commandCommandAlias in command.CommandAliases)
                {
                    if (TnmsAbstractedServerCommands.Any(c => c.Key == commandCommandAlias))
                    {
                        Logger.LogWarning("Command alias '{alias}' is already registered, skipping alias registration.", commandCommandAlias);
                        continue;
                    }
                    
                    SharedSystem.GetConVarManager().CreateConsoleCommand("ms_" + commandCommandAlias, command.Execute, command.CommandDescription, command.ConVarFlags);
                    TnmsAbstractedServerCommands.Add(commandCommandAlias, command);
                }
            }
        }
    }
    
    /// <summary>
    /// Add TnmsAbstracted command to ModSharp
    /// </summary>
    public void AddTnmsCommandByClass<T>() where T : TnmsAbstractCommandBase
    {
        var command = (T)Activator.CreateInstance(typeof(T), ServiceProvider)!;
        AddTnmsCommand(command);
    }

    /// <summary>
    /// Automatically discovers and registers all TnmsAbstractCommandBase-derived classes under the specified namespace.
    /// </summary>
    /// <param name="nameSpace">The namespace to search for commands</param>
    /// <param name="includeSubNamespaces">If true, includes classes from sub-namespaces. Default is false (only direct namespace).</param>
    public void AddTnmsCommandsUnderNamespace(string nameSpace, bool includeSubNamespaces = false)
    {
        DiscoverAndProcessTypes<TnmsAbstractCommandBase>(nameSpace, includeSubNamespaces, "command", commandType =>
        {
            var command = (TnmsAbstractCommandBase)ActivatorUtilities.CreateInstance(ServiceProvider, commandType, ServiceProvider);

            AddTnmsCommand(command);
        });
    }

    /// <summary>
    /// Remove TnmsAbstracted command to ModSharp
    /// </summary>
    /// <param name="command">Classes that inherited TnmsAbstractCommandBase</param>
    public void RemoveTnmsCommand(TnmsAbstractCommandBase command)
    {
        if (command.CommandRegistrationType == 0)
            throw new ArgumentException("Command registration type should have at least 1 flag!");

        if (command.CommandRegistrationType.HasFlag(TnmsCommandRegistrationType.Client))
        {
            SharedSystem.GetClientManager().RemoveCommandCallback(command.CommandName, command.Execute);

            foreach (var commandAlias in command.CommandAliases)
            {
                if (!TnmsAbstractedClientCommands.TryGetValue(commandAlias, out var alias))
                    continue;
                
                if (alias.CommandName != command.CommandName)
                    continue;
                
                SharedSystem.GetClientManager().RemoveCommandCallback(commandAlias, command.Execute);
                TnmsAbstractedClientCommands.Remove(commandAlias);
            }
        }

        if (command.CommandRegistrationType.HasFlag(TnmsCommandRegistrationType.Server))
        {
            SharedSystem.GetConVarManager().ReleaseCommand("ms_" + command.CommandName);

            foreach (var commandCommandAlias in command.CommandAliases)
            {
                if (!TnmsAbstractedServerCommands.TryGetValue(commandCommandAlias, out var alias))
                    continue;
                
                if (alias.CommandName != command.CommandName)
                    continue;
                
                SharedSystem.GetConVarManager().ReleaseCommand("ms_" + commandCommandAlias);
                TnmsAbstractedServerCommands.Remove(commandCommandAlias);
            }
        }
    }
}
