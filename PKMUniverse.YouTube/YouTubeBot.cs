// PKM Universe Bot - YouTube Bot Integration
// Written by PKM Universe - 2025

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using PKHeX.Core;
using PKMUniverse.Core.Logging;
using PKMUniverse.Trade.Executor;
using PKMUniverse.Trade.Queue;

namespace PKMUniverse.YouTube;

public class YouTubeBot
{
    private readonly YouTubeService _youtubeService;
    private readonly YouTubeConfig _config;
    private readonly TradeBotRunner _runner;
    private readonly Dictionary<string, DateTime> _cooldowns = new();
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private string? _activeLiveChatId;

    public bool IsRunning => _isRunning;
    public event Action<string>? OnLog;

    public YouTubeBot(YouTubeConfig config, TradeBotRunner runner)
    {
        _config = config;
        _runner = runner;

        _youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = config.ApiKey,
            ApplicationName = "PKM Universe Bot"
        });
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        _isRunning = true;

        Log("Starting YouTube chat bot...");

        // Find active live stream
        _activeLiveChatId = await GetActiveLiveChatIdAsync();

        if (string.IsNullOrEmpty(_activeLiveChatId))
        {
            Log("No active live stream found. Will retry periodically.");
        }
        else
        {
            Log($"Connected to live chat: {_activeLiveChatId}");
        }

        _ = Task.Run(() => PollChatAsync(_cts.Token));
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        Log("YouTube bot stopped");
    }

    private async Task<string?> GetActiveLiveChatIdAsync()
    {
        try
        {
            // Use provided live chat ID if available
            if (!string.IsNullOrEmpty(_config.LiveChatId))
                return _config.LiveChatId;

            // Search for active live broadcast
            var searchRequest = _youtubeService.Search.List("snippet");
            searchRequest.ChannelId = _config.ChannelId;
            searchRequest.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
            searchRequest.Type = "video";
            searchRequest.MaxResults = 1;

            var searchResponse = await searchRequest.ExecuteAsync();
            var liveVideo = searchResponse.Items?.FirstOrDefault();

            if (liveVideo == null)
                return null;

            // Get the live chat ID from the video
            var videoRequest = _youtubeService.Videos.List("liveStreamingDetails");
            videoRequest.Id = liveVideo.Id.VideoId;

            var videoResponse = await videoRequest.ExecuteAsync();
            var video = videoResponse.Items?.FirstOrDefault();

            return video?.LiveStreamingDetails?.ActiveLiveChatId;
        }
        catch (Exception ex)
        {
            Log($"Error finding live stream: {ex.Message}");
            return null;
        }
    }

    private async Task PollChatAsync(CancellationToken token)
    {
        string? nextPageToken = null;

        while (_isRunning && !token.IsCancellationRequested)
        {
            try
            {
                // Check for active stream if we don't have one
                if (string.IsNullOrEmpty(_activeLiveChatId))
                {
                    _activeLiveChatId = await GetActiveLiveChatIdAsync();
                    if (string.IsNullOrEmpty(_activeLiveChatId))
                    {
                        await Task.Delay(30000, token); // Wait 30s before retry
                        continue;
                    }
                    Log($"Found live chat: {_activeLiveChatId}");
                }

                var request = _youtubeService.LiveChatMessages.List(_activeLiveChatId, "snippet,authorDetails");
                request.PageToken = nextPageToken;

                var response = await request.ExecuteAsync(token);
                nextPageToken = response.NextPageToken;

                foreach (var message in response.Items ?? Enumerable.Empty<LiveChatMessage>())
                {
                    await ProcessMessageAsync(message);
                }

                // Use YouTube's suggested polling interval or our config
                var pollingInterval = response.PollingIntervalMillis ?? _config.PollingIntervalMs;
                await Task.Delay((int)pollingInterval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Log("Live chat ended or access denied. Waiting for new stream...");
                _activeLiveChatId = null;
                await Task.Delay(30000, token);
            }
            catch (Exception ex)
            {
                Log($"Chat polling error: {ex.Message}");
                await Task.Delay(5000, token);
            }
        }
    }

    private async Task ProcessMessageAsync(LiveChatMessage message)
    {
        var text = message.Snippet?.TextMessageDetails?.MessageText ?? message.Snippet?.DisplayMessage ?? "";
        var author = message.AuthorDetails;

        if (string.IsNullOrEmpty(text) || author == null)
            return;

        if (!text.StartsWith(_config.CommandPrefix))
            return;

        var username = author.DisplayName ?? "Unknown";
        var channelId = author.ChannelId ?? "";
        var isModerator = author.IsChatModerator ?? false;
        var isOwner = author.IsChatOwner ?? false;
        var isMember = author.IsChatSponsor ?? false;

        // Check member-only mode
        if (_config.MemberOnly && !isMember && !isModerator && !isOwner)
        {
            await SendMessageAsync($"@{username} Trading is currently member-only!");
            return;
        }

        // Check cooldown
        if (_cooldowns.TryGetValue(channelId, out var lastCommand))
        {
            var elapsed = DateTime.Now - lastCommand;
            if (elapsed.TotalSeconds < _config.CooldownSeconds && !isModerator && !isOwner)
            {
                var remaining = _config.CooldownSeconds - (int)elapsed.TotalSeconds;
                await SendMessageAsync($"@{username} Please wait {remaining}s before using another command.");
                return;
            }
        }

        _cooldowns[channelId] = DateTime.Now;

        var parts = text.Substring(_config.CommandPrefix.Length).Split(' ', 2);
        var command = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "trade":
                await HandleTradeCommandAsync(username, channelId, args);
                break;
            case "queue":
                await HandleQueueCommandAsync();
                break;
            case "position":
                await HandlePositionCommandAsync(username, channelId);
                break;
            case "cancel":
                await HandleCancelCommandAsync(username, channelId);
                break;
            case "status":
                await HandleStatusCommandAsync();
                break;
            case "help":
                await HandleHelpCommandAsync(username);
                break;
            case "clone":
                await HandleCloneCommandAsync(username, channelId);
                break;
        }
    }

    private async Task HandleTradeCommandAsync(string username, string channelId, string showdownSet)
    {
        if (string.IsNullOrWhiteSpace(showdownSet))
        {
            await SendMessageAsync($"@{username} Usage: !trade <showdown set>");
            return;
        }

        if (!_config.EnableTrading)
        {
            await SendMessageAsync($"@{username} Trading is currently disabled.");
            return;
        }

        try
        {
            var pokemon = ParseShowdown(showdownSet);
            if (pokemon == null)
            {
                await SendMessageAsync($"@{username} Could not parse your Showdown set.");
                return;
            }

            var tradeCode = new Random().Next(10000000, 99999999);
            var userId = (ulong)channelId.GetHashCode();

            var result = _runner.AddToQueue(userId, username, pokemon, tradeCode);

            if (result == QueueResult.Success)
            {
                var position = _runner.GetQueuePosition(userId);
                var pokemonName = GetPokemonName(pokemon);
                await SendMessageAsync($"@{username} {pokemonName} added! Position: #{position} | Code: {tradeCode}");
                Log($"Trade queued for {username}: {pokemonName}");
            }
            else
            {
                await SendMessageAsync($"@{username} Could not add to queue: {result}");
            }
        }
        catch (Exception ex)
        {
            Log($"Trade error: {ex.Message}");
            await SendMessageAsync($"@{username} Error processing trade request.");
        }
    }

    private async Task HandleCloneCommandAsync(string username, string channelId)
    {
        if (!_config.EnableTrading)
        {
            await SendMessageAsync($"@{username} Trading is currently disabled.");
            return;
        }

        var tradeCode = new Random().Next(10000000, 99999999);
        var userId = (ulong)channelId.GetHashCode();

        var result = _runner.AddCloneToQueue(userId, username, tradeCode);

        if (result == QueueResult.Success)
        {
            var position = _runner.GetQueuePosition(userId);
            await SendMessageAsync($"@{username} Clone trade added! Position: #{position} | Code: {tradeCode}");
            Log($"Clone trade queued for {username}");
        }
        else
        {
            await SendMessageAsync($"@{username} Could not add to queue: {result}");
        }
    }

    private async Task HandleQueueCommandAsync()
    {
        var queueSize = _runner.QueueSize;
        var status = _runner.IsPaused ? "PAUSED" : "Active";
        await SendMessageAsync($"Queue: {queueSize} trades | Status: {status}");
    }

    private async Task HandlePositionCommandAsync(string username, string channelId)
    {
        var userId = (ulong)channelId.GetHashCode();
        var position = _runner.GetQueuePosition(userId);

        if (position > 0)
            await SendMessageAsync($"@{username} You are #{position} in the queue.");
        else
            await SendMessageAsync($"@{username} You are not in the queue.");
    }

    private async Task HandleCancelCommandAsync(string username, string channelId)
    {
        var userId = (ulong)channelId.GetHashCode();
        var removed = _runner.RemoveFromQueue(userId);

        if (removed)
        {
            await SendMessageAsync($"@{username} Your trade has been cancelled.");
            Log($"Trade cancelled for {username}");
        }
        else
        {
            await SendMessageAsync($"@{username} You don't have a trade in the queue.");
        }
    }

    private async Task HandleStatusCommandAsync()
    {
        var activeBots = _runner.ActiveBots;
        var totalBots = _runner.TotalBots;
        var queueSize = _runner.QueueSize;
        await SendMessageAsync($"Bots: {activeBots}/{totalBots} | Queue: {queueSize}");
    }

    private async Task HandleHelpCommandAsync(string username)
    {
        await SendMessageAsync($"@{username} Commands: !trade <showdown> | !clone | !queue | !position | !cancel | !status");
    }

    private async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrEmpty(_activeLiveChatId))
            return;

        try
        {
            var chatMessage = new LiveChatMessage
            {
                Snippet = new LiveChatMessageSnippet
                {
                    LiveChatId = _activeLiveChatId,
                    Type = "textMessageEvent",
                    TextMessageDetails = new LiveChatTextMessageDetails
                    {
                        MessageText = message
                    }
                }
            };

            var request = _youtubeService.LiveChatMessages.Insert(chatMessage, "snippet");
            await request.ExecuteAsync();
        }
        catch (Exception ex)
        {
            Log($"Failed to send message: {ex.Message}");
        }
    }

    private PKM? ParseShowdown(string showdownSet)
    {
        try
        {
            var set = new ShowdownSet(showdownSet);
            var pokemon = new PK9();

            pokemon.Species = set.Species;
            pokemon.Form = set.Form;
            pokemon.Gender = (byte)set.Gender;
            pokemon.Nature = set.Nature;
            pokemon.Ability = set.Ability;
            pokemon.CurrentLevel = set.Level;

            if (set.Shiny)
                pokemon.SetShiny();

            var ivs = set.IVs;
            pokemon.IV_HP = ivs[0];
            pokemon.IV_ATK = ivs[1];
            pokemon.IV_DEF = ivs[2];
            pokemon.IV_SPA = ivs[3];
            pokemon.IV_SPD = ivs[4];
            pokemon.IV_SPE = ivs[5];

            var evs = set.EVs;
            pokemon.EV_HP = evs[0];
            pokemon.EV_ATK = evs[1];
            pokemon.EV_DEF = evs[2];
            pokemon.EV_SPA = evs[3];
            pokemon.EV_SPD = evs[4];
            pokemon.EV_SPE = evs[5];

            var moves = set.Moves;
            if (moves.Length > 0) pokemon.Move1 = (ushort)moves[0];
            if (moves.Length > 1) pokemon.Move2 = (ushort)moves[1];
            if (moves.Length > 2) pokemon.Move3 = (ushort)moves[2];
            if (moves.Length > 3) pokemon.Move4 = (ushort)moves[3];

            return pokemon;
        }
        catch
        {
            return null;
        }
    }

    private static string GetPokemonName(PKM pokemon)
    {
        var species = GameInfo.Strings.Species;
        if (pokemon.Species < species.Count)
            return species[pokemon.Species];
        return $"Pokemon #{pokemon.Species}";
    }

    private void Log(string message)
    {
        Logger.Info("YouTube", message);
        OnLog?.Invoke(message);
    }
}
