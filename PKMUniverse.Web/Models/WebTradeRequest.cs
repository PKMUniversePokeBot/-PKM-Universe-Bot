// PKM Universe Bot - Web Trade Request Model
// Written by PKM Universe - 2025

namespace PKMUniverse.Web.Models;

public class WebTradeRequest
{
    public ulong UserId { get; set; }
    public string TrainerName { get; set; } = "";
    public int TradeCode { get; set; }
    public string? ShowdownSet { get; set; }
    public string? FileBase64 { get; set; }
    public string? Game { get; set; } = "LegendsZA";
}
