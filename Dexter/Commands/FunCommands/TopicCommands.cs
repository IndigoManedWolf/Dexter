﻿using Dexter.Attributes.Methods;
using Dexter.Databases.FunTopics;
using Discord.Commands;
using System.Runtime.InteropServices;

namespace Dexter.Commands
{

    public partial class FunCommands
    {

        /// <summary>
        /// Returns a random topic from the database if the command provided is an empty string or null object.
        /// Otherwise, provides a list of options to suggest edits to the already-existing topics in the database.
        /// </summary>
        /// <remarks>This command has a 2-minute cooldown.</remarks>
        /// <param name="Command"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("topic")]
        [Summary("A topic starter command - perfect for when chat has died!\n" +
                    "`ADD [TOPIC]` - adds a topic to the database.\n" +
                    "`GET [TOPIC]` - gets a topic by name from the database.\n" +
                    "`EDIT [TOPIC ID] [TOPIC]` - edits a topic in the database.\n" +
                    "`REMOVE [TOPIC ID]` - removes a topic from the database.")]
        [CommandCooldown(120)]

        public async Task TopicCommand([Optional][Remainder] string Command)
        {
            await RunTopic(Command, TopicType.Topic);
        }

        /// <summary>
        /// Returns a random would-you-rather from the database if the command provided is an empty string or null object.
        /// Otherwise, provides a list of options to suggest edits to the already-existing would-you-rather questions in the database.
        /// </summary>
        /// <remarks>This command has a 2-minute cooldown.</remarks>
        /// <param name="Command"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("wyr")]
        [Summary("A would-you-rather command comparing two different choices from which a discussion can be made from.\n" +
                    "`ADD [WYR]` - adds a wyr to the database.\n" +
                    "`GET [WYR]` - gets a wyr by name from the database.\n" +
                    "`EDIT [WYR ID] [WYR]` - edits a wyr in the database.\n" +
                    "`REMOVE [WYR ID]` - removes a wyr from the database.")]
        [Alias("would you rather", "wouldyourather")]
        [CommandCooldown(120)]

        public async Task WYRCommand([Optional][Remainder] string Command)
        {
            await RunTopic(Command, TopicType.WouldYouRather);
        }

        /// <summary>
        /// Returns a random fun fact from the database if the command provided is an empty string or null object.
        /// Otherwise, provides a list of options to suggest edits to the already-existing fun facts in the database.
        /// </summary>
        /// <remarks>This command has a 2-minute cooldown.</remarks>
        /// <param name="Command"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("funfact")]
        [Summary("A fun fact command that displays and manages curious, interesting, or unexpected facts about reality or the world.\n" +
                    "`ADD [FACT]` - adds a fun fact to the database.\n" +
                    "`GET [FACT]` - gets a fun fact by name from the database.\n" +
                    "`EDIT [FACT ID] [NEW FACT]` - edits a fun fact in the database.\n" +
                    "`REMOVE [FACT ID]` - removes a fun fact from the database.")]
        [Alias("fact", "sciencefact")]
        [CommandCooldown(120)]

        public async Task FunFactCommand([Optional][Remainder] string Command)
        {
            await RunTopic(Command, TopicType.FunFact);
        }

        /// <summary>
        /// Returns a random joke from the database if the command provided is an empty string or null object.
        /// Otherwise, provides a list of options to suggest edits to the already-existing jokes in the database.
        /// </summary>
        /// <remarks>This command has a 2-minute cooldown.</remarks>
        /// <param name="Command"></param>
        /// <returns>A <c>Task</c> object, which can be awaited until this method completes successfully.</returns>

        [Command("joke")]
        [Summary("A joke command that displays a funny statement, generally with a punchline; aimed for comedic intent.\n" +
                    "`ADD [JOKE]` - adds a joke to the database.\n" +
                    "`GET [JOKE]` - gets a joke by name from the database.\n" +
                    "`EDIT [JOKE ID] [NEW JOKE]` - edits a joke in the database.\n" +
                    "`REMOVE [JOKE ID]` - removes a joke from the database.")]
        [Alias("pun")]
        [CommandCooldown(120)]

        public async Task JokeCommand([Optional][Remainder] string Command)
        {
            await RunTopic(Command, TopicType.Joke);
        }

    }

}
