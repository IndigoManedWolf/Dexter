﻿using Dexter.Abstractions;
using Dexter.Configurations;
using Dexter.Databases.CustomCommands;
using Dexter.Enums;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;

namespace Dexter.Services
{

    /// <summary>
    /// The CommandHandlerService deals with invoking the command and the errors that may occur as a result.
    /// It filters the command to see if the user is not a bot and that it has the prefix set in the
    /// bot configuration. It also catches all errors that may crop up in a command, logs it, and then sends
    /// an appropriate error to the channel, pinging the developers if the error is unknown.
    /// </summary>

    public class CommandHandlerService : Service
    {

        /// <summary>
        /// The ServiceProvider is where our dependencies are stored - given as a field to DiscordNet's execution method.
        /// </summary>

        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// The CommandService of the bot is used to check if there are any errors on the CommandExecuted event.
        /// </summary>

        public CommandService CommandService { get; set; }

        /// <summary>
        /// The CustomCommandDB is used to get our custom commands, which - if we fail as the command is unknown - we parse to find a match.
        /// </summary>

        public CustomCommandDB CustomCommandDB { get; set; }

        /// <summary>
        /// The LoggingService is used to log unexpected errors that may occur on command execution.
        /// </summary>

        public LoggingService LoggingService { get; set; }

        /// <summary>
        /// The ProposalConfiguration is used to operate the suggestion service and confugure voting thresholds.
        /// </summary>

        public ProposalConfiguration ProposalConfiguration { get; set; }

        /// <summary>
        /// The CustomCommandsConfiguration is used to ascertain certain details to verify user command interactions.
        /// </summary>

        public CustomCommandsConfiguration CustomCommandsConfiguration { get; set; }

        /// <summary>
        /// The Initialize override hooks into both the Client's MessageReceived event and the CommandService's CommandExecuted event.
        /// </summary>

        public override void Initialize()
        {
            DiscordSocketClient.MessageReceived += HandleCommandAsync;
            CommandService.CommandExecuted += SendCommandError;
        }

        /// <summary>
        /// The HandleCommandAsync runs on MessageReceived and will check for if the message has the bot's prefix,
        /// if the author is a bot and if we're in a guild, if so - execute!
        /// </summary>
        /// <param name="socketMessage">The SocketMessage event is given as a parameter of MessageReceived and
        /// is used to find and execute the command if the parameters have been met.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            // We do not check the message if it is not an instance of a user message.
            if (socketMessage is not SocketUserMessage message)
                return;

            int argumentPosition = 0;

            // We do not parse the message if it does not have the prefix or it is from a bot.
            if (!message.HasStringPrefix(BotConfiguration.Prefix, ref argumentPosition) || message.Author.IsBot || BotConfiguration.DisallowedChannels.Contains(message.Channel.Id))
                return;

            // Finally, if all prerequesites have returned correctly, we run and parse the command with an instance of our socket command context and our services.
            await CommandService.ExecuteAsync(new SocketCommandContext(DiscordSocketClient, message), argumentPosition, ServiceProvider);
        }

        /// <summary>
        /// The SendCommandError runs on CommandExecuted and checks if the command run has encountered an error. It also handles custom commands through the result of an unknown command.
        /// </summary>
        /// <param name="commandInfo">This gives information about the command that may have been run, such as its name.</param>
        /// <param name="commandContext">The context command provides is with information about the message, including who sent it and the channel it was set in.</param>
        /// <param name="result">The result specifies the outcome of the attempted run of the command - whether it was successful or not and the error it may have run in to.</param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        public async Task SendCommandError(Optional<CommandInfo> commandInfo, ICommandContext commandContext, IResult result)
        {
            if (result.IsSuccess)
                return;

            try
            {
                EmbedBuilder message = null;

                switch (result.Error)
                {

                    // Unmet Precondition specifies that the error is a result as one of the preconditions specified by an attribute has returned FromError.
                    case CommandError.UnmetPrecondition:
                        if (result.ErrorReason.Length <= 0)
                            return;

                        message = BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Halt! Don't go there-")
                            .WithDescription(result.ErrorReason);

                        break;

                    // Bad Argument Count specifies that the command has had an invalid amount of arguments parsed to it. It will send all the commands with their parameters and summaries in response.
                    case CommandError.BadArgCount:
                        message = BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("You've entered an invalid amount of parameters for this command!")
                            .WithDescription($"Here are some options of parameters you can have for the command **{commandInfo.Value.Name}**.")
                            .GetParametersForCommand(commandInfo.Value, BotConfiguration);

                        break;

                    // Unknown Command specifies that the parser was unable to find a command with the name specified. If this throws, we look for custom commands that may have the name and then send that it is an unknown command if there are not any returned.
                    case CommandError.UnknownCommand:
                        string[] customCommandArgs = commandContext.Message.Content[BotConfiguration.Prefix.Length..].Split(' ');

                        CustomCommand customCommand = CustomCommandDB.GetCommandByNameOrAlias(customCommandArgs[0].ToLower());

                        if (customCommand != null)
                        {
                            if (customCommand.Reply.Length > 0 && Commands.CustomCommands.IsCustomCommandActive(customCommand, DiscordSocketClient, BotConfiguration, CustomCommandsConfiguration))
                            {
                                string reply = customCommand.Reply;

                                ulong firstMentionedID = commandContext.Message.MentionedUserIds.FirstOrDefault();

                                reply = reply.Replace("AUTHOR", (firstMentionedID != default && firstMentionedID != commandContext.User.Id) || !reply.Contains("USER")
                                    ? commandContext.User.Mention : commandContext.Client.CurrentUser.Mention);

                                List<string> userMentions = new();
                                foreach(ulong id in commandContext.Message.MentionedUserIds)
                                {
                                    IUser user = DiscordSocketClient.GetUser(id);
                                    if (user is not null)
                                        userMentions.Add(user.Mention);
                                }
                                string userReplacement = !userMentions.Any() ? commandContext.User.Mention : LanguageHelper.Enumerate(userMentions);
                                reply = reply.Replace("USER", userReplacement);

                                await commandContext.Channel.SendMessageAsync(reply);
                            }
                            else
                                message = BuildEmbed(EmojiEnum.Annoyed)
                                    .WithTitle("Misconfigured command!")
                                    .WithDescription($"`{customCommand.CommandName}` has not been configured! Please contact a moderator about this. <3");
                        }
                        else
                        {
                            if (commandContext.Message.Content.Length <= 1)
                                return;
                            else if (commandContext.Message.Content.Count(Character => Character == '~') > 1 ||
                                    ProposalConfiguration.CommandRemovals.Contains(commandContext.Message.Content.Split(' ')[0]))
                                return;
                            else
                            {
                                message = BuildEmbed(EmojiEnum.Annoyed)
                                        .WithTitle("Unknown Command.")
                                        .WithDescription($"Oopsies! It seems as if the command **{customCommandArgs[0].SanitizeMarkdown()}** doesn't exist!");
                            }
                        }

                        break;

                    // Parse Failed specifies that the TypeReader has been unable to parse a specific parameter of the command.
                    case CommandError.ParseFailed:
                        message = BuildEmbed(EmojiEnum.Annoyed)
                            .WithTitle("Unable to parse command!")
                            .WithDescription("Invalid amount of command arguments.");

                        break;

                    // The default case specifies that this command has run into an unknown error that will need to be reported.
                    default:

                        // If we have been thrown an ObjectNotFound error, this means that the argument has been unable to be found. This could be due to caching, thus we do not need to ping the developers of this error.
                        if (result.ToString().Contains("ObjectNotFound"))
                        {
                            message = BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle(result.ErrorReason)
                                .WithDescription($"If you believe this was an error, please do ping a developer!\nIf the {result.ErrorReason.Split(' ')[0].ToLower()} does exist, it may be due to caching. If so, please wait a few minutes.");

                            return;
                        }

                        // If the error is not an ObjectNotFound error, we log the message to the console with the appropriate data.
                        await LoggingService.LogMessageAsync(new LogMessage(LogSeverity.Warning, GetType().Name.Prettify(), $"Unknown statement reached!\nCommand: {(commandInfo.IsSpecified ? commandInfo.Value.Name : null)}\nresult: {result}"));

                        EmbedBuilder commandErrorEmbed;

                        // Once logged, we check to see if the error is an Executeresult error as these execution results have more data about the issue that has gone wrong.
                        if (result is ExecuteResult executeResult)
                            commandErrorEmbed = BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle(executeResult.Exception.GetType().Name.Prettify())
                                .WithDescription(executeResult.Exception.Message);
                        else
                            commandErrorEmbed = BuildEmbed(EmojiEnum.Annoyed)
                                .WithTitle(result.Error.GetType().Name.Prettify())
                                .WithDescription(result.ErrorReason);

                        // Finally, we send the error into the channel with a ping to the developers to take notice of.
                        await commandContext.Channel.SendMessageAsync($"Unknown error!{(BotConfiguration.PingDevelopers ? $" I'll tell the developers.\n<@&{BotConfiguration.DeveloperRoleID}>" : string.Empty)}", embed: commandErrorEmbed.Build());
                        break;
                }

                _ = Task.Run(async () =>
                {
                    if (message == null)
                        return;

                    IMessage sent = await commandContext.Channel.SendMessageAsync(
                        embed: message.Build()
                    );

                    await Task.Delay(5000);

                    await sent.DeleteAsync();
                });
            }
            catch (HttpException)
            {
                await commandContext.Channel.SendMessageAsync($"Haiya <@&{BotConfiguration.DeveloperRoleID}>, it seems as though the bot does not have the correct permissions to send embeds into this channel!\n" +
                    $"Command errored out on the {result.Error.Value} error.");
            }

        }

    }

}
