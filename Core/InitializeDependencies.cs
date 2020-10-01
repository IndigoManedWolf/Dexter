﻿using Dexter.Core.Abstractions;
using Dexter.Core.Configuration;
using Dexter.Core.DiscordApp;
using Discord.Commands;
using Discord.WebSocket;
using Figgle;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dexter.Core {
    public static class InitializeDependencies {
        private static ServiceProvider Services;

        public static async Task Main() {
            Console.Title = "Starting...";
            Console.ForegroundColor = ConsoleColor.Blue;
            await Console.Out.WriteLineAsync(FiggleFonts.Standard.Render("Starting..."));

            ServiceCollection ServiceCollection = new ServiceCollection();

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfiguration)) && !Type.IsAbstract).ToList().ForEach(Type => {
                if (!File.Exists($"Configurations/{Type.Name}.json")) {
                    File.WriteAllText(
                        $"Configurations/{Type.Name}.json",
                        JsonSerializer.Serialize(
                            Activator.CreateInstance(Type),
                            new JsonSerializerOptions() { WriteIndented = true }
                        )
                    );

                    ServiceCollection.AddSingleton(Type);

                    Console.WriteLine($" This application does not have a configuration file for {Type.Name}! " +
                        $"A mock JSON class has been created in its place...");
                } else
                    ServiceCollection.AddSingleton(
                        Type,
                        JsonSerializer.Deserialize(
                            File.ReadAllText($"Configurations/{Type.Name}.json"),
                            Type,
                            new JsonSerializerOptions() { WriteIndented = true }
                        )
                    );
            });

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(Type => Type.IsSubclassOf(typeof(EntityDatabase)) && !Type.IsAbstract)
                .ToList().ForEach(Type => ServiceCollection.AddSingleton(Type));

            ServiceCollection.AddSingleton(
                new DiscordSocketClient(new DiscordSocketConfig { MessageCacheSize = 1000 })
            );

            ServiceCollection.AddSingleton<CommandService>();

            ServiceCollection.AddSingleton(typeof(CommandModule), FormatterServices.GetUninitializedObject(typeof(CommandModule)));

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(InitializableModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => ServiceCollection.AddSingleton(Type)
            );
            
            Services = ServiceCollection.BuildServiceProvider();

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(InitializableModule)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => (Services.GetService(Type) as InitializableModule).AddDelegates()
            );
            
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnProcessExit);

            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(EntityDatabase)) && !Type.IsAbstract)
                    .ToList().ForEach(
                DBType => {
                    EntityDatabase EntityDatabase = (EntityDatabase)Services.GetRequiredService(DBType);

                    if (EntityDatabase.Database.EnsureCreated()) {
                        RelationalDatabaseCreator RelationalDatabaseCreator =
                                (RelationalDatabaseCreator)EntityDatabase.Database.GetService<IDatabaseCreator>();
                        try {
                            RelationalDatabaseCreator.CreateTables();
                        } catch (Exception Exception) {
                            Console.WriteLine(Exception.Message);
                        }
                    }
                }
            );

            Services.GetRequiredService<CommandModule>().BotConfiguration = Services.GetRequiredService<BotConfiguration>();

            await Services.GetRequiredService<FrontendConsole>().RunAsync();
        }

        public static void OnProcessExit(object Sender, EventArgs Arguments) {
            Assembly.GetExecutingAssembly().GetTypes()
                    .Where(Type => Type.IsSubclassOf(typeof(JSONConfiguration)) && !Type.IsAbstract)
                    .ToList().ForEach(
                Type => {
                    File.WriteAllText(
                        $"Configurations/{Type.Name}.json",
                        JsonSerializer.Serialize(
                            Services.GetService(Type),
                            new JsonSerializerOptions() { WriteIndented = true }
                        )
                    );
                }
            );
        }
    }
}
