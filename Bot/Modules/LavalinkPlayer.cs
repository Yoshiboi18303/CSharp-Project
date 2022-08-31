using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Events;
using Lavalink4NET.Player;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace CSharp_Project.Modules;

public sealed class LavalinkPlayer
{
    public readonly LavalinkNode Node;
    public Lavalink4NET.Player.LavalinkPlayer Player;
    private DiscordSocketClient _client;
    private SocketTextChannel? _textChannel;
    private int _trackNumber = 1;
    private int[] _trackNumbers = { };
    private bool _cancelCollector;
    private LavalinkTrack _track;
    private LavalinkTrack[] _tracks = { };
    private bool _trackReceived;

    public LavalinkPlayer(DiscordSocketClient client, Lavalink4NET.Logging.ILogger? logger = null, IDiscordClientWrapper? clientWrapper = null)
    {
        clientWrapper ??= new DiscordClientWrapper(client);
        Node = new LavalinkNode(new LavalinkNodeOptions
        {
            // Free Lavalink Server, I'm fine to disclose the source.
            RestUri = "https://lavalink.oops.wtf",
            Password = "www.freelavalink.ga",
            WebSocketUri = "wss://lavalink.oops.wtf"
        }, clientWrapper, logger);
        Node.InitializeAsync();
        
        // Set up events
        Node.Connected += Node_Connected;
        Node.PlayerConnected += Player_Connected;
        Node.TrackStarted += Track_Started;
        
        // Add client to private field for later use
        _client = client;
    }

    private async Task Track_Started(object sender, TrackStartedEventArgs eventArgs)
    {
        LavalinkTrack currentTrack = eventArgs.Player.CurrentTrack;
        Embed trackStartedEmbed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("Now Playing")
            .WithFields(new[]{
                new EmbedFieldBuilder()
                .WithName("Title")
                .WithValue(currentTrack.Title)
                .WithIsInline(true),
                new EmbedFieldBuilder()
                .WithName("Duration")
                .WithValue($"{currentTrack.Duration.Hours.ToString()}:{currentTrack.Duration.Minutes.ToString()}:{currentTrack.Duration.Seconds.ToString()}")
                .WithIsInline(true)
                })
            .Build();
    }

    private async Task Player_Connected(object sender, PlayerConnectedEventArgs eventArgs)
    {
        Embed playerConnectedEmbed = new EmbedBuilder()
            .WithColor(Color.Green)
            .WithTitle("Player Connected")
            .WithDescription("Player connected, ready to play music!")
            .Build();
        
        Console.WriteLine($"Player in {_textChannel.Guild.Name} successfully connected!");
        
        await _textChannel?.SendMessageAsync(embed: playerConnectedEmbed);
    }

    private Task Node_Connected(object sender, ConnectedEventArgs eventArgs)
    {
        Console.WriteLine($"Connected to {eventArgs.Uri.Host} on port {eventArgs.Uri.Port.ToString()}");
        return Task.CompletedTask;
    }


    /// <summary>
    /// Finds and plays a song depending on the entered query.
    /// </summary>
    /// <param name="voiceChannel">The voice channel that the end-user is in.</param>
    /// <param name="guild">The guild to lock onto.</param>
    /// <param name="textChannel">The text channel for the end-user</param>
    /// <param name="query">The end-users' query</param>
    /// <param name="doSearch">Whether to search for the song and send the results for the end-user to choose from.</param>
    public async void PlaySong(SocketVoiceChannel voiceChannel, SocketGuild guild, SocketTextChannel textChannel, string query, bool doSearch = true)
    {
        ulong voiceChannelId = voiceChannel.Id;
        ulong guildId = guild.Id;

        _textChannel = textChannel;

        var player = Node.GetPlayer(guildId) ?? await Node.JoinAsync(guildId: guildId, voiceChannelId: voiceChannelId);

        Player = player;

            string querySubstring = query[(query.StartsWith("http") ? 6 : query.StartsWith("https") ? 7 : 0)..];
            if (querySubstring.StartsWith("youtube"))
            {

                IEnumerable<LavalinkTrack> tracks = await Node.GetTracksAsync(query);
                EmbedBuilder searchResultsEmbedBuilder =
                    new EmbedBuilder()
                        .WithColor(Color.Blue)
                        .WithTitle($"Results for `{query}`");
                string description = "";
                foreach (var result in tracks)
                {
                    description += $"`{_trackNumber}` | **{result.Title}** - `{result.Duration}`\n";
                    Debug.Assert(_trackNumbers != null, nameof(_trackNumbers) + " != null");
#pragma warning disable CA1806
                    _trackNumbers.Append(_trackNumber);
                    _tracks.Append(result);
#pragma warning restore CA1806
                    _trackNumber++;
                }

                description += "Type anything else to cancel searching.";

                Embed searchResultsEmbed = searchResultsEmbedBuilder
                    .WithDescription(description)
                    .Build();

                await textChannel.SendMessageAsync(embed: searchResultsEmbed);

                _client.MessageReceived += HandleTrackAsync;

                while (!_trackReceived) return;

                await player.PlayAsync(_track);
            }
    }
    
    // TODO Finish this method overload.
    // public async void PlaySong(ulong voiceChannelId, ulong guildId, ulong textChannelId, string query,
    //    SearchMode? mode = null)
    // {
    //    IChannel channel = await _client.GetChannelAsync(textChannelId);
    //    if (channel == null) throw new ArgumentException("Channel not found");
    //    SocketTextChannel textChannel = channel as SocketTextChannel;
        
    // }

    private async Task HandleTrackAsync(SocketMessage arg)
    {
        SocketUserMessage message = arg as SocketUserMessage;
        int argPos = 0;
        int number = 0;
        foreach(int trackNumber in _trackNumbers)
        {
            if (message.Content != trackNumber.ToString() && trackNumber == _trackNumbers[^1])
                _cancelCollector = true;
            else number = Convert.ToInt32(message.Content);
        }
        if (_cancelCollector) return;
        LavalinkTrack? track = _tracks[number - 1];
        if (track?.Title != "" && track != null)
        {
            _track = track;
            _trackReceived = true;
        }
    }

    public async void SetVolume(float newVolume)
    {
        await Player.SetVolumeAsync(newVolume);
    }
}
