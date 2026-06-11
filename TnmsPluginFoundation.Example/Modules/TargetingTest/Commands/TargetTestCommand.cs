using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Extensions.Client;
using TnmsPluginFoundation.Models.Command;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Utils.Entity;

namespace TnmsPluginFoundation.Example.Modules.TargetingTest.Commands;

public class TargetTestCommand(IServiceProvider provider) : TnmsAbstractCommandBase(provider)
{
    // ms_ is automatically declared so we don't need to specify here
    public override string CommandName => "tg";
    public override string CommandDescription => ".";
    public override TnmsCommandRegistrationType CommandRegistrationType => TnmsCommandRegistrationType.Client;

    protected override ICommandValidator GetValidator() => new CompositeValidator()
        .Add(new ArgumentCountValidator(1, true))
        .Add(new TargetValidator(1, true));

    protected override ValidationFailureResult OnValidationFailed(ValidationFailureContext context)
    {
        Logger.LogInformation("Validation failed by {validator}", context.Validator.ValidatorName);
        return ValidationFailureResult.SilentAbort();
    }

    protected override void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments)
    {
        if (validatedArguments is null || client is null)
            return;

        var clients = validatedArguments.GetArgument<List<IGameClient>>(1)!;
        
        foreach (var gameClient in clients)
        {
            GameRulesUtil.SetRoundTime(1500);
            client.GetPlayerController()!.PrintToChat($"Found target: {gameClient.Name}");
        }
        
        
    }
}