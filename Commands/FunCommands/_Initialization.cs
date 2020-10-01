﻿using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Commands.FunCommands {
    public partial class FunCommands : ModuleBase<CommandModule> {

        private readonly FunConfiguration FunConfiguration;
        private readonly DiscordSocketClient Client;

        public FunCommands(DiscordSocketClient _Client, FunConfiguration _FunConfiguration) {
            FunConfiguration = _FunConfiguration;
            Client = _Client;
        }

    }
}
