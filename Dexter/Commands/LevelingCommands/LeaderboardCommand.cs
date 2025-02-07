﻿using Dexter.Configurations;
using Dexter.Databases.Levels;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dexter.Commands
{

    public partial class LevelingCommands
    {

        /// <summary>
        /// Creates a leaderboard spanning users from page <paramref name="min"/> to page <paramref name="max"/> and posts it in chat.
        /// </summary>
        /// <param name="min">The first page to display</param>
        /// <param name="max">The last page to display</param>
        /// <returns>A <c>Task</c> object, which can be awaited until the method completes successfully.</returns>

        [Command("levels")]
        [Alias("leaderboard")]
        [Summary("Usage: `levels (min) (max)`")]

        public async Task LeaderboardCommand(int min = 1, int max = 100)
        {
            if (min >= max)
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Invalid range!")
                    .WithDescription($"Argument \"max\" ({max}) must be lower than \"min\" ({min})")
                    .SendEmbed(Context.Channel);
                return;
            }
            if (max - min > LevelingConfiguration.MaxLeaderboardItems)
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Invalid range!")
                    .WithDescription($"Item count exceeds maximum! You may request up to {LevelingConfiguration.MaxLeaderboardItems} items. You requested {max - min}.")
                    .SendEmbed(Context.Channel);
                return;
            }
            List<UserLevel> textLevels = LevelingDB.Levels.ToList();
            List<UserLevel> voiceLevels = LevelingDB.Levels.ToList();

            textLevels.Sort((a, b) => b.TextXP.CompareTo(a.TextXP));
            voiceLevels.Sort((a, b) => b.VoiceXP.CompareTo(a.VoiceXP));

            List<LeaderboardItem> lbitems = new();
            for (int i = min - 1; i < max && i < Math.Min(textLevels.Count, voiceLevels.Count); i++)
            {
                lbitems.Add(new(i + 1, textLevels[i], voiceLevels[i], DiscordSocketClient, LevelingConfiguration));
            }

            string file = LeaderboardPath(lbitems);
            await Context.Channel.SendFileAsync(file);
            File.Delete(file);
        }

        /// <summary>
        /// Creates a leaderboard HTML file in the cached images directory and returns the path to it.
        /// </summary>
        /// <param name="levels">The Leaderboard items to include in the leaderboard.</param>
        /// <returns>A string containing the path to the generated file.</returns>
        public string LeaderboardPath(IEnumerable<LeaderboardItem> levels)
        {
            const string tempCacheFileName = "leaderboard.html";

            string finalPath = Path.Combine(
                Directory.GetCurrentDirectory(), "ImageCache", tempCacheFileName);

            string levelTemplate = File.ReadAllText(Path.Combine(
                Directory.GetCurrentDirectory(), "Images", "OtherMedia", "HTML", "Leaderboard", "LevelItemTemplate.html"));
            using StreamReader leaderboardTemplate = new(Path.Combine(
                Directory.GetCurrentDirectory(), "Images", "OtherMedia", "HTML", "Leaderboard", "leaderboard.html"));
            using StreamWriter leaderboardOutput = new(finalPath);

            string line = leaderboardTemplate.ReadLine();
            while (line is not null)
            {
                if (line.Contains("$LIST"))
                {
                    foreach (LeaderboardItem li in levels)
                    {
                        leaderboardOutput.Write(li.ToString(levelTemplate));
                    }
                }
                else
                {
                    leaderboardOutput.WriteLine(line);
                }
                line = leaderboardTemplate.ReadLine();
            }

            return finalPath;
        }

        /// <summary>
        /// Holds the relevant information to create a leaderboard item for one specific rank.
        /// </summary>

        public class LeaderboardItem
        {
            private int rank;
            /// <summary>
            /// The userlevel corresponding to the user whose rank is <see cref="rank"/> on text.
            /// </summary>
            public readonly UserLevel text;
            /// <summary>
            /// The userlevel corresponding to the user whose rank is <see cref="rank"/> on voice.
            /// </summary>
            public readonly UserLevel voice;
            private DiscordSocketClient client;
            private LevelingConfiguration config;

            /// <summary>
            /// Standard constructor of the class.
            /// </summary>
            /// <param name="rank">The rank the object represents</param>
            /// <param name="text">The UserLevel who this text rank corresponds to.</param>
            /// <param name="voice">The UserLevel who this voice rank corresponds to.</param>
            /// <param name="client">The standard DiscordSocketClient necessary for user parsing.</param>
            /// <param name="config">The standard LevelingConfig necessary for level calculations.</param>

            public LeaderboardItem(int rank, UserLevel text, UserLevel voice, DiscordSocketClient client, LevelingConfiguration config)
            {
                this.rank = rank;
                this.text = text;
                this.voice = voice;
                this.client = client;
                this.config = config;
            }

            private string ReplaceAll(string template, bool isText)
            {
                UserLevel reference = isText ? text : voice;
                IUser user = client.GetUser(reference.UserID);
                string name;
                string avatarurl;
                if (user is not null)
                {
                    name = $"{user.Username}#{user.Discriminator}";
                    avatarurl = user.GetTrueAvatarUrl();
                }
                else
                {
                    name = reference.UserID.ToString();
                    avatarurl = "https://cdn.discordapp.com/attachments/792661500182790174/856996405288632370/QMarkAlpha.png";
                }


                long xp = isText ? reference.TextXP : reference.VoiceXP;
                int lvl = config.GetLevelFromXP(xp, out long rxp, out long lxp);
                float fraction = (float)rxp / lxp;
                int rot = (int)(fraction * 360);
                int leftrot = rot > 180 ? rot : 180;
                int rightrot = rot > 180 ? 180 : rot;

                return template
                    .Replace("$TYPE", isText ? "text" : "voice hide")
                    .Replace("$RANK", rank.ToString())
                    .Replace("$PFPURL", avatarurl)
                    .Replace("$NAME", name)
                    .Replace("$EXP", xp.ToUnit())
                    .Replace("$LVL", lvl.ToString())
                    .Replace("$LEFTROT", leftrot.ToString())
                    .Replace("$RIGHTROT", rightrot.ToString());
            }

            /// <summary>
            /// Converts the object into the full HTML expression obtained from <paramref name="template"/>.
            /// </summary>
            /// <param name="template">The HTML template with annotations to change into their corresponding values.</param>
            /// <returns>A fully formed HTML expression which contains the text and hidden voice rank item.</returns>

            public string ToString(string template)
            {
                return $"{ReplaceAll(template, true)}"
                    + $"{ReplaceAll(template, false)}";
            }
        }
    }

}
