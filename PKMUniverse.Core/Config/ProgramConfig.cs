// PKM Universe Bot - Program Configuration
// Written by PKM Universe - 2025

using System.Text.Json;

namespace PKMUniverse.Core.Config;

public class ProgramConfig
{
    public List<SwitchBotConfig> Bots { get; set; } = new();
    public HubConfig Hub { get; set; } = new();
    public DiscordConfig Discord { get; set; } = new();
    public WebApiConfig WebApi { get; set; } = new();

    private const string ConfigFile = "config.json";

    public static ProgramConfig Load()
    {
        if (!File.Exists(ConfigFile))
            return new ProgramConfig();

        var json = File.ReadAllText(ConfigFile);
        return JsonSerializer.Deserialize<ProgramConfig>(json) ?? new ProgramConfig();
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(ConfigFile, json);
    }
}

public class HubConfig
{
    public int MaxQueueSize { get; set; } = 50;
    public bool AllowClone { get; set; } = true;
    public bool AllowDump { get; set; } = true;
    public bool AllowBatch { get; set; } = true;
    public bool AllowMysteryEgg { get; set; } = true;
    public bool AllowSeedCheck { get; set; } = true;
    public int TradeTimeout { get; set; } = 60;
    public int TradeCooldown { get; set; } = 2;
}

public class DiscordConfig
{
    public string Token { get; set; } = "";
    public string Prefix { get; set; } = "!";
    public List<ulong> OwnerIds { get; set; } = new();
    public List<ulong> SudoIds { get; set; } = new();
    public ulong TradeChannelId { get; set; }
    public ulong LogChannelId { get; set; }
}

public class WebApiConfig
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 8100;
    public string ApiKey { get; set; } = "";
}
