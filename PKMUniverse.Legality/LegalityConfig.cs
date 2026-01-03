// PKM Universe Bot - Legality Configuration
// Written by PKM Universe - 2025

namespace PKMUniverse.Legality;

public class LegalityConfig
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
    public int MaxLevel { get; set; } = 100;
    public int MinLevel { get; set; } = 1;
    public List<int> BannedSpecies { get; set; } = new();
    public List<int> BannedMoves { get; set; } = new();
    public List<int> BannedAbilities { get; set; } = new();
    public List<int> BannedItems { get; set; } = new();
}
