using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Extensions.Client;

namespace TnmsPluginFoundation.Utils.Entity;

/// <summary>
/// Utility class for manipulate player entity
/// </summary>
public static class PlayerUtil
{
    private static IGameEvent? _nextLevelChangedEvent;

    private static IGameEvent? GetNextLevelChangedEvent()
    {
        return _nextLevelChangedEvent ??= TnmsPlugin.StaticSharedSystem.GetEventManager().CreateEvent("nextlevel_changed", true);
    }
    
    /// <summary>
    /// Check player is alive
    /// </summary>
    /// <param name="client">Client instance</param>
    /// <returns>Returns true if player alive. Otherwise false</returns>
    public static bool IsPlayerAlive(IGameClient? client)
    {
        if (client == null)
            return false;
        
        var playerPawn = client.GetPlayerPawn();
        
        if (playerPawn == null)
            return false;
        
        return playerPawn.LifeState == LifeState.Alive;
    }
    
    /// <summary>
    /// Get player model path
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <returns>Returns model name if found. Otherwise, empty string</returns>
    public static string GetPlayerModel(IGameClient client)
    {
        var playerPawn = client.GetPlayerPawn();
        
        if (playerPawn == null)
            return string.Empty;

        var sceneNode = playerPawn.GetBodyComponent().GetSceneNode();
        
        if (sceneNode == null)
            return string.Empty;

        if (sceneNode.AsSkeletonInstance == null)
            return string.Empty;
        
        return sceneNode.AsSkeletonInstance.GetModelState().ModelName;
    }

    /// <summary>
    /// Set player model
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="modelPath">Path to model end with .vmdl</param>
    /// <returns>Return true if successfully to set. Otherwise false</returns>
    public static bool SetPlayerModel(IGameClient client, string modelPath)
    {
        var playerPawn = client.GetPlayerPawn();
        
        if (playerPawn == null)
            return false;

        playerPawn.SetModel(modelPath);
        return true;
    }
    
    private static readonly string ServerConsoleName = $" {ChatColor.DarkRed}CONSOLE{ChatColor.White}";
    
    /// <summary>
    /// Returns a name of player, If param is null, then returns CONSOLE
    /// This method is useful for replying command or broadcasting executor name.
    /// </summary>
    /// <param name="client">Target CCSPlayerController</param>
    /// <returns></returns>
    public static string GetPlayerName(IGameClient? client)
    {
        if (client == null)
            return ServerConsoleName;
        
        var playerController = client.GetPlayerController();
        if (playerController == null)
            return ServerConsoleName;
        
        return playerController.PlayerName;
    }

    /// <summary>
    /// Set player's name
    /// </summary>
    /// <param name="client">Target CCSPlayerController</param>
    /// <param name="playerName">Name of player</param>
    public static void SetPlayerName(IGameClient client, string playerName)
    {
        var playerController = client.GetPlayerController();

        if (playerController == null)
            return;

        // IBaseEntity.SetName sets the entity targetname; the shown name is the controller's PlayerName and the client's name.
        playerController.PlayerName = playerName;
        client.SetName(playerName);
    }

    
    /// <summary>
    /// Set player's clan tag
    /// </summary>
    /// <param name="client">Target CCSPlayerController</param>
    /// <param name="playerClanTag">Tag name string</param>
    public static void SetPlayerClanTag(IGameClient client, string playerClanTag)
    {
        var playerController = client.GetPlayerController();
        
        if (playerController == null)
            return;
        
        playerController.SetClanTag(playerClanTag);
        
        // TODO() Is it required to implement Event to apply change immediately?
        // var fakeEvent = new EventNextlevelChanged(false);
        // fakeEvent.FireEvent(false);
    }

    /// <summary>
    /// Set player's team
    /// </summary>
    /// <param name="client">Target CCSPlayerController</param>
    /// <param name="playerTeam"></param>
    public static void SetPlayerTeam(IGameClient client, CStrikeTeam playerTeam)
    {
        var playerController = client.GetPlayerController();
        
        if (playerController == null)
            return;
        
        TnmsPlugin.StaticSharedSystem.GetModSharp().InvokeAction(() =>
        {
            playerController.SwitchTeam(playerTeam);
        });
    }

    /// <summary>
    /// Set player's health to specified value
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="health">The value of health</param>
    /// <returns>Return true if successfully to set. Otherwise false</returns>
    public static bool SetPlayerHealth(IGameClient client, int health)
    {
        var playerPawn = client.GetPlayerPawn();
        
        if (playerPawn == null)
            return false;
        
        playerPawn.Health = health;
        playerPawn.NetworkStateChanged("m_iHealth");
        return true;
    }

    /// <summary>
    /// Set player's max health to specified value
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="maxHealth">Value of max health</param>
    /// <returns>Return true if successfully to set. Otherwise false</returns>
    public static bool SetPlayerMaxHealth(IGameClient client, int maxHealth)
    {
        var playerPawn = client.GetPlayerPawn();
        
        if (playerPawn == null)
            return false;
        
        playerPawn.MaxHealth = maxHealth;
        playerPawn.NetworkStateChanged("m_iMaxHealth");
        return true;
    }

    /// <summary>
    /// Set player's armor to specified value
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="amount">Value of kevlar armor</param>
    /// <param name="hasHelmet">Boolean to specify player should have a helmet</param>
    /// <returns>Return true if successfully to set. Otherwise false</returns>
    public static bool SetPlayerArmor(IGameClient client, int amount, bool hasHelmet = false)
    {
        var playerPawn = client.GetPlayerPawn();
        
        if (playerPawn == null)
            return false;

        playerPawn.ArmorValue = amount;
        playerPawn.NetworkStateChanged("m_ArmorValue");
        
        if (!hasHelmet)
            return true;

        var itemService = playerPawn.GetItemService();
        if (itemService == null)
            return false;
        
        itemService.HasHelmet = hasHelmet;
        playerPawn.NetworkStateChanged("m_pItemServices");
        return true;
    }

    /// <summary>
    /// Set player's money to specified value
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="money">Value of player money</param>
    /// <returns>Return true if successfully to set. Otherwise false</returns>
    public static bool SetPlayerMoney(IGameClient client, int money)
    {
        var playerController = client.GetPlayerController();
        if (playerController == null)
            return false;
        
        var inGameMoneyServices = playerController.GetInGameMoneyService();
        if (inGameMoneyServices == null)
            return false;

        inGameMoneyServices.Account = money;
        playerController.NetworkStateChanged("m_pInGameMoneyServices");
        return true;
    }

    /// <summary>
    /// Set player's BuyZone status to specified value
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="inBuyZone">Value of player BuyZone status</param>
    /// <returns>Return true if successfully to set. Otherwise false</returns>
    public static bool SetPlayerBuyZoneStatus(IGameClient client, bool inBuyZone)
    {
        var playerPawn = client.GetPlayerPawn();
        if (playerPawn == null)
            return false;
        
        playerPawn.SetNetVar("m_bInBuyZone", inBuyZone);
        playerPawn.NetworkStateChanged("m_bInBuyZone");
        return true;
    }


    /// <summary>
    /// Show progress bar hud to player <br/>
    /// You should call <see cref="RemoveProgressBarHud"/> to remove progress bar hud, Otherwise hud will remain on player's screen.
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    /// <param name="durationSeconds">Countdown count specify with seconds</param>
    /// <param name="action">At this time, we can only use k_CSPlayerBlockingUseAction_None, otherwise hud will not appear to client</param>
    public static void ShowProgressBarHud(IGameClient client, int durationSeconds/*, CSPlayerBlockingUseAction_t action = CSPlayerBlockingUseAction_t.k_CSPlayerBlockingUseAction_None*/)
    {
        var playerPawn = client.GetPlayerPawn();
        if (playerPawn == null)
            return;

        float currentTime = TnmsPlugin.StaticSharedSystem.GetModSharp().GetGlobals().CurTime;


        // TODO() Maybe need this?
        // playerPawn.SimulationTime = currentTime + durationSeconds;
        
        playerPawn.ProgressBarDuration = durationSeconds;
        playerPawn.ProgressBarStartTime = currentTime;
        // TODO() is there a similar things in ModSharp?
        // pawn.BlockingUseActionInProgress = action;
    }

    /// <summary>
    /// Remove progress bar hud from player. if player is not showing any progress bar hud, this method will do nothing.
    /// </summary>
    /// <param name="client">Target CCSPlayerController instance. This parameter shouldn't be null</param>
    public static void RemoveProgressBarHud(IGameClient client)
    {
        var playerPawn = client.GetPlayerPawn();
        if (playerPawn == null)
            return;
        
        playerPawn.ProgressBarDuration = 0;
        playerPawn.ProgressBarStartTime = 0.0f;
    }
}