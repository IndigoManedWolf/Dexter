﻿using Dexter.Abstractions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Dexter.Databases.CustomCommands
{

    /// <summary>
    /// The CustomCommandDB contains a set of custom commands that the bot will reply with once a command has been run.
    /// </summary>

    public class CustomCommandDB : Database
    {

        /// <summary>
        /// A table of the custom commands in the CustomCommandDB database.
        /// </summary>

        public DbSet<CustomCommand> CustomCommands { get; set; }

        /// <summary>
        /// The Get Command By Name Or Alias method gets the CustomCommand object from the CustomCommands table
        /// that matches an alias or the command name itself through a query of the database.
        /// </summary>
        /// <param name="name">The name of the command or alias you wish to query for.</param>
        /// <returns>A CustomCommand object of the command you queried.</returns>

        public CustomCommand GetCommandByNameOrAlias(string name)
        {
            CustomCommand cmdByName = CustomCommands.Find(name);
            if (cmdByName is not null)
                return cmdByName;

            CustomCommand[] cmdByAlias = CustomCommands.AsQueryable().Where(customcmd => customcmd.Alias.Contains(name)).ToArray();

            foreach (CustomCommand command in cmdByAlias)
                if (JsonConvert.DeserializeObject<List<string>>(command.Alias).Contains(name))
                    return command;

            return null;
        }

        /// <summary>
        /// Obtains a user command by the user attached to it.
        /// </summary>
        /// <param name="id">The id of the target user.</param>
        /// <param name="type">The type of command to search for, any if unspecified.</param>
        /// <returns><see langword="null"/> if no command exists attached to that user; otherwise returns the respective <see cref="CustomCommand"/>.</returns>
        public CustomCommand GetCommandByUser(ulong id, UserCommandSource type = UserCommandSource.Unspecified)
        {
            if (id == 0) return null;
            return CustomCommands.AsQueryable().Where(uc => uc.User == id && (type == UserCommandSource.Unspecified || uc.CommandType == type)).FirstOrDefault();
        }

    }

}
