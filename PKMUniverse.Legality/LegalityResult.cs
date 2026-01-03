// PKM Universe Bot - Legality Result
// Written by PKM Universe - 2025

using PKHeX.Core;

namespace PKMUniverse.Legality;

public class LegalityResult
{
    public bool IsLegal { get; set; }
    public bool WasFixed { get; set; }
    public PKM? Pokemon { get; set; }
    public List<string> Issues { get; set; } = new();
    public string? EncounterType { get; set; }
    public int Generation { get; set; }

    public static LegalityResult Success(PKM pokemon)
    {
        return new LegalityResult
        {
            IsLegal = true,
            Pokemon = pokemon
        };
    }

    public static LegalityResult Failure(string reason)
    {
        return new LegalityResult
        {
            IsLegal = false,
            Issues = new List<string> { reason }
        };
    }

    public string GetSummary()
    {
        if (IsLegal)
        {
            if (WasFixed)
                return "Legal (auto-fixed)";
            return "Legal";
        }

        if (Issues.Count == 0)
            return "Illegal (unknown reason)";

        return $"Illegal: {string.Join("; ", Issues.Take(3))}";
    }

    public override string ToString()
    {
        var status = IsLegal ? "LEGAL" : "ILLEGAL";
        var fixedStr = WasFixed ? " (fixed)" : "";
        var issuesStr = Issues.Count > 0 ? $" - {string.Join(", ", Issues)}" : "";
        return $"[{status}{fixedStr}]{issuesStr}";
    }
}
