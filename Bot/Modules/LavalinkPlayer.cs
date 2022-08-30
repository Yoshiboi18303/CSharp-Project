using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace CSharp_Project.Modules;

public class LavalinkPlayer
{
    public LavalinkNode Node;
    public Lavalink4NET.Player.LavalinkPlayer Player;
    private DiscordSocketClient _client;
    private int _trackNumber = 1;
    private int[] _trackNumbers = new Int32[] { };
    private bool _cancelCollector = false;
    private LavalinkTrack _track;
    private LavalinkTrack[] _tracks = new LavalinkTrack[] { };
    private bool _trackReceived = false;

    public LavalinkPlayer(DiscordSocketClient client, Lavalink4NET.Logging.ILogger? logger = null, IDiscordClientWrapper? clientWrapper = null)
    {
        if (clientWrapper == null) clientWrapper = new DiscordClientWrapper(client);
        Node = new LavalinkNode(new LavalinkNodeOptions
        {
            // Free Lavalink Server, I'm fine to disclose the source.
            RestUri = "lavalink.oops.wtf",
            Password = "www.freelavalink.ga",
            WebSocketUri = "wss://lavalink.oops.wtf"
        }, clientWrapper, logger);
        Node.InitializeAsync();
        _client = client;
    }
    
    
    /**
     * <summary>
     * Adds whatever song is chosen by the end-user
     * </summary>
     * <param name="voiceChannel">
     * The voice channel to use.
     * </param>
     * <param name="guild">
     */
    public async void PlaySong(SocketVoiceChannel voiceChannel, SocketGuild guild, SocketTextChannel textChannel, string query, SearchMode? mode = null)
    {
        ulong voiceChannelId = voiceChannel.Id;
        ulong guildId = guild.Id;

        var player = Node.GetPlayer(guildId) ?? await Node.JoinAsync(guildId: guildId, voiceChannelId: voiceChannelId);

        Player = player;

        if (mode == null)
        {
            string querySubstring = query[(query.StartsWith("http") ? 6 : 7)..];
            if (querySubstring.StartsWith("youtube.com"))
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
                    _trackNumbers.Append(_trackNumber);
                    _tracks.Append(result);
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
        else
        {
            
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
