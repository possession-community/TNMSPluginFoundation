using System;
using System.Collections.Generic;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using TnmsPluginFoundation.Models.Plugin;
using TnmsPluginFoundation.Models.Command.Validators;
using TnmsPluginFoundation.Models.Command.Validators.RangedValidators;

namespace TnmsPluginFoundation.Models.Command;

/// <summary>
/// Abstracted Command Base
/// </summary>
public abstract class TnmsAbstractCommandBase(IServiceProvider provider): PluginTranslatableFeatureBase(provider)
{
    /// <summary>
    /// Name of command (e.g. test)  'ms_' prefix is automatically declared so we don't need to specify here
    /// </summary>
    public abstract string CommandName { get; }
    
    /// <summary>
    /// Aliases of command. Same as CommandName, 'ms_' prefix is automatically declared
    /// </summary>
    public virtual List<string> CommandAliases { get; } = [];
    
    /// <summary>
    /// Description of command
    /// </summary>
    public abstract string CommandDescription { get; }
    
    /// <summary>
    /// Flag of this command
    /// </summary>
    public virtual ConVarFlags ConVarFlags { get; } = ConVarFlags.None;

    public abstract TnmsCommandRegistrationType CommandRegistrationType { get; }
    
    /// <summary>
    /// Called once after this command is registered to ModSharp (name and aliases).
    /// Not called if every name was already taken.
    /// </summary>
    protected internal virtual void OnRegistered()
    {
    }

    /// <summary>
    /// Called once after this command is removed from ModSharp, including on plugin unload.
    /// </summary>
    protected internal virtual void OnUnregistered()
    {
    }

    /// <summary>
    /// Default validation failure message
    /// </summary>
    protected virtual string CommonValidationFailureMessage => "Common.Validation.Failure";

    /// <summary>
    /// Internal function handled by TnmsPluginFoundation
    /// </summary>
    /// <param name="player">CCSPlayerController</param>
    /// <param name="commandInfo">CommandInfo</param>
    public ECommandAction Execute(IGameClient? player, StringCommand commandInfo)
    {
        var validator = GetValidator();
        ValidatedArguments? validatedArguments = null;
        
        if (validator != null)
        {
            var validationContext = validator.ValidateWithArguments(player, commandInfo);
            
            if (validationContext.Result != TnmsCommandValidationResult.Success)
            {
                var actualFailedValidator = validator;
                if (validator is CompositeValidator composite)
                {
                    actualFailedValidator = composite.GetLastFailedValidator() ?? validator;
                }
                
                var context = new ValidationFailureContext(actualFailedValidator, player, commandInfo, validationContext.Result);
                var failureResult = OnValidationFailed(context);

                switch (failureResult.Action)
                {
                    case ValidationFailureAction.UseDefaultFallback:
                        if (validationContext.Result == TnmsCommandValidationResult.Failed)
                        {
                            var message = GetDefaultValidationMessage(context);
                            PrintMessageToServerOrPlayerChat(player, message);
                        }
                        return ECommandAction.Skipped;

                    case ValidationFailureAction.SilentAbort:
                    default:
                        return ECommandAction.Skipped;
                }
            }
            
            validatedArguments = validationContext.ValidatedArguments;
        }

        ExecuteCommand(player, commandInfo, validatedArguments);
        return ECommandAction.Handled;
    }

    /// <summary>
    /// Gets the validator for this command
    /// </summary>
    /// <returns>ICommandValidator or null</returns>
    protected virtual ICommandValidator? GetValidator() => null;

    /// <summary>
    /// Called when validation fails - override to customize failure handling
    /// </summary>
    /// <param name="context">Validation failure context</param>
    /// <returns>ValidationFailureResult specifying how to handle the failure</returns>
    protected virtual ValidationFailureResult OnValidationFailed(ValidationFailureContext context)
    {
        return ValidationFailureResult.UseDefaultFallback();
    }

    /// <summary>
    /// Gets the default validation message
    /// </summary>
    /// <param name="context">Validation failure context</param>
    /// <returns>Default message</returns>
    protected virtual string GetDefaultValidationMessage(ValidationFailureContext context)
    {
        if (context.RangedValidator != null && context.RangedResult.HasValue)
            return GetRangedValidationMessage(context.RangedValidator, context.RangedResult.Value);
        
        if (!string.IsNullOrEmpty(context.Validator.ValidationFailureMessage))
            return context.Validator.ValidationFailureMessage;
        
        return CommonValidationFailureMessage;
    }

    /// <summary>
    /// Gets validation message for ranged validators
    /// </summary>
    /// <param name="rangedValidator">The ranged validator</param>
    /// <param name="result">The ranged validation result</param>
    /// <returns>Validation message</returns>
    protected virtual string GetRangedValidationMessage(IRangedArgumentValidator rangedValidator, TnmsRangedCommandValidationResult result)
    {
        var range = rangedValidator.GetRangeDescription();
        
        return result switch
        {
            TnmsRangedCommandValidationResult.FailedLowerThanExpected => $"Value is too low. Valid range: {range}",
            TnmsRangedCommandValidationResult.FailedHigherThanExpected => $"Value is too high. Valid range: {range}",
            TnmsRangedCommandValidationResult.FailedOutOfRange => $"Invalid value or out of range. Valid range: {range}",
            _ => "Range validation failed."
        };
    }

    /// <summary>
    /// Command body - implement in derived classes
    /// </summary>
    /// <param name="client">Player who executed the command</param>
    /// <param name="commandInfo">Command information</param>
    /// <param name="validatedArguments">Validated arguments (null if no validator was used or validation didn't populate arguments)</param>
    protected abstract void ExecuteCommand(IGameClient? client, StringCommand commandInfo, ValidatedArguments? validatedArguments);
}