using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using Wuling.Abstract.Tianshi.Authority;

namespace TnmsPluginFoundation.Example.Modules.PermissionTest.Commands;

public class RemovePermission(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    // ms_ is automatically declared so we don't need to specify here
    public override string CommandName => "rp";
    public override string CommandDescription => ".";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    protected override ICommandValidator? GetValidator() => new ArgumentCountValidator(1, true);

    protected override ValidationFailureResult OnValidationFailed(ValidationFailureContext context)
    {
        Logger.LogInformation("Validation failed");
        return base.OnValidationFailed(context);
    }

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (client == null)
            return;
        var permission = commandInfo.GetArg(1);

        if (TnmsPlugin.AdminManager.RemovePermissionFromPlayer(client.SteamId, permission) == AuthoritySaveResult.Success)
        {
            client.GetPlayerController()!.PrintToChat($"Removed permission '{permission}' from client.");
        }
        else
        {
            client.GetPlayerController()!.PrintToChat($"Failed to remove permission '{permission}' from client.");
        }
    }
}