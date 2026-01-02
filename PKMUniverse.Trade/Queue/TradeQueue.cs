// PKM Universe Bot - Trade Queue
// Written by PKM Universe - 2025

namespace PKMUniverse.Trade.Queue;

public class TradeQueue<T> where T : class
{
    private readonly List<TradeEntry<T>> _queue = new();
    private readonly object _lock = new();
    private readonly int _maxSize;

    public int Count
    {
        get { lock (_lock) return _queue.Count(e => e.Status == TradeStatus.Waiting); }
    }

    public event Action<TradeEntry<T>>? OnTradeAdded;
    public event Action<TradeEntry<T>>? OnTradeCompleted;

    public TradeQueue(int maxSize = 50)
    {
        _maxSize = maxSize;
    }

    public QueueResult Enqueue(TradeEntry<T> entry)
    {
        lock (_lock)
        {
            if (_queue.Count(e => e.Status == TradeStatus.Waiting) >= _maxSize)
                return QueueResult.QueueFull;

            if (_queue.Any(e => e.UserId == entry.UserId && e.Status == TradeStatus.Waiting))
                return QueueResult.AlreadyInQueue;

            entry.QueueTime = DateTime.Now;
            entry.Status = TradeStatus.Waiting;
            _queue.Add(entry);
            OnTradeAdded?.Invoke(entry);
            return QueueResult.Success;
        }
    }

    public TradeEntry<T>? Dequeue()
    {
        lock (_lock)
        {
            var entry = _queue.FirstOrDefault(e => e.Status == TradeStatus.Waiting);
            return entry;
        }
    }

    public bool Remove(ulong userId)
    {
        lock (_lock)
        {
            var entry = _queue.FirstOrDefault(e => e.UserId == userId && e.Status == TradeStatus.Waiting);
            if (entry == null) return false;
            _queue.Remove(entry);
            return true;
        }
    }

    public int GetPosition(ulong userId)
    {
        lock (_lock)
        {
            var waiting = _queue.Where(e => e.Status == TradeStatus.Waiting).ToList();
            var index = waiting.FindIndex(e => e.UserId == userId);
            return index + 1;
        }
    }

    public void MarkStarted(TradeEntry<T> entry)
    {
        lock (_lock)
        {
            entry.Status = TradeStatus.Processing;
            entry.StartTime = DateTime.Now;
        }
    }

    public void MarkCompleted(TradeEntry<T> entry, bool success)
    {
        lock (_lock)
        {
            entry.Status = success ? TradeStatus.Completed : TradeStatus.Failed;
            entry.EndTime = DateTime.Now;
            OnTradeCompleted?.Invoke(entry);
        }
    }

    public List<TradeEntry<T>> GetAll()
    {
        lock (_lock)
        {
            return _queue.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }
}

public class TradeEntry<T> where T : class
{
    public ulong UserId { get; set; }
    public string TrainerName { get; set; } = "";
    public T? Pokemon { get; set; }
    public string PokemonName { get; set; } = "";
    public int TradeCode { get; set; }
    public TradeType Type { get; set; } = TradeType.Trade;
    public TradeStatus Status { get; set; } = TradeStatus.Waiting;
    public string? AssignedBot { get; set; }
    public DateTime QueueTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public enum TradeStatus
{
    Waiting,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum TradeType
{
    Trade,
    Clone,
    Dump,
    Batch,
    MysteryEgg,
    SeedCheck
}

public enum QueueResult
{
    Success,
    QueueFull,
    AlreadyInQueue,
    InvalidPokemon,
    NotAllowed
}
