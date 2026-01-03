// PKM Universe Bot - Trade Bot Runner
// Written by PKM Universe - 2025

using PKHeX.Core;
using PKMUniverse.Core.Config;
using PKMUniverse.Core.Logging;
using PKMUniverse.Switch.Connection;
using PKMUniverse.Trade.Queue;

namespace PKMUniverse.Trade.Executor;

public class TradeBotRunner
{
    private readonly TradeQueue<PKM> _queue;
    private readonly List<TradeBotInstance> _bots = new();
    private readonly object _lock = new();
    private bool _running;
    private bool _paused;

    public int TotalBots => _bots.Count;
    public int ActiveBots => _bots.Count(b => b.IsProcessing);
    public int QueueSize => _queue.Count;
    public bool IsPaused => _paused;

    public event Action<string, string, bool>? OnTradeComplete;
    public event Action<string, string>? OnBotStatusChanged;

    public TradeBotRunner(int maxQueueSize = 50)
    {
        _queue = new TradeQueue<PKM>(maxQueueSize);

        _queue.OnTradeAdded += (entry) => Logger.Trade("Queue", $"New trade added: {entry.TrainerName}");
        _queue.OnTradeCompleted += (entry) => OnTradeComplete?.Invoke(entry.PokemonName, entry.TrainerName, entry.Status == TradeStatus.Completed);
    }

    public async Task AddBotAsync(SwitchBotConfig config)
    {
        var connection = new SwitchConnection(config.IP, config.Port);
        var connected = await connection.ConnectAsync();

        if (!connected)
        {
            Logger.Error("Runner", $"Failed to connect bot {config.Name} to {config.IP}");
            return;
        }

        TradeExecutor executor = config.Game switch
        {
            Core.Config.GameVersion.LegendsZA => new TradeExecutorLZA(connection, config.Name),
            Core.Config.GameVersion.ScarletViolet => new TradeExecutorSV(connection, config.Name),
            _ => new TradeExecutorLZA(connection, config.Name)
        };

        var bot = new TradeBotInstance(config.Name, connection, executor);
        bot.OnTradeComplete += (pokemon, trainer, success) =>
        {
            OnTradeComplete?.Invoke(pokemon, trainer, success);
        };

        lock (_lock)
        {
            _bots.Add(bot);
        }

        Logger.Info("Runner", $"Bot {config.Name} added and connected");
        OnBotStatusChanged?.Invoke(config.Name, "Connected");
    }

    public void RemoveBot(string name)
    {
        lock (_lock)
        {
            var bot = _bots.FirstOrDefault(b => b.Name == name);
            if (bot != null)
            {
                bot.Stop();
                _bots.Remove(bot);
                Logger.Info("Runner", $"Bot {name} removed");
            }
        }
    }

    public QueueResult AddToQueue(ulong userId, string trainerName, PKM pokemon, int tradeCode)
    {
        var entry = new TradeEntry<PKM>
        {
            UserId = userId,
            TrainerName = trainerName,
            Pokemon = pokemon,
            PokemonName = GetPokemonName(pokemon),
            TradeCode = tradeCode,
            Type = TradeType.Trade
        };

        return _queue.Enqueue(entry);
    }

    public QueueResult AddCloneToQueue(ulong userId, string trainerName, int tradeCode)
    {
        var entry = new TradeEntry<PKM>
        {
            UserId = userId,
            TrainerName = trainerName,
            Pokemon = null,
            PokemonName = "Clone Trade",
            TradeCode = tradeCode,
            Type = TradeType.Clone
        };

        return _queue.Enqueue(entry);
    }

    public QueueResult AddDumpToQueue(ulong userId, string trainerName, int tradeCode)
    {
        var entry = new TradeEntry<PKM>
        {
            UserId = userId,
            TrainerName = trainerName,
            Pokemon = null,
            PokemonName = "Dump Trade",
            TradeCode = tradeCode,
            Type = TradeType.Dump
        };

        return _queue.Enqueue(entry);
    }

    public bool RemoveFromQueue(ulong userId)
    {
        return _queue.Remove(userId);
    }

    public int GetQueuePosition(ulong userId)
    {
        return _queue.GetPosition(userId);
    }

    public void Start()
    {
        _running = true;
        _paused = false;

        foreach (var bot in _bots)
        {
            _ = RunBotAsync(bot);
        }

        Logger.Info("Runner", "Trade bot runner started");
    }

    public void Stop()
    {
        _running = false;

        lock (_lock)
        {
            foreach (var bot in _bots)
            {
                bot.Stop();
            }
        }

        Logger.Info("Runner", "Trade bot runner stopped");
    }

    public void Pause()
    {
        _paused = true;
        Logger.Info("Runner", "Trade bot runner paused");
    }

    public void Resume()
    {
        _paused = false;
        Logger.Info("Runner", "Trade bot runner resumed");
    }

    private async Task RunBotAsync(TradeBotInstance bot)
    {
        while (_running)
        {
            if (_paused)
            {
                await Task.Delay(1000);
                continue;
            }

            if (bot.IsProcessing)
            {
                await Task.Delay(500);
                continue;
            }

            var entry = _queue.Dequeue();
            if (entry == null)
            {
                await Task.Delay(1000);
                continue;
            }

            entry.AssignedBot = bot.Name;
            _queue.MarkStarted(entry);
            OnBotStatusChanged?.Invoke(bot.Name, $"Trading with {entry.TrainerName}");

            var success = await bot.ProcessTradeAsync(entry);

            _queue.MarkCompleted(entry, success);
            OnBotStatusChanged?.Invoke(bot.Name, success ? "Trade Complete" : "Trade Failed");

            await Task.Delay(2000);
        }
    }

    private static string GetPokemonName(PKM pokemon)
    {
        var species = GameInfo.Strings.Species;
        if (pokemon.Species < species.Count)
            return species[pokemon.Species];
        return $"Pokemon #{pokemon.Species}";
    }

    public List<BotStatus> GetBotStatuses()
    {
        lock (_lock)
        {
            return _bots.Select(b => new BotStatus
            {
                Name = b.Name,
                IsConnected = b.IsConnected,
                IsProcessing = b.IsProcessing,
                TradeCount = b.TradeCount,
                LastTrade = b.LastTradeTime,
                CurrentTrainer = b.CurrentTrainer
            }).ToList();
        }
    }
}

public class TradeBotInstance
{
    private readonly SwitchConnection _connection;
    private readonly TradeExecutor _executor;
    private CancellationTokenSource? _cts;

    public string Name { get; }
    public bool IsConnected => _connection.IsConnected;
    public bool IsProcessing { get; private set; }
    public int TradeCount { get; private set; }
    public DateTime? LastTradeTime { get; private set; }
    public string? CurrentTrainer { get; private set; }

    public event Action<string, string, bool>? OnTradeComplete;

    public TradeBotInstance(string name, SwitchConnection connection, TradeExecutor executor)
    {
        Name = name;
        _connection = connection;
        _executor = executor;

        _executor.OnTradeComplete += (pokemon, trainer, success) =>
        {
            if (success) TradeCount++;
            LastTradeTime = DateTime.Now;
            OnTradeComplete?.Invoke(pokemon, trainer, success);
        };
    }

    public async Task<bool> ProcessTradeAsync(TradeEntry<PKM> entry)
    {
        if (!IsConnected)
        {
            Logger.Error(Name, "Cannot process trade - not connected");
            return false;
        }

        IsProcessing = true;
        CurrentTrainer = entry.TrainerName;
        _cts = new CancellationTokenSource();

        try
        {
            return await _executor.ExecuteTradeAsync(entry, _cts.Token);
        }
        finally
        {
            IsProcessing = false;
            CurrentTrainer = null;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        IsProcessing = false;
    }
}

public class BotStatus
{
    public string Name { get; set; } = "";
    public bool IsConnected { get; set; }
    public bool IsProcessing { get; set; }
    public int TradeCount { get; set; }
    public DateTime? LastTrade { get; set; }
    public string? CurrentTrainer { get; set; }
}
