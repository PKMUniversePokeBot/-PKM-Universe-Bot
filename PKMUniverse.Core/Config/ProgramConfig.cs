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
    // Trade Mode Settings
    public TradeSettings Trade { get; set; } = new();
    public CloneSettings Clone { get; set; } = new();
    public DumpSettings Dump { get; set; } = new();
    public BatchSettings Batch { get; set; } = new();
    public DistributionSettings Distribution { get; set; } = new();
    public SeedCheckSettings SeedCheck { get; set; } = new();
    public MysteryEggSettings MysteryEgg { get; set; } = new();

    // Queue Settings
    public QueueSettings Queue { get; set; } = new();

    // Timing Settings
    public TimingSettings Timing { get; set; } = new();

    // Counting/Stats Settings
    public CountSettings Counts { get; set; } = new();

    // Folder Settings
    public FolderSettings Folders { get; set; } = new();

    // Legality Settings
    public LegalityHubSettings Legality { get; set; } = new();

    // Stream Settings (Twitch/YouTube)
    public StreamSettings Stream { get; set; } = new();
}

public class TradeSettings
{
    public bool Enabled { get; set; } = true;
    public int TradeTimeout { get; set; } = 60;
    public int TradeCooldown { get; set; } = 2;
    public int MaxTradesPerUser { get; set; } = 5;
    public bool RequireLegalPokemon { get; set; } = true;
    public bool AutoCorrectShowdown { get; set; } = true;
    public bool AllowRequestedShiny { get; set; } = true;
    public bool AllowRequestedNickname { get; set; } = true;
    public bool AllowRequestedOT { get; set; } = false;
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 100;
    public bool UseTradePartnerInfo { get; set; } = true;
    public string DefaultOT { get; set; } = "PKM Universe";
    public int DefaultTID { get; set; } = 123456;
    public int DefaultSID { get; set; } = 1234;
}

public class CloneSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxClones { get; set; } = 6;
    public bool AllowIllegalClones { get; set; } = false;
    public bool RequireHeldItem { get; set; } = false;
    public bool CloneEntireBox { get; set; } = false;
}

public class DumpSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxDumpCount { get; set; } = 30;
    public bool DumpEntireBox { get; set; } = true;
    public bool SaveToFolder { get; set; } = true;
    public bool SendToDiscord { get; set; } = true;
    public string DumpFormat { get; set; } = "pk9";
}

public class BatchSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxBatchSize { get; set; } = 6;
    public bool AllowBatchClone { get; set; } = true;
    public bool AllowBatchTrade { get; set; } = true;
}

public class DistributionSettings
{
    public bool Enabled { get; set; } = false;
    public int DistributeWhileIdle { get; set; } = 0;
    public bool ShuffleDistribution { get; set; } = true;
    public bool ResetWhenEmpty { get; set; } = true;
    public string DistributionFolder { get; set; } = "distribution";
    public bool AllowRepeatTrades { get; set; } = false;
    public int RepeatCooldown { get; set; } = 300;
}

public class SeedCheckSettings
{
    public bool Enabled { get; set; } = true;
    public bool ShowAllZ3Results { get; set; } = false;
    public bool ShowNonShinyPokemon { get; set; } = true;
    public int MaxFramesToSearch { get; set; } = 10000;
    public bool CalculateShinyAdvances { get; set; } = true;
}

public class MysteryEggSettings
{
    public bool Enabled { get; set; } = true;
    public bool AllowShinyEggs { get; set; } = true;
    public bool RandomizeStats { get; set; } = true;
    public bool RandomizeAbility { get; set; } = true;
    public bool RandomizeBall { get; set; } = false;
    public int EggLevel { get; set; } = 1;
    public string EggFolder { get; set; } = "eggs";
}

public class QueueSettings
{
    public int MaxQueueSize { get; set; } = 50;
    public int MaxUsersInQueue { get; set; } = 25;
    public int MaxTradesPerUserInQueue { get; set; } = 1;
    public bool FlexMode { get; set; } = false;
    public int FlexModeThreshold { get; set; } = 3;
    public bool AllowPriority { get; set; } = true;
    public bool ClearOnRestart { get; set; } = false;
    public QueueMode Mode { get; set; } = QueueMode.FlexTrade;
    public bool CanQueue { get; set; } = true;
    public bool EmitQueueUpdates { get; set; } = true;
}

public enum QueueMode
{
    FlexTrade,
    FixedTrade,
    Clone,
    Dump,
    Distribution,
    SeedCheck,
    MysteryEgg
}

public class TimingSettings
{
    // Connection Timing
    public int ConnectionRetryDelay { get; set; } = 5000;
    public int MaxConnectionAttempts { get; set; } = 5;

    // Trade Timing
    public int ExtraTimeOpenBox { get; set; } = 0;
    public int ExtraTimeWaitTradePartner { get; set; } = 0;
    public int ExtraTimeTradeConfirm { get; set; } = 0;
    public int ExtraTimeYCommOpen { get; set; } = 0;
    public int ExtraTimeJoinUnionRoom { get; set; } = 0;

    // Button Press Timing
    public int KeypressTime { get; set; } = 50;
    public int OpenHomeDelay { get; set; } = 2000;
    public int NavigateToHomeDelay { get; set; } = 1000;
    public int CloseGameDelay { get; set; } = 3000;
    public int RestartGameDelay { get; set; } = 8000;

    // Recovery Timing
    public int RecoveryCheckDelay { get; set; } = 500;
    public int FreezeRecoveryTime { get; set; } = 30000;
    public int IdleWaitTime { get; set; } = 1000;
}

public class CountSettings
{
    public int CompletedTrades { get; set; } = 0;
    public int CompletedClones { get; set; } = 0;
    public int CompletedDumps { get; set; } = 0;
    public int CompletedDistributions { get; set; } = 0;
    public int CompletedSeedChecks { get; set; } = 0;
    public int CompletedMysteryEggs { get; set; } = 0;
    public int FailedTrades { get; set; } = 0;
    public int TotalPokemonTraded { get; set; } = 0;
    public bool PersistCounts { get; set; } = true;
    public bool LogCounts { get; set; } = true;
}

public class FolderSettings
{
    public string TradeFolder { get; set; } = "trades";
    public string DumpFolder { get; set; } = "dumps";
    public string DistributionFolder { get; set; } = "distribution";
    public string EggFolder { get; set; } = "eggs";
    public string LogFolder { get; set; } = "logs";
    public string BackupFolder { get; set; } = "backups";
    public bool CreateFoldersIfMissing { get; set; } = true;
    public bool OrganizeByDate { get; set; } = true;
    public bool OrganizeByUser { get; set; } = false;
}

public class LegalityHubSettings
{
    public bool EnableLegalityCheck { get; set; } = true;
    public bool AllowIllegal { get; set; } = false;
    public bool AutoFix { get; set; } = true;
    public bool StrictMode { get; set; } = false;
    public bool AllowShiny { get; set; } = true;
    public bool AllowHiddenAbility { get; set; } = true;
    public bool AllowEggMoves { get; set; } = true;
    public bool AllowEventMoves { get; set; } = true;
    public bool AllowTransferMoves { get; set; } = true;
    public bool SetSuggestedMet { get; set; } = true;
    public bool SetSuggestedBall { get; set; } = true;
    public bool SetSuggestedAbility { get; set; } = false;
    public bool RerollPID { get; set; } = true;
    public bool RerollEncryption { get; set; } = true;
    public List<int> BannedSpecies { get; set; } = new();
    public List<int> BannedMoves { get; set; } = new();
    public List<int> BannedAbilities { get; set; } = new();
    public List<int> BannedItems { get; set; } = new();
}

public class StreamSettings
{
    // Twitch Settings
    public bool TwitchEnabled { get; set; } = false;
    public string TwitchUsername { get; set; } = "";
    public string TwitchOAuthToken { get; set; } = "";
    public string TwitchChannel { get; set; } = "";
    public string TwitchClientId { get; set; } = "";
    public string TwitchPrefix { get; set; } = "!";
    public bool TwitchSubOnly { get; set; } = false;
    public int TwitchCooldown { get; set; } = 30;
    public List<string> TwitchModerators { get; set; } = new();

    // YouTube Settings
    public bool YouTubeEnabled { get; set; } = false;
    public string YouTubeApiKey { get; set; } = "";
    public string YouTubeClientId { get; set; } = "";
    public string YouTubeClientSecret { get; set; } = "";
    public string YouTubeChannelId { get; set; } = "";
    public string YouTubeLiveChatId { get; set; } = "";
    public string YouTubePrefix { get; set; } = "!";
    public bool YouTubeMemberOnly { get; set; } = false;
    public int YouTubeCooldown { get; set; } = 30;
    public int YouTubePollingInterval { get; set; } = 3000;
    public List<string> YouTubeModerators { get; set; } = new();

    // Common Stream Settings
    public bool AnnounceTradeStart { get; set; } = true;
    public bool AnnounceTradeComplete { get; set; } = true;
    public bool ShowQueuePosition { get; set; } = true;
    public bool ShowTradeCode { get; set; } = true;
}

public class DiscordConfig
{
    public string Token { get; set; } = "";
    public string Prefix { get; set; } = "!";
    public List<ulong> OwnerIds { get; set; } = new();
    public List<ulong> SudoIds { get; set; } = new();
    public ulong TradeChannelId { get; set; }
    public ulong LogChannelId { get; set; }

    // Additional Discord Settings
    public bool UseEmbeds { get; set; } = true;
    public bool ShowQueuePosition { get; set; } = true;
    public bool ShowTradeCode { get; set; } = true;
    public bool DeleteTradeMessages { get; set; } = false;
    public int DeleteMessageDelay { get; set; } = 10;
    public bool AllowDMTrades { get; set; } = false;
    public bool RequireRole { get; set; } = false;
    public ulong RequiredRoleId { get; set; }
    public string BotStatus { get; set; } = "Trading Pokemon!";
    public int StatusUpdateInterval { get; set; } = 60;
}

public class WebApiConfig
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 8100;
    public string ApiKey { get; set; } = "";

    // Additional Web API Settings
    public bool RequireApiKey { get; set; } = true;
    public bool AllowCORS { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
    public bool EnableSwagger { get; set; } = false;
    public bool EnableWebhooks { get; set; } = true;
    public string WebhookUrl { get; set; } = "";
    public int RateLimitPerMinute { get; set; } = 60;
    public bool LogRequests { get; set; } = true;
}
