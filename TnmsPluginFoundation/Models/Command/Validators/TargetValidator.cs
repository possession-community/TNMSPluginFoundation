using System.Collections.Generic;
using System.Linq;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace TnmsPluginFoundation.Models.Command.Validators;


/// <summary>
/// Targeting validator for TnmsAbstractCommandBase <br/>
/// Find players by using Sharp.Modules.TargetingManager. This validator fails when if no players found <br/>
/// Supported target strings follow TargetingManager's resolution rules (e.g. @all, @me, @t, @ct, #name, SteamID64) <br/>
/// You can obtain validated targets by using `validatedArguments.GetArgument&lt;List&lt;IGameClient&gt;&gt;(argumentIndex)`
/// </summary>
/// <param name="argumentIndex">Index of the argument containing the target string (1-based)</param>
/// <param name="dontNotifyWhenFailed">When true, it will return TnmsCommandValidationResult.FailedIgnoreDefault to avoid print default failure message</param>
/// <param name="bypassAdminCheck">When true, it will bypass admin check</param>
public sealed class TargetValidator(int argumentIndex, bool dontNotifyWhenFailed = false, bool bypassAdminCheck = false): CommandValidatorBase, ICommandArgumentValidator
{
    private List<IGameClient>? _lastFoundTargets;
    private string? _lastTargetString;
    /// <summary>
    /// Name of this validator for identification purposes
    /// </summary>
    public override string ValidatorName => "TnmsBuiltinTargetValidator";

    /// <summary>
    /// Message of validation failure
    /// </summary>
    public override string ValidationFailureMessage => "Common.Validation.Failure.Target";

    public int ArgumentIndex => argumentIndex;

    /// <summary>
    /// Find players by using TargetingManager. This validator fails when if no players found
    /// </summary>
    /// <param name="client">CCSPlayerController</param>
    /// <param name="commandInfo">CommandInfo</param>
    /// <returns>TnmsCommandValidationResult</returns>
    public override TnmsCommandValidationResult Validate(IGameClient? client, StringCommand commandInfo)
    {
        _lastFoundTargets = null;
        _lastTargetString = null;

        // Check if the argument exists
        if (commandInfo.ArgCount < argumentIndex)
        {
            if (dontNotifyWhenFailed)
                return TnmsCommandValidationResult.FailedIgnoreDefault;

            return TnmsCommandValidationResult.Failed;
        }

        var targetString = commandInfo.GetArg(argumentIndex);
        _lastTargetString = targetString;

        var foundTargets = TnmsPlugin.TargetingManager.GetByTarget(client, targetString).ToList();

        if (client != null && !bypassAdminCheck)
        {
            foundTargets = foundTargets
                .Where(target => TnmsPlugin.AdminManager.PlayerCanTarget(client.SteamId, target.SteamId))
                .ToList();
        }

        if (foundTargets.Count == 0)
        {
            if (dontNotifyWhenFailed)
                return TnmsCommandValidationResult.FailedIgnoreDefault;

            return TnmsCommandValidationResult.Failed;
        }

        _lastFoundTargets = foundTargets;
        return TnmsCommandValidationResult.Success;
    }

    /// <summary>
    /// Gets the last found targets from validation
    /// </summary>
    /// <returns>List of found players or null if no validation was performed or failed</returns>
    public List<IGameClient>? GetFoundTargets() => _lastFoundTargets;

    /// <summary>
    /// Gets the last target string that was validated
    /// </summary>
    /// <returns>Target string or null if no validation was performed</returns>
    public string? GetLastTargetString() => _lastTargetString;

    /// <summary>
    /// Extracts validated arguments after successful validation
    /// </summary>
    /// <param name="player">CCSPlayerController</param>
    /// <param name="commandInfo">CommandInfo</param>
    /// <returns>ValidatedArguments with found targets</returns>
    protected override ValidatedArguments ExtractArguments(IGameClient? player, StringCommand commandInfo)
    {
        var validatedArguments = base.ExtractArguments(player, commandInfo);

        if (_lastFoundTargets != null)
        {
            validatedArguments.SetArgument(argumentIndex, _lastFoundTargets);
        }

        return validatedArguments;
    }
}
