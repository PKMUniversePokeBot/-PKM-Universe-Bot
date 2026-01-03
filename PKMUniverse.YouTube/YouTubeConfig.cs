// PKM Universe Bot - YouTube Configuration
// Written by PKM Universe - 2025

namespace PKMUniverse.YouTube;

public class YouTubeConfig
{
    public string ApiKey { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public string LiveChatId { get; set; } = "";
    public string CommandPrefix { get; set; } = "!";
    public bool EnableTrading { get; set; } = true;
    public bool MemberOnly { get; set; } = false;
    public int CooldownSeconds { get; set; } = 30;
    public int PollingIntervalMs { get; set; } = 3000;
    public List<string> ModeratorChannelIds { get; set; } = new();
}
