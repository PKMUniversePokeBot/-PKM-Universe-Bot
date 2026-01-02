// PKM Universe Bot - Web API Server
// Written by PKM Universe - 2025

using System.Net;
using System.Text;
using System.Text.Json;
using PKHeX.Core;
using PKMUniverse.Core.Logging;
using PKMUniverse.Trade.Executor;
using PKMUniverse.Trade.Queue;
using PKMUniverse.Web.Models;

namespace PKMUniverse.Web;

public class WebApiServer
{
    private readonly HttpListener _listener;
    private readonly TradeBotRunner _runner;
    private readonly int _port;
    private bool _running;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _running;
    public string Url => $"http://localhost:{_port}/";

    public event Action<string>? OnRequest;

    public WebApiServer(TradeBotRunner runner, int port = 8100)
    {
        _runner = runner;
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
    }

    public async Task StartAsync()
    {
        if (_running) return;

        try
        {
            _listener.Start();
            _running = true;
            _cts = new CancellationTokenSource();

            Logger.Info("WebAPI", $"Server started on port {_port}");

            _ = Task.Run(() => ListenAsync(_cts.Token));
        }
        catch (HttpListenerException ex)
        {
            Logger.Error("WebAPI", $"Failed to start: {ex.Message}");
            Logger.Info("WebAPI", "Try running as administrator or use: netsh http add urlacl url=http://+:8100/ user=Everyone");
        }
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
        _listener.Stop();
        Logger.Info("WebAPI", "Server stopped");
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (_running && !token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), token);
            }
            catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException)
            {
                if (_running)
                    Logger.Error("WebAPI", $"Listener error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // Enable CORS for website access
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            response.Close();
            return;
        }

        var path = request.Url?.AbsolutePath ?? "/";
        OnRequest?.Invoke($"{request.HttpMethod} {path}");

        try
        {
            var result = path.ToLower() switch
            {
                "/api/status" => HandleStatus(),
                "/api/queue" => HandleQueue(),
                "/api/queue/add" => await HandleAddToQueueAsync(request),
                "/api/queue/position" => HandlePosition(request),
                "/api/queue/cancel" => HandleCancel(request),
                "/api/bots" => HandleBots(),
                "/api/trade/showdown" => await HandleShowdownTradeAsync(request),
                "/api/trade/file" => await HandleFileTradeAsync(request),
                "/" => new ApiResponse { Success = true, Message = "PKM Universe Bot API v1.0" },
                _ => new ApiResponse { Success = false, Message = "Endpoint not found" }
            };

            await SendResponseAsync(response, result);
        }
        catch (Exception ex)
        {
            Logger.Error("WebAPI", $"Request error: {ex.Message}");
            await SendResponseAsync(response, new ApiResponse { Success = false, Message = ex.Message });
        }
    }

    private ApiResponse HandleStatus()
    {
        return new ApiResponse
        {
            Success = true,
            Data = new
            {
                Online = true,
                QueueSize = _runner.QueueSize,
                TotalBots = _runner.TotalBots,
                ActiveBots = _runner.ActiveBots,
                IsPaused = _runner.IsPaused
            }
        };
    }

    private ApiResponse HandleQueue()
    {
        return new ApiResponse
        {
            Success = true,
            Data = new
            {
                Count = _runner.QueueSize,
                IsPaused = _runner.IsPaused
            }
        };
    }

    private async Task<ApiResponse> HandleAddToQueueAsync(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
            return new ApiResponse { Success = false, Message = "POST required" };

        var body = await ReadBodyAsync(request);
        var tradeRequest = JsonSerializer.Deserialize<WebTradeRequest>(body);

        if (tradeRequest == null)
            return new ApiResponse { Success = false, Message = "Invalid request body" };

        // Parse Showdown set or decode file
        PKM? pokemon = null;

        if (!string.IsNullOrEmpty(tradeRequest.ShowdownSet))
        {
            pokemon = ParseShowdown(tradeRequest.ShowdownSet, tradeRequest.Game);
        }
        else if (!string.IsNullOrEmpty(tradeRequest.FileBase64))
        {
            pokemon = ParseFileData(tradeRequest.FileBase64);
        }

        if (pokemon == null)
            return new ApiResponse { Success = false, Message = "Could not parse Pokemon data" };

        var result = _runner.AddToQueue(
            tradeRequest.UserId,
            tradeRequest.TrainerName,
            pokemon,
            tradeRequest.TradeCode
        );

        return new ApiResponse
        {
            Success = result == QueueResult.Success,
            Message = result.ToString(),
            Data = new { Position = _runner.GetQueuePosition(tradeRequest.UserId) }
        };
    }

    private ApiResponse HandlePosition(HttpListenerRequest request)
    {
        var userIdStr = request.QueryString["userId"];
        if (!ulong.TryParse(userIdStr, out var userId))
            return new ApiResponse { Success = false, Message = "Invalid userId" };

        var position = _runner.GetQueuePosition(userId);
        return new ApiResponse
        {
            Success = true,
            Data = new { Position = position, InQueue = position > 0 }
        };
    }

    private ApiResponse HandleCancel(HttpListenerRequest request)
    {
        var userIdStr = request.QueryString["userId"];
        if (!ulong.TryParse(userIdStr, out var userId))
            return new ApiResponse { Success = false, Message = "Invalid userId" };

        var removed = _runner.RemoveFromQueue(userId);
        return new ApiResponse
        {
            Success = removed,
            Message = removed ? "Trade cancelled" : "Not in queue"
        };
    }

    private ApiResponse HandleBots()
    {
        var statuses = _runner.GetBotStatuses();
        return new ApiResponse
        {
            Success = true,
            Data = statuses.Select(b => new
            {
                b.Name,
                b.IsConnected,
                b.IsProcessing,
                b.TradeCount,
                b.CurrentTrainer,
                LastTrade = b.LastTrade?.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList()
        };
    }

    private async Task<ApiResponse> HandleShowdownTradeAsync(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
            return new ApiResponse { Success = false, Message = "POST required" };

        var body = await ReadBodyAsync(request);
        var tradeRequest = JsonSerializer.Deserialize<WebTradeRequest>(body);

        if (tradeRequest == null || string.IsNullOrEmpty(tradeRequest.ShowdownSet))
            return new ApiResponse { Success = false, Message = "Showdown set required" };

        var pokemon = ParseShowdown(tradeRequest.ShowdownSet, tradeRequest.Game);
        if (pokemon == null)
            return new ApiResponse { Success = false, Message = "Could not parse Showdown set" };

        var result = _runner.AddToQueue(
            tradeRequest.UserId,
            tradeRequest.TrainerName,
            pokemon,
            tradeRequest.TradeCode
        );

        return new ApiResponse
        {
            Success = result == QueueResult.Success,
            Message = result.ToString(),
            Data = new
            {
                Position = _runner.GetQueuePosition(tradeRequest.UserId),
                Pokemon = GetPokemonName(pokemon),
                Species = pokemon.Species,
                IsShiny = pokemon.IsShiny
            }
        };
    }

    private async Task<ApiResponse> HandleFileTradeAsync(HttpListenerRequest request)
    {
        if (request.HttpMethod != "POST")
            return new ApiResponse { Success = false, Message = "POST required" };

        var body = await ReadBodyAsync(request);
        var tradeRequest = JsonSerializer.Deserialize<WebTradeRequest>(body);

        if (tradeRequest == null || string.IsNullOrEmpty(tradeRequest.FileBase64))
            return new ApiResponse { Success = false, Message = "File data required" };

        var pokemon = ParseFileData(tradeRequest.FileBase64);
        if (pokemon == null)
            return new ApiResponse { Success = false, Message = "Could not parse Pokemon file" };

        var result = _runner.AddToQueue(
            tradeRequest.UserId,
            tradeRequest.TrainerName,
            pokemon,
            tradeRequest.TradeCode
        );

        return new ApiResponse
        {
            Success = result == QueueResult.Success,
            Message = result.ToString(),
            Data = new
            {
                Position = _runner.GetQueuePosition(tradeRequest.UserId),
                Pokemon = GetPokemonName(pokemon),
                Species = pokemon.Species,
                IsShiny = pokemon.IsShiny
            }
        };
    }

    private PKM? ParseShowdown(string showdownSet, string? game)
    {
        try
        {
            var set = new ShowdownSet(showdownSet);

            // Determine generation based on game
            var gen = game?.ToLower() switch
            {
                "legendsza" or "lza" or "za" => 9,
                "scarletviolet" or "sv" => 9,
                "legendsarceus" or "pla" => 8,
                "brilliantdiamond" or "shiningpearl" or "bdsp" => 8,
                "swordshield" or "swsh" => 8,
                _ => 9
            };

            // Create appropriate PKM type
            PKM pokemon = gen switch
            {
                9 => new PK9(),
                8 => new PK8(),
                _ => new PK9()
            };

            // Apply ShowdownSet data directly
            pokemon.Species = set.Species;
            pokemon.Form = set.Form;
            pokemon.Gender = (byte)set.Gender;
            pokemon.Nature = set.Nature;
            pokemon.Ability = set.Ability;
            pokemon.CurrentLevel = set.Level;

            if (set.Shiny)
                pokemon.SetShiny();

            // Set IVs
            var ivs = set.IVs;
            pokemon.IV_HP = ivs[0];
            pokemon.IV_ATK = ivs[1];
            pokemon.IV_DEF = ivs[2];
            pokemon.IV_SPA = ivs[3];
            pokemon.IV_SPD = ivs[4];
            pokemon.IV_SPE = ivs[5];

            // Set EVs
            var evs = set.EVs;
            pokemon.EV_HP = evs[0];
            pokemon.EV_ATK = evs[1];
            pokemon.EV_DEF = evs[2];
            pokemon.EV_SPA = evs[3];
            pokemon.EV_SPD = evs[4];
            pokemon.EV_SPE = evs[5];

            // Set moves
            var moves = set.Moves;
            if (moves.Length > 0) pokemon.Move1 = (ushort)moves[0];
            if (moves.Length > 1) pokemon.Move2 = (ushort)moves[1];
            if (moves.Length > 2) pokemon.Move3 = (ushort)moves[2];
            if (moves.Length > 3) pokemon.Move4 = (ushort)moves[3];

            return pokemon;
        }
        catch (Exception ex)
        {
            Logger.Error("WebAPI", $"Showdown parse error: {ex.Message}");
            return null;
        }
    }

    private PKM? ParseFileData(string base64Data)
    {
        try
        {
            var data = Convert.FromBase64String(base64Data);

            // Detect file type by size and first byte
            if (data.Length == 344)
            {
                // Could be PK9 or PB8 - check version byte
                if (data[0xDE] >= 47 && data[0xDE] <= 48) // BDSP versions
                    return new PB8(data);
                return new PK9(data);
            }

            return data.Length switch
            {
                328 => new PA8(data),  // Legends Arceus
                260 => new PK8(data),  // Gen 8
                232 => new PK7(data),  // Gen 7
                _ => EntityFormat.GetFromBytes(data)
            };
        }
        catch (Exception ex)
        {
            Logger.Error("WebAPI", $"File parse error: {ex.Message}");
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

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task SendResponseAsync(HttpListenerResponse response, ApiResponse result)
    {
        response.ContentType = "application/json";
        response.StatusCode = result.Success ? 200 : 400;

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
}
