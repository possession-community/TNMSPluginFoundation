using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace TnmsPluginFoundation.Models.Command.Validators;


/// <summary>
/// Permission validator for TnmsAbstractCommandBase
/// </summary>
/// <param name="requiredPermission">Permission node that required</param>
/// <param name="dontNotifyWhenFailed">When true, it will return TnmsCommandValidationResult.FailedIgnoreDefault to avoid print default failure message</param>
public sealed class PermissionValidator(string requiredPermission, bool dontNotifyWhenFailed = false) : CommandValidatorBase
{
    /// <summary>
    /// Name of this validator for identification purposes
    /// </summary>
    public override string ValidatorName => "TnmsBuiltinPermissionValidator";

    /// <summary>
    /// Message of validation failure
    /// </summary>
    public override string ValidationFailureMessage => "Common.Validation.Failure.Permission";

    /// <summary>
    /// Validates player permission
    /// </summary>
    /// <param name="client">CCSPlayerController</param>
    /// <param name="commandInfo">CommandInfo</param>
    /// <returns>TnmsCommandValidationResult</returns>
    public override TnmsCommandValidationResult Validate(IGameClient? client, StringCommand commandInfo)
    {
        // null client = server console, which bypasses permission checks
        if (client == null || TnmsPlugin.AdminManager.PlayerHasPermission(client.SteamId, requiredPermission))
            return TnmsCommandValidationResult.Success;
        
        if (dontNotifyWhenFailed)
            return TnmsCommandValidationResult.FailedIgnoreDefault;
        
        return TnmsCommandValidationResult.Failed;
    }
}