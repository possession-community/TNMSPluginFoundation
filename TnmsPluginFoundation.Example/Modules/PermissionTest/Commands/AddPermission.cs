using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using Wuling.Abstract.Tianshi.Authority;

namespace TnmsPluginFoundation.Example.Modules.PermissionTest.Commands;

public class AddPermission(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    // ms_ is automatically declared so we don't need to specify here
    public override string CommandName => "ap";
    public override string CommandDescription => ".";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    protected override ICommandValidator? GetValidator() => new ArgumentCountValidator(1, true);

    protected override ValidationFailureResult OnValidationFailed(ValidationFailureContext context)
    {
        Logger.LogInformation("Validation failed by {validator}", context.Validator.ValidatorName);
        return ValidationFailureResult.SilentAbort();
    }

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client == null)
            return;
        
        var permission = commandInfo.GetArg(1);

        if (TnmsPlugin.AdminManager.AddPermissionToPlayer(client.SteamId, permission) == AuthoritySaveResult.Success)
        {
            client.GetPlayerController()!.PrintToChat($"Added permission '{permission}' to client.");
        }
        else
        {
            client.GetPlayerController()!.PrintToChat($"Failed to add permission '{permission}' to client.");
        }
    }
}