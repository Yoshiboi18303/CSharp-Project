using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;
using Discord.Rest;
using CSharp_Project.Modules;
using Lavalink4NET.DiscordNet;

namespace CSharp_Project.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private LavalinkPlayer? _player;
        #region info_commands
        [Command("ping")]
        public async Task Ping()
        {
            int ping = Context.Client.Latency;
            await ReplyAsync("Pong! **" + ping + "ms**");
        }
        
        [Command("userinfo")]
        public async Task UserInfo(IGuildUser? user = null)
        {
            user ??= Context.User as IGuildUser;
            
            await ReplyAsync("Check the console!");
        }
        #endregion

        #region moderation_commands
        [Command("ban")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "You don't have the `BAN_MEMBERS` permission!")]
        [RequireBotPermission(GuildPermission.BanMembers, ErrorMessage = "I don't have the `BAN_MEMBERS` permission!")]
        public async Task Ban(IGuildUser? user = null, int days = 0, [Remainder] string reason = "No reason provided!")
        {
            SocketGuild guild = Context.Guild;
            if (user == null)
            {
                Embed noUserEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Please provide a user to ban!")
                    .Build();
                await ReplyAsync(embed: noUserEmbed);
                return;
            }

            if (days < 0 || days > 7)
            {
                Embed invalidDaysEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Your `days` argument is past the range of 0 and 7!")
                    .Build();
                await ReplyAsync(embed: invalidDaysEmbed);
                return;
            }

            await guild.AddBanAsync(user, days, reason);
            Embed completedEmbed = new EmbedBuilder()
                .WithTitle("Success")
                .WithColor(Color.Green)
                .WithDescription(user.Username + " was banned from " + guild.Name + "!")
                .Build();

            IEnumerable<EmbedFieldBuilder> fields = new[]{
                new EmbedFieldBuilder()
                    .WithName("Moderator")
                    .WithValue(Context.User.Username)
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Reason")
                    .WithValue(reason)
                    .WithIsInline(true),
            };

            Embed bannedEmbed = new EmbedBuilder()
                .WithTitle("Banned")
                .WithDescription("You were banned from " + guild.Name)
                .WithColor(Color.Red)
                .WithFields(fields)
                .Build();

            bool sendWarning = false;

            try
            {
                await user.SendMessageAsync(embed: bannedEmbed);
            }
            catch (HttpException)
            {
                sendWarning = true;
            }

            if (!sendWarning)
            {
                await ReplyAsync(embed: completedEmbed);
            }
            else
            {
                await ReplyAsync(message: "I couldn't DM the user, but they were banned from **" + guild.Name + "**, don't worry!", embed: completedEmbed);
            }
        }

        [Command("unban")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "You don't have the `BAN_MEMBERS` permission!")]
        public async Task Unban(ulong? user = null)
        {
            RestBan ban = await Context.Guild.GetBanAsync(userId: (ulong)user!);

            if (ban == null)
            {
                Embed noBanEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("No ban was found for user ID: **`" + (ulong)user + "`**")
                    .Build();
                await ReplyAsync(embed: noBanEmbed);
                return;
            }

            await Context.Guild.RemoveBanAsync(userId: (ulong)user);

            Embed unbannedEmbed = new EmbedBuilder()
                .WithTitle("Success")
                .WithColor(Color.Green)
                .WithDescription(ban.User.Username + " was unbanned successfully!")
                .Build();

            await ReplyAsync(message: Context.User.Mention, embed: unbannedEmbed);
        }

        [Command("kick")]
        [RequireUserPermission(GuildPermission.KickMembers, ErrorMessage = "You don't have the `KICK_MEMBERS` permission!")]
        [RequireBotPermission(GuildPermission.KickMembers, ErrorMessage = "I don't have the `KICK_MEMBERS` permission!")]
        public async Task Kick(IGuildUser? user = null, [Remainder] string reason = "No reason provided!")
        {
            if (user == null)
            {
                Embed noUserEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Please provide a user to kick!")
                    .Build();
                await ReplyAsync(embed: noUserEmbed);
                return;
            }

            await user.KickAsync(reason);

            Embed successEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Success")
                .WithDescription(user.Mention + " was successfully kicked from " + Context.Guild.Name + "!")
                .Build();

            IEnumerable<EmbedFieldBuilder> fields = new[]{
                new EmbedFieldBuilder()
                    .WithName("Moderator")
                    .WithValue(Context.User.Mention + " (" + Context.User.Username + "#" + Context.User.Discriminator + ")")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Reason")
                    .WithValue(reason)
                    .WithIsInline(true),
            };

            Embed kickedEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Kicked")
                .WithDescription("You were kicked from " + Context.Guild.Name + "!")
                .WithFields(fields)
                .Build();

            await ReplyAsync(message: Context.User.Mention, embed: successEmbed);
            await user.SendMessageAsync(embed: kickedEmbed);

        }

        [Command("bans")]
        [RequireUserPermission(GuildPermission.BanMembers, ErrorMessage = "You don't have the `BAN_MEMBERS` permission!")]
        public async Task Bans()
        {
            IAsyncEnumerable<IReadOnlyCollection<RestBan>> bans = Context.Guild.GetBansAsync();
            IEnumerable<string> map = new string[]{};
            int banNumber = 1;
            await foreach (IReadOnlyCollection<RestBan> guildBans in bans)
            {
                map = (from ban in guildBans let bannedUser = ban.User select "`" + banNumber + "` - **User:** " + bannedUser.Mention + "`(" + bannedUser.Username + "#" + bannedUser.Discriminator + ")`\n**Reason:** " + ban.Reason).Aggregate(map, (current, content) => current.Append(content));
            }

            EmbedBuilder bansEmbedBuilder = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("Bans in " + Context.Guild.Name);

            Embed bansEmbed = bansEmbedBuilder.Build();

            await ReplyAsync(embed: bansEmbed);
        }
        #endregion
        
        #region music_commands

        [Command("play")]
        [RequireBotPermission(GuildPermission.Connect,
            ErrorMessage = "I don't have the `CONNECT` permission in this guild!")]
        [RequireBotPermission(GuildPermission.Speak,
            ErrorMessage = "I don't have the `SPEAK` permission in this guild!")]
        public async Task PlaySong([Remainder] string? query = null)
        {
            _player ??= new LavalinkPlayer(Context.Client, clientWrapper: new DiscordClientWrapper(Context.Client));
            bool currentUserInVoiceChannel = false;
            SocketVoiceChannel? currentVoiceChannel = null;
            // List<IReadOnlyCollection<SocketGuildUser>> allConnectedUsers = new List<IReadOnlyCollection<SocketGuildUser>>();

            foreach (SocketVoiceChannel voiceChannel in Context.Guild.VoiceChannels)
            {
                foreach (SocketGuildUser user in voiceChannel.ConnectedUsers)
                {
                    if (user.Id == Context.User.Id)
                    {
                        currentUserInVoiceChannel = true;
                        currentVoiceChannel = voiceChannel;
                    }
                }
            }

            if (!currentUserInVoiceChannel)
            {
                Embed noVoiceChannelEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Please join a voice channel!")
                    .Build();

                await ReplyAsync(embed: noVoiceChannelEmbed);
                return;
            }

            if (query == null)
            {
                Embed noQueryEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithDescription("Please provide a query!")
                    .Build();

                await ReplyAsync(embed: noQueryEmbed);
                return;
            }

            _player.PlaySong(currentVoiceChannel!, Context.Guild, Context.Channel as SocketTextChannel, query);
        }
        #endregion
    }
}
