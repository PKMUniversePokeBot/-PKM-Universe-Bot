// PKM Universe Bot - Twitch Configuration
// Written by PKM Universe - 2025

namespace PKMUniverse.Twitch;

public class TwitchConfig
{
    public string Username { get; set; } = "";
    public string OAuthToken { get; set; } = "";
    public string Channel { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string CommandPrefix { get; set; } = "!";
    public bool EnableTrading { get; set; } = true;
    public bool SubscriberOnly { get; set; } = false;
    public int CooldownSeconds { get; set; } = 30;
    public List<string> ModeratorUsernames { get; set; } = new();
}
