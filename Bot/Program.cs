using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using NetCoreAudio;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using CSharp_Project.Modules;
using Npgsql;
using System.Diagnostics;

namespace CSharp_Project
{
    class Program
    {
        #region publicFields
        // public SqlHandler? SqlHandler;
        #endregion
        
        #region privateFields
        public DiscordSocketClient? Client;
        private CommandService? _commands;
        private IServiceProvider? _services;

        // Please view: https://discordnet.dev/api/Discord.WebSocket.DiscordSocketConfig.html for more info.
        private readonly DiscordSocketConfig _config = new()
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100,
            LogGatewayIntentWarnings = false,
            HandlerTimeout = null,
            AlwaysDownloadDefaultStickers = true,
            AlwaysResolveStickers = true,
        };
        
        private static readonly string CurrentPath = "C:\\Users\\mom2b\\RiderProjects\\Discord Bot\\Bot";

        private readonly Player _player = new();
        private SocketGuild? _supportGuild;
        #endregion
        
        public static void Main(string[] args)
        {
            Console.Clear();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            // SqlHandler = new SqlHandler(await File.ReadAllTextAsync(CurrentPath + "\\connectionString.txt"));
            Client = new(_config);
            _commands = new();

            // Initialize service provider
            _services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(_commands)
                .AddSingleton<IAudioService, LavalinkNode>()
                .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
                .AddSingleton(new LavalinkNodeOptions
                {
                    RestUri = "lavalink.oops.wtf",
                    WebSocketUri = "wss://lavalink.oops.wtf",
                    Password = "www.freelavalink.ga",
                    AllowResuming = true,
                })
                .BuildServiceProvider();

            // Register event functions
            Client.Log += Log;
            Client.Ready += Client_Ready;
            Client.JoinedGuild += Guild_Joined;
            Client.UserIsTyping += Typing_Start;

            string token = await File.ReadAllTextAsync(CurrentPath + "\\token.txt");

            await RegisterCommandsAsync();

            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task Typing_Start(Cacheable<IUser, ulong> userCache, Cacheable<IMessageChannel, ulong> messageCache)
        {
            IUser user = await userCache.GetOrDownloadAsync();
            IMessageChannel channel = await messageCache.GetOrDownloadAsync();
        }

        private async Task Guild_Joined(SocketGuild guild)
        {
            SocketTextChannel channel = _supportGuild!.GetTextChannel(927333175582158959);
            Embed embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle("New Guild!")
                .WithDescription(Client!.CurrentUser.Username + " was added to a new guild!")
                .AddField("Guild Name", guild.Name, true)
                .AddField("New Guild Count", Client.Guilds.Count, true)
                .Build();

            await channel.SendMessageAsync(embed: embed);
        }

        private async Task RegisterCommandsAsync()
        {
            Client!.MessageReceived += HandleCommandAsync;
            await _commands!.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private Task Log(LogMessage log)
        {
            // Console.Beep();
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        // Event that executes when the client is ready.
        private async Task Client_Ready()
        {
            _supportGuild = Client!.GetGuild(833671287381032970);
            await _player.Play(CurrentPath + "\\Audio/startup.mp3");
            Console.WriteLine("The client is ready!");
            // NpgsqlCommand command = new NpgsqlCommand
            //{
            //    Connection = SqlHandler!.Connection,
            //};
            //SqlHandler.DataReaderCommand(command);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage? message = arg as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;
            SocketCommandContext context = new(Client, message);

            int argPos = 0;
            if (!message.HasStringPrefix(await File.ReadAllTextAsync(CurrentPath + "\\prefix.txt"), ref argPos)) return;
            IResult result = await _commands!.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess)
            {
                if (result.Error.Equals(CommandError.UnmetPrecondition))
                {
                    await message.ReplyAsync(result.ErrorReason);
                    return;
                }
                await _player.Play(CurrentPath + "\\Audio/error.mp3");
                Console.WriteLine(result.ErrorReason);
                await message.ReplyAsync("An error occurred, this was reported to the developers!\n\n**Error:** ||" + result.ErrorReason + "||");
            }
        }
    }
}
