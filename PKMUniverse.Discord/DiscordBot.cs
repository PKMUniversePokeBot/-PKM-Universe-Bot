// PKM Universe Bot - Discord Bot
// Written by PKM Universe - 2025

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKMUniverse.Core.Config;
using PKMUniverse.Core.Logging;
using PKMUniverse.Trade.Executor;

namespace PKMUniverse.Discord;

public class DiscordBot
{
    private readonly DiscordSocketClient _client;
    private readonly CommandService _commands;
    private readonly Core.Config.DiscordConfig _config;
    private readonly TradeBotRunner _runner;
    private IServiceProvider? _services;

    public bool IsConnected => _client.ConnectionState == ConnectionState.Connected;

    public DiscordBot(Core.Config.DiscordConfig config, TradeBotRunner runner)
    {
        _config = config;
        _runner = runner;

        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            LogLevel = LogSeverity.Info
        };

        _client = new DiscordSocketClient(socketConfig);
        _commands = new CommandService();

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;
    }

    public async Task StartAsync()
    {
        if (string.IsNullOrEmpty(_config.Token))
        {
            Logger.Error("Discord", "No token configured");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();
        Logger.Info("Discord", "Bot started");
    }

    public async Task StopAsync()
    {
        await _client.StopAsync();
        Logger.Info("Discord", "Bot stopped");
    }

    private Task LogAsync(LogMessage log)
    {
        Logger.Info("Discord", log.Message ?? "");
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        Logger.Info("Discord", $"Logged in as {_client.CurrentUser.Username}");
        await _client.SetGameAsync("PKM Universe Bot | !help");
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message is not SocketUserMessage userMessage) return;
        if (userMessage.Author.IsBot) return;

        int argPos = 0;
        if (!userMessage.HasStringPrefix(_config.Prefix, ref argPos)) return;

        var context = new SocketCommandContext(_client, userMessage);
        await HandleCommandAsync(context, argPos);
    }

    private async Task HandleCommandAsync(SocketCommandContext context, int argPos)
    {
        var content = context.Message.Content.Substring(argPos).Trim();
        var parts = content.Split(' ', 2);
        var command = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1] : "";

        switch (command)
        {
            case "trade":
                await HandleTradeAsync(context, args);
                break;
            case "queue":
                await HandleQueueAsync(context);
                break;
            case "position":
                await HandlePositionAsync(context);
                break;
            case "cancel":
                await HandleCancelAsync(context);
                break;
            case "status":
                await HandleStatusAsync(context);
                break;
            case "help":
                await HandleHelpAsync(context);
                break;
            default:
                await context.Channel.SendMessageAsync($"Unknown command: {command}. Use !help for commands.");
                break;
        }
    }

    private async Task HandleTradeAsync(SocketCommandContext context, string args)
    {
        await context.Channel.SendMessageAsync("Trade command received. Attach a .pk9 file or paste a Showdown set.");
    }

    private async Task HandleQueueAsync(SocketCommandContext context)
    {
        var count = _runner.QueueSize;
        var embed = new EmbedBuilder()
            .WithTitle("Trade Queue")
            .WithDescription($"There are **{count}** trades in the queue.")
            .WithColor(Color.Purple)
            .Build();

        await context.Channel.SendMessageAsync(embed: embed);
    }

    private async Task HandlePositionAsync(SocketCommandContext context)
    {
        var position = _runner.GetQueuePosition(context.User.Id);
        if (position == 0)
        {
            await context.Channel.SendMessageAsync("You are not in the queue.");
        }
        else
        {
            await context.Channel.SendMessageAsync($"You are in position **{position}** in the queue.");
        }
    }

    private async Task HandleCancelAsync(SocketCommandContext context)
    {
        var removed = _runner.RemoveFromQueue(context.User.Id);
        if (removed)
        {
            await context.Channel.SendMessageAsync("Your trade has been cancelled.");
        }
        else
        {
            await context.Channel.SendMessageAsync("You are not in the queue.");
        }
    }

    private async Task HandleStatusAsync(SocketCommandContext context)
    {
        var statuses = _runner.GetBotStatuses();
        var embed = new EmbedBuilder()
            .WithTitle("Bot Status")
            .WithColor(Color.Purple);

        foreach (var bot in statuses)
        {
            var status = bot.IsProcessing ? $"Trading with {bot.CurrentTrainer}" :
                         bot.IsConnected ? "Idle" : "Disconnected";
            embed.AddField(bot.Name, $"Status: {status}\nTrades: {bot.TradeCount}", inline: true);
        }

        await context.Channel.SendMessageAsync(embed: embed.Build());
    }

    private async Task HandleHelpAsync(SocketCommandContext context)
    {
        var embed = new EmbedBuilder()
            .WithTitle("PKM Universe Bot Commands")
            .WithColor(Color.Purple)
            .AddField("!trade", "Request a trade (attach .pk9 or paste Showdown)")
            .AddField("!queue", "View the current queue size")
            .AddField("!position", "Check your position in the queue")
            .AddField("!cancel", "Cancel your trade request")
            .AddField("!status", "View bot status")
            .Build();

        await context.Channel.SendMessageAsync(embed: embed);
    }
}
