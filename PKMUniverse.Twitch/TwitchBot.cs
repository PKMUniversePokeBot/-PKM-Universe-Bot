// PKM Universe Bot - Twitch Bot Integration
// Written by PKM Universe - 2025

using PKHeX.Core;
using PKMUniverse.Core.Logging;
using PKMUniverse.Trade.Executor;
using PKMUniverse.Trade.Queue;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace PKMUniverse.Twitch;

public class TwitchBot
{
    private readonly TwitchClient _client;
    private readonly TwitchConfig _config;
    private readonly TradeBotRunner _runner;
    private readonly Dictionary<string, DateTime> _cooldowns = new();
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public event Action<string>? OnLog;

    public TwitchBot(TwitchConfig config, TradeBotRunner runner)
    {
        _config = config;
        _runner = runner;

        var credentials = new ConnectionCredentials(_config.Username, _config.OAuthToken);
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        var customClient = new WebSocketClient(clientOptions);
        _client = new TwitchClient(customClient);
        _client.Initialize(credentials, _config.Channel);

        _client.OnConnected += Client_OnConnected;
        _client.OnDisconnected += Client_OnDisconnected;
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnJoinedChannel += Client_OnJoinedChannel;
        _client.OnError += Client_OnError;
    }

    public void Connect()
    {
        Log("Connecting to Twitch...");
        _client.Connect();
    }

    public void Disconnect()
    {
        Log("Disconnecting from Twitch...");
        _client.Disconnect();
        _isConnected = false;
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        _isConnected = true;
        Log($"Connected to Twitch as {e.BotUsername}");
    }

    private void Client_OnDisconnected(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
    {
        _isConnected = false;
        Log("Disconnected from Twitch");
    }

    private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Log($"Joined channel: {e.Channel}");
        _client.SendMessage(e.Channel, "PKM Universe Bot is now online! Use !trade <showdown set> to request a trade.");
    }

    private void Client_OnError(object? sender, TwitchLib.Communication.Events.OnErrorEventArgs e)
    {
        Log($"Twitch error: {e.Exception.Message}");
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var message = e.ChatMessage.Message;
        var username = e.ChatMessage.Username;
        var userId = e.ChatMessage.UserId;

        if (!message.StartsWith(_config.CommandPrefix))
            return;

        var parts = message.Substring(_config.CommandPrefix.Length).Split(' ', 2);
        var command = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1] : "";

        // Check subscriber-only mode
        if (_config.SubscriberOnly && !e.ChatMessage.IsSubscriber && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
        {
            _client.SendMessage(e.ChatMessage.Channel, $"@{username} Trading is currently subscriber-only!");
            return;
        }

        // Check cooldown
        if (_cooldowns.TryGetValue(username.ToLower(), out var lastCommand))
        {
            var elapsed = DateTime.Now - lastCommand;
            if (elapsed.TotalSeconds < _config.CooldownSeconds && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                var remaining = _config.CooldownSeconds - (int)elapsed.TotalSeconds;
                _client.SendMessage(e.ChatMessage.Channel, $"@{username} Please wait {remaining}s before using another command.");
                return;
            }
        }

        _cooldowns[username.ToLower()] = DateTime.Now;

        switch (command)
        {
            case "trade":
                HandleTradeCommand(e.ChatMessage, args);
                break;
            case "queue":
                HandleQueueCommand(e.ChatMessage);
                break;
            case "position":
                HandlePositionCommand(e.ChatMessage, userId);
                break;
            case "cancel":
                HandleCancelCommand(e.ChatMessage, userId);
                break;
            case "status":
                HandleStatusCommand(e.ChatMessage);
                break;
            case "help":
                HandleHelpCommand(e.ChatMessage);
                break;
            case "clone":
                HandleCloneCommand(e.ChatMessage, args);
                break;
        }
    }

    private void HandleTradeCommand(ChatMessage chat, string showdownSet)
    {
        if (string.IsNullOrWhiteSpace(showdownSet))
        {
            _client.SendMessage(chat.Channel, $"@{chat.Username} Usage: !trade <showdown set>");
            return;
        }

        if (!_config.EnableTrading)
        {
            _client.SendMessage(chat.Channel, $"@{chat.Username} Trading is currently disabled.");
            return;
        }

        try
        {
            var pokemon = ParseShowdown(showdownSet);
            if (pokemon == null)
            {
                _client.SendMessage(chat.Channel, $"@{chat.Username} Could not parse your Showdown set. Please check the format.");
                return;
            }

            // Generate random trade code
            var tradeCode = new Random().Next(10000000, 99999999);
            var parsedUserId = ulong.TryParse(chat.UserId, out var uid) ? uid : 0;

            var result = _runner.AddToQueue(
                parsedUserId,
                chat.Username,
                pokemon,
                tradeCode
            );

            if (result == QueueResult.Success)
            {
                var position = _runner.GetQueuePosition(parsedUserId);
                var pokemonName = GetPokemonName(pokemon);
                _client.SendMessage(chat.Channel, $"@{chat.Username} {pokemonName} added to queue! Position: #{position} | Trade Code: {tradeCode}");
                Log($"Trade queued for {chat.Username}: {pokemonName}");
            }
            else
            {
                _client.SendMessage(chat.Channel, $"@{chat.Username} Could not add to queue: {result}");
            }
        }
        catch (Exception ex)
        {
            Log($"Trade error: {ex.Message}");
            _client.SendMessage(chat.Channel, $"@{chat.Username} Error processing trade request.");
        }
    }

    private void HandleCloneCommand(ChatMessage chat, string args)
    {
        if (!_config.EnableTrading)
        {
            _client.SendMessage(chat.Channel, $"@{chat.Username} Trading is currently disabled.");
            return;
        }

        // Generate random trade code
        var tradeCode = new Random().Next(10000000, 99999999);
        var parsedUserId = ulong.TryParse(chat.UserId, out var uid) ? uid : 0;

        var result = _runner.AddCloneToQueue(parsedUserId, chat.Username, tradeCode);

        if (result == QueueResult.Success)
        {
            var position = _runner.GetQueuePosition(parsedUserId);
            _client.SendMessage(chat.Channel, $"@{chat.Username} Clone trade added to queue! Position: #{position} | Trade Code: {tradeCode} | Show me any Pokemon and I'll clone it!");
            Log($"Clone trade queued for {chat.Username}");
        }
        else
        {
            _client.SendMessage(chat.Channel, $"@{chat.Username} Could not add to queue: {result}");
        }
    }

    private void HandleQueueCommand(ChatMessage chat)
    {
        var queueSize = _runner.QueueSize;
        var isPaused = _runner.IsPaused;
        var status = isPaused ? "PAUSED" : "Active";
        _client.SendMessage(chat.Channel, $"Queue: {queueSize} trades waiting | Status: {status}");
    }

    private void HandlePositionCommand(ChatMessage chat, string twitchUserId)
    {
        var parsedUserId = ulong.TryParse(twitchUserId, out var uid) ? uid : 0;
        var position = _runner.GetQueuePosition(parsedUserId);

        if (position > 0)
            _client.SendMessage(chat.Channel, $"@{chat.Username} You are #{position} in the queue.");
        else
            _client.SendMessage(chat.Channel, $"@{chat.Username} You are not in the queue.");
    }

    private void HandleCancelCommand(ChatMessage chat, string twitchUserId)
    {
        var parsedUserId = ulong.TryParse(twitchUserId, out var uid) ? uid : 0;
        var removed = _runner.RemoveFromQueue(parsedUserId);

        if (removed)
        {
            _client.SendMessage(chat.Channel, $"@{chat.Username} Your trade has been cancelled.");
            Log($"Trade cancelled for {chat.Username}");
        }
        else
        {
            _client.SendMessage(chat.Channel, $"@{chat.Username} You don't have a trade in the queue.");
        }
    }

    private void HandleStatusCommand(ChatMessage chat)
    {
        var activeBots = _runner.ActiveBots;
        var totalBots = _runner.TotalBots;
        var queueSize = _runner.QueueSize;
        _client.SendMessage(chat.Channel, $"Bots: {activeBots}/{totalBots} online | Queue: {queueSize} trades");
    }

    private void HandleHelpCommand(ChatMessage chat)
    {
        _client.SendMessage(chat.Channel, $"@{chat.Username} Commands: !trade <showdown set> | !clone | !queue | !position | !cancel | !status");
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
        Logger.Info("Twitch", message);
        OnLog?.Invoke(message);
    }
}
