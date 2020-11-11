﻿using Dexter.Configurations;
using Dexter.Abstractions;
using Dexter.Extensions;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Dexter.Services {

    /// <summary>
    /// The MeetNGreet service is used to log messages that have been updated and deleted from the MeetNGreet channel.
    /// It does this through creating a webhook and sending to that very webhook each time an event runs.
    /// </summary>
    public class MeetNGreetService : InitializableModule {

        private readonly DiscordSocketClient Client;

        private readonly MNGConfiguration MNGConfig;

        private readonly DiscordWebhookClient Webhook;

        /// <summary>
        /// The constructor for the MeetNGreetService module. This takes in the injected dependencies and sets them as per what the class requires.
        /// The constructor also creates the MeetNGreet webhook if it doesn't exist already in the MeetNGreet logs channel.
        /// </summary>
        /// <param name="Client">The instance of the client, which is used to hook into the API.</param>
        /// <param name="MNGConfig">The instance of the MNGConfiguration, which is used to find and create the MNG webhook.</param>
        public MeetNGreetService(DiscordSocketClient Client, MNGConfiguration MNGConfig) {
            this.Client = Client;
            this.MNGConfig = MNGConfig;
            
            if(!string.IsNullOrEmpty(MNGConfig.MeetNGreetWebhookURL))
                Webhook = new DiscordWebhookClient(MNGConfig.MeetNGreetWebhookURL);
        }

        /// <summary>
        /// The AddDelegates method adds the MessageDeleted and MessageUpdated hooks into their respective MNG check methods.
        /// </summary>
        public override void AddDelegates() {
            Client.MessageDeleted += MNGMessageDeleted;
            Client.MessageUpdated += MNGMessageUpdated;
        }

        /// <summary>
        /// The MNGMessageUpdated method check if a message is edited in the MNG channel and, if so,
        /// it uses the MNG webhook to send details pertaining to the previous message, the now updated message,
        /// the author of the message, the ID of the message, and a link to quickly scroll to the message in question.
        /// </summary>
        /// <param name="OldMessage">An object of the previous message that had been edited.</param>
        /// <param name="NewMessage">The instance of the new, changed message.</param>
        /// <param name="Channel">The channel from which the message had been sent from.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task MNGMessageUpdated(Cacheable<IMessage, ulong> OldMessage, SocketMessage NewMessage, ISocketMessageChannel Channel) {
            if (Channel.Id != MNGConfig.MeetNGreetChannel)
                return;

            IMessage CachedMessage = await OldMessage.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (CachedMessage.Author.IsBot)
                return;

            if (Webhook != null)
                await new EmbedBuilder()
                    .WithAuthor(CachedMessage.Author)
                    .WithDescription($"**Message edited in <#{Channel.Id}>** [Jump to message](https://discordapp.com/channels/{ (NewMessage.Channel as SocketGuildChannel).Guild.Id }/{ NewMessage.Channel.Id }/{ NewMessage.Id })")
                    .AddField("Before", CachedMessage.Content.Length > 1000 ? CachedMessage.Content.Substring(0, 1000) + "..." : CachedMessage.Content)
                    .AddField("After", NewMessage.Content.Length > 1000 ? NewMessage.Content.Substring(0, 1000) + "..." : NewMessage.Content)
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(Webhook);
        }

        /// <summary>
        /// The MNGMessageDeleted method checks if a message is deleted in the MNG channel and, if so,
        /// send a message through the webhook containing the message sent, author, ID and content.
        /// </summary>
        /// <param name="Message">The message that has been cached from the sent channel.</param>
        /// <param name="Channel">The channel from which the message had been sent from.</param>
        /// <returns>A task object, from which we can await until this method completes successfully.</returns>
        public async Task MNGMessageDeleted(Cacheable<IMessage, ulong> Message, IChannel Channel) {
            if (Channel.Id != MNGConfig.MeetNGreetChannel)
                return;

            IMessage CachedMessage = await Message.GetOrDownloadAsync();

            if (CachedMessage == null)
                return;

            if (CachedMessage.Author.IsBot)
                return;

            if (Webhook != null)
                await new EmbedBuilder()
                    .WithAuthor(CachedMessage.Author)
                    .WithDescription($"**Message sent by <@{CachedMessage.Author.Id}> deleted in in <#{Channel.Id}>**\n{(CachedMessage.Content.Length > 1900 ? CachedMessage.Content.Substring(0, 1900) + "...": CachedMessage.Content)}")
                    .WithFooter($"Author: {CachedMessage.Author.Id} | Message ID: {CachedMessage.Id}")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Blue)
                    .SendEmbed(Webhook);
        }

    }

}
