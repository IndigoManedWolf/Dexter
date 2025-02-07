﻿using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord;
using Discord.Commands;

namespace Dexter.Commands
{
    public partial class LevelingCommands
    {

        /// <summary>
        /// Updates the roles for the user who uses this command based on Dex XP.
        /// </summary>
        /// <returns></returns>

        [Command("updateroles")]
        [Summary("Updates your ranked roles to fit the Dexter System XP")]
        [BotChannel]

        public async Task UpdateRolesCommand()
        {

            if (!LevelingConfiguration.HandleRoles)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Dexter Role Handling is disabled.")
                    .WithDescription("Dexter is not currently responsible for handling roles! This command is disabled.")
                    .SendEmbed(Context.Channel);
                return;
            }
            IGuildUser user = DiscordSocketClient.GetGuild(BotConfiguration.GuildID).GetUser(Context.User.Id);

            if (user is null)
            {
                await BuildEmbed(EmojiEnum.Annoyed)
                    .WithTitle("Unable to find user in the server!")
                    .WithDescription("This may be due to caching, try again later.")
                    .SendEmbed(Context.Channel);
                return;
            }

            if (!await LevelingService.UpdateRoles(user, true))
            {
                await Context.Channel.SendMessageAsync("Your roles are already up to date!");
            }
            else
            {
                await BuildEmbed(EmojiEnum.Love)
                    .WithTitle("Successfully Updated Roles!")
                    .WithDescription("Your roles have been updated to those adequate for your current Dexter level!")
                    .SendEmbed(Context.Channel);
            }

        }
    }
}
