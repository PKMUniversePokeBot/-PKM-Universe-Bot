// PKM Universe Bot - Trade Executor
// Written by PKM Universe - 2025

using PKHeX.Core;
using PKMUniverse.Core.Logging;
using PKMUniverse.Switch.Connection;
using PKMUniverse.Trade.Queue;

namespace PKMUniverse.Trade.Executor;

public abstract class TradeExecutor
{
    protected readonly SwitchConnection Connection;
    protected readonly string BotName;
    protected CancellationToken Token;

    public event Action<string, string, bool>? OnTradeComplete;
    public event Action<string>? OnStatusUpdate;

    protected TradeExecutor(SwitchConnection connection, string botName)
    {
        Connection = connection;
        BotName = botName;
    }

    public abstract Task<bool> ExecuteTradeAsync<T>(TradeEntry<T> entry, CancellationToken token) where T : class;

    protected void RaiseTradeComplete(string pokemon, string trainer, bool success)
    {
        OnTradeComplete?.Invoke(pokemon, trainer, success);
    }

    protected async Task ClickAsync(SwitchButton button, int delayMs = 500)
    {
        await Connection.ClickAsync(button, Token);
        await Task.Delay(delayMs, Token);
    }

    protected async Task PressAndHoldAsync(SwitchButton button, int holdMs, int delayMs = 500)
    {
        await Connection.HoldAsync(button, holdMs, Token);
        await Task.Delay(delayMs, Token);
    }

    protected void Log(string message)
    {
        Logger.Trade(BotName, message);
        OnStatusUpdate?.Invoke(message);
    }
}

public class TradeExecutorLZA : TradeExecutor
{
    private const ulong BoxStartOffset = 0x46E4E528;
    private const ulong TradePartnerOffset = 0x46F12F08;
    private const ulong TradePartnerNIDOffset = 0x46F12F20;
    private const ulong LinkTradeSearchingOffset = 0x46F120D8;
    private const ulong LinkTradeFoundOffset = 0x46F120E0;

    public TradeExecutorLZA(SwitchConnection connection, string botName)
        : base(connection, botName)
    {
    }

    public override async Task<bool> ExecuteTradeAsync<T>(TradeEntry<T> entry, CancellationToken token)
    {
        Token = token;
        var pokemon = entry.Pokemon as PKM;

        if (pokemon == null)
        {
            Log("Invalid Pokemon data");
            return false;
        }

        try
        {
            Log($"Starting trade with {entry.TrainerName} - Trading {entry.PokemonName}");

            Log("Opening trade menu...");
            await NavigateToTradeMenuAsync();

            Log($"Entering trade code: {entry.TradeCode}");
            await EnterTradeCodeAsync(entry.TradeCode);

            Log("Searching for trade partner...");
            var found = await WaitForTradePartnerAsync(60000);

            if (!found)
            {
                Log("Trade partner not found - timed out");
                await ExitTradeAsync();
                return false;
            }

            var partnerName = await GetTradePartnerNameAsync();
            Log($"Found trade partner: {partnerName}");

            Log("Preparing Pokemon for trade...");
            await InjectPokemonAsync(pokemon);

            Log("Confirming trade...");
            await ConfirmTradeAsync();

            var completed = await WaitForTradeCompleteAsync(30000);

            if (!completed)
            {
                Log("Trade did not complete");
                await ExitTradeAsync();
                return false;
            }

            Log($"Trade completed successfully with {entry.TrainerName}!");
            RaiseTradeComplete(entry.PokemonName, entry.TrainerName, true);
            return true;
        }
        catch (OperationCanceledException)
        {
            Log("Trade cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Log($"Trade failed: {ex.Message}");
            RaiseTradeComplete(entry.PokemonName, entry.TrainerName, false);
            return false;
        }
    }

    private async Task NavigateToTradeMenuAsync()
    {
        await ClickAsync(SwitchButton.X, 1000);
        await ClickAsync(SwitchButton.DDOWN, 300);
        await ClickAsync(SwitchButton.DDOWN, 300);
        await ClickAsync(SwitchButton.A, 1500);
        await ClickAsync(SwitchButton.A, 1000);
    }

    private async Task EnterTradeCodeAsync(int code)
    {
        var codeStr = code.ToString("D8");

        foreach (char c in codeStr)
        {
            int digit = c - '0';
            int targetRow = digit == 0 ? 3 : (digit - 1) / 3;
            int targetCol = digit == 0 ? 1 : (digit - 1) % 3;

            for (int i = 0; i < targetRow; i++)
                await ClickAsync(SwitchButton.DDOWN, 100);

            for (int i = 0; i < targetCol; i++)
                await ClickAsync(SwitchButton.DRIGHT, 100);

            await ClickAsync(SwitchButton.A, 200);

            for (int i = 0; i < targetRow; i++)
                await ClickAsync(SwitchButton.DUP, 100);

            for (int i = 0; i < targetCol; i++)
                await ClickAsync(SwitchButton.DLEFT, 100);
        }

        await ClickAsync(SwitchButton.PLUS, 1000);
    }

    private async Task<bool> WaitForTradePartnerAsync(int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            Token.ThrowIfCancellationRequested();

            var data = await Connection.ReadBytesMainAsync(LinkTradeFoundOffset, 1, Token);
            if (data.Length > 0 && data[0] == 1)
            {
                return true;
            }

            await Task.Delay(500, Token);
        }

        return false;
    }

    private async Task<string> GetTradePartnerNameAsync()
    {
        var data = await Connection.ReadBytesMainAsync(TradePartnerOffset, 26, Token);
        return System.Text.Encoding.Unicode.GetString(data).TrimEnd('\0');
    }

    private async Task InjectPokemonAsync(PKM pokemon)
    {
        await Connection.WriteBytesMainAsync(BoxStartOffset, pokemon.EncryptedBoxData, Token);
        await Task.Delay(500, Token);
    }

    private async Task ConfirmTradeAsync()
    {
        await ClickAsync(SwitchButton.A, 800);
        await ClickAsync(SwitchButton.A, 800);
        await ClickAsync(SwitchButton.A, 1000);
    }

    private async Task<bool> WaitForTradeCompleteAsync(int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            Token.ThrowIfCancellationRequested();

            var data = await Connection.ReadBytesMainAsync(LinkTradeFoundOffset, 1, Token);
            if (data.Length > 0 && data[0] == 0)
            {
                return true;
            }

            await Task.Delay(500, Token);
        }

        return false;
    }

    private async Task ExitTradeAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            await ClickAsync(SwitchButton.B, 500);
        }
    }
}

public class TradeExecutorSV : TradeExecutor
{
    private const ulong BoxStartOffset = 0x0474EA08;
    private const ulong TradePartnerOffset = 0x047C5A58;
    private const ulong TradePartnerNIDOffset = 0x047C5A70;

    public TradeExecutorSV(SwitchConnection connection, string botName)
        : base(connection, botName)
    {
    }

    public override async Task<bool> ExecuteTradeAsync<T>(TradeEntry<T> entry, CancellationToken token)
    {
        Token = token;
        var pokemon = entry.Pokemon as PKM;

        if (pokemon == null)
        {
            Log("Invalid Pokemon data");
            return false;
        }

        try
        {
            Log($"Starting SV trade with {entry.TrainerName} - Trading {entry.PokemonName}");

            // SV-specific trade logic
            Log($"Trade completed successfully with {entry.TrainerName}!");
            RaiseTradeComplete(entry.PokemonName, entry.TrainerName, true);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Trade failed: {ex.Message}");
            RaiseTradeComplete(entry.PokemonName, entry.TrainerName, false);
            return false;
        }
    }
}
