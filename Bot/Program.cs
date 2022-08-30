using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using NetCoreAudio;
using MongoDB.Driver;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;

namespace CSharp_Project
{
    class Program
    {
        private DiscordSocketClient? _client;
        private CommandService? _commands;
        private IServiceProvider? _services;
        
        // Please view: https://discordnet.dev/api/Discord.WebSocket.DiscordSocketConfig.html for more info.
        private readonly DiscordSocketConfig _config = new DiscordSocketConfig()
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

        private readonly Player _player = new Player();
        private MongoClient _mongoClient = new MongoClient(File.ReadAllText(CurrentPath + "\\connectionString.txt"));
        private SocketGuild? _supportGuild;

        public static void Main(string[] args)
        {
            Console.Clear();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            _client = new DiscordSocketClient(_config);
            _commands = new CommandService();

            // Initialize service provider
            _services = new ServiceCollection()
                .AddSingleton(_client)
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
            _client.Log += Log;
            _client.Ready += _client_Ready;
            _client.JoinedGuild += Guild_Joined;
            _client.UserIsTyping += Typing_Start;

            string token = await File.ReadAllTextAsync(CurrentPath + "\\token.txt");

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

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
                .WithDescription(_client!.CurrentUser.Username + " was added to a new guild!")
                .AddField("Guild Name", guild.Name, true)
                .AddField("New Guild Count", _client.Guilds.Count, true)
                .Build();

            await channel.SendMessageAsync(embed: embed);
        }

        private async Task RegisterCommandsAsync()
        {
            _client!.MessageReceived += HandleCommandAsync;
            await _commands!.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private Task Log(LogMessage log)
        {
            // Console.Beep();
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        // Event that executes when the client is ready.
        private async Task _client_Ready()
        {
            _supportGuild = _client!.GetGuild(833671287381032970);
            await _player.Play(CurrentPath + "\\Audio/startup.mp3");
            Console.WriteLine("The client is ready!");
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage? message = arg as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;
            SocketCommandContext context = new SocketCommandContext(_client, message);

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
