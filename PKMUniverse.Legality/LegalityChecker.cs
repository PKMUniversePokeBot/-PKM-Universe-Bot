// PKM Universe Bot - Legality Checker
// Written by PKM Universe - 2025

using PKHeX.Core;
using PKMUniverse.Core.Logging;

namespace PKMUniverse.Legality;

public class LegalityChecker
{
    private readonly LegalityConfig _config;

    public LegalityChecker(LegalityConfig config)
    {
        _config = config;
    }

    public LegalityResult Check(PKM pokemon)
    {
        if (!_config.EnableLegalityCheck)
            return LegalityResult.Success(pokemon);

        var result = new LegalityResult { Pokemon = pokemon };

        // Basic validation
        if (!ValidateBasics(pokemon, result))
            return result;

        // Species check
        if (!ValidateSpecies(pokemon, result))
            return result;

        // Move validation
        if (!ValidateMoves(pokemon, result))
            return result;

        // Ability validation
        if (!ValidateAbility(pokemon, result))
            return result;

        // Item validation
        if (!ValidateItem(pokemon, result))
            return result;

        // Level validation
        if (!ValidateLevel(pokemon, result))
            return result;

        // PKHeX legality analysis
        if (!PerformLegalityAnalysis(pokemon, result))
        {
            if (_config.AutoFix && !_config.AllowIllegal)
            {
                var fixedPokemon = TryAutoFix(pokemon);
                if (fixedPokemon != null)
                {
                    result.Pokemon = fixedPokemon;
                    result.WasFixed = true;
                    result.IsLegal = true;
                    result.Issues.Add("Pokemon was auto-fixed to be legal");
                    return result;
                }
            }

            if (!_config.AllowIllegal)
                return result;
        }

        result.IsLegal = true;
        return result;
    }

    private bool ValidateBasics(PKM pokemon, LegalityResult result)
    {
        if (pokemon.Species == 0)
        {
            result.Issues.Add("Invalid species (0)");
            return false;
        }

        if (pokemon.Species > (int)Species.MAX_COUNT)
        {
            result.Issues.Add($"Species ID {pokemon.Species} exceeds maximum");
            return false;
        }

        return true;
    }

    private bool ValidateSpecies(PKM pokemon, LegalityResult result)
    {
        if (_config.BannedSpecies.Contains(pokemon.Species))
        {
            result.Issues.Add($"Species {pokemon.Species} is banned");
            return false;
        }

        return true;
    }

    private bool ValidateMoves(PKM pokemon, LegalityResult result)
    {
        var moves = new[] { (int)pokemon.Move1, (int)pokemon.Move2, (int)pokemon.Move3, (int)pokemon.Move4 };

        foreach (var move in moves)
        {
            if (move == 0) continue;

            if (_config.BannedMoves.Contains(move))
            {
                result.Issues.Add($"Move {move} is banned");
                return false;
            }
        }

        // Check for duplicate moves
        var nonZeroMoves = moves.Where(m => m != 0).ToArray();
        if (nonZeroMoves.Length != nonZeroMoves.Distinct().Count())
        {
            result.Issues.Add("Duplicate moves detected");
            return false;
        }

        return true;
    }

    private bool ValidateAbility(PKM pokemon, LegalityResult result)
    {
        if (_config.BannedAbilities.Contains(pokemon.Ability))
        {
            result.Issues.Add($"Ability {pokemon.Ability} is banned");
            return false;
        }

        // Check hidden ability if restricted
        if (!_config.AllowHiddenAbility)
        {
            var pi = pokemon.PersonalInfo;
            if (pi.AbilityCount > 2 && pokemon.AbilityNumber == 4)
            {
                result.Issues.Add("Hidden abilities are not allowed");
                return false;
            }
        }

        return true;
    }

    private bool ValidateItem(PKM pokemon, LegalityResult result)
    {
        if (pokemon.HeldItem == 0)
            return true;

        if (_config.BannedItems.Contains(pokemon.HeldItem))
        {
            result.Issues.Add($"Item {pokemon.HeldItem} is banned");
            return false;
        }

        return true;
    }

    private bool ValidateLevel(PKM pokemon, LegalityResult result)
    {
        if (pokemon.CurrentLevel < _config.MinLevel)
        {
            result.Issues.Add($"Level {pokemon.CurrentLevel} is below minimum {_config.MinLevel}");
            return false;
        }

        if (pokemon.CurrentLevel > _config.MaxLevel)
        {
            result.Issues.Add($"Level {pokemon.CurrentLevel} exceeds maximum {_config.MaxLevel}");
            return false;
        }

        return true;
    }

    private bool PerformLegalityAnalysis(PKM pokemon, LegalityResult result)
    {
        try
        {
            var la = new LegalityAnalysis(pokemon);

            if (la.Valid)
                return true;

            result.IsLegal = false;

            // Collect all legality issues
            foreach (var check in la.Results)
            {
                if (!check.Valid)
                {
                    result.Issues.Add($"{check.Identifier}: {check.Comment}");
                }
            }

            // Add parsed info
            result.EncounterType = la.EncounterOriginal?.GetType().Name ?? "Unknown";
            result.Generation = la.Info.Generation;

            Logger.Debug("Legality", $"Pokemon {pokemon.Species} failed legality: {string.Join(", ", result.Issues)}");

            return false;
        }
        catch (Exception ex)
        {
            Logger.Error("Legality", $"Legality analysis error: {ex.Message}");
            result.Issues.Add($"Analysis error: {ex.Message}");
            return !_config.StrictMode;
        }
    }

    private PKM? TryAutoFix(PKM pokemon)
    {
        try
        {
            // Create a copy for fixing
            var clone = pokemon.Clone();

            // Fix common issues
            FixOT(clone);
            FixMet(clone);
            FixMoves(clone);
            FixAbility(clone);
            FixIVsEVs(clone);
            FixRibbons(clone);

            // Check if fixed
            var la = new LegalityAnalysis(clone);
            if (la.Valid)
            {
                Logger.Info("Legality", $"Auto-fixed Pokemon {clone.Species}");
                return clone;
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.Error("Legality", $"Auto-fix error: {ex.Message}");
            return null;
        }
    }

    private void FixOT(PKM pokemon)
    {
        // Ensure OT is set
        if (string.IsNullOrEmpty(pokemon.OriginalTrainerName))
        {
            pokemon.OriginalTrainerName = "PKM";
            pokemon.OriginalTrainerGender = 0;
        }

        // Ensure TID/SID are set
        if (pokemon.TID16 == 0)
            pokemon.TID16 = (ushort)Random.Shared.Next(1, 65535);
    }

    private void FixMet(PKM pokemon)
    {
        // Ensure met date is set
        if (pokemon.MetDate == null)
            pokemon.MetDate = DateOnly.FromDateTime(DateTime.Now);

        // Ensure met level is valid
        if (pokemon.MetLevel > pokemon.CurrentLevel)
            pokemon.MetLevel = (byte)pokemon.CurrentLevel;

        if (pokemon.MetLevel == 0)
            pokemon.MetLevel = 1;
    }

    private void FixMoves(PKM pokemon)
    {
        // Reset PP
        pokemon.HealPP();

        // Fix move PP ups
        pokemon.Move1_PPUps = 3;
        pokemon.Move2_PPUps = 3;
        pokemon.Move3_PPUps = 3;
        pokemon.Move4_PPUps = 3;
    }

    private void FixAbility(PKM pokemon)
    {
        // Ensure ability is valid for species
        var pi = pokemon.PersonalInfo;
        var abilityIndex = pokemon.AbilityNumber >> 1;

        if (abilityIndex >= pi.AbilityCount)
        {
            pokemon.AbilityNumber = 1;
            pokemon.RefreshAbility(0);
        }
    }

    private void FixIVsEVs(PKM pokemon)
    {
        // Ensure EVs don't exceed limit
        var totalEVs = pokemon.EV_HP + pokemon.EV_ATK + pokemon.EV_DEF +
                       pokemon.EV_SPA + pokemon.EV_SPD + pokemon.EV_SPE;

        if (totalEVs > 510)
        {
            // Reset to max legal distribution
            pokemon.EV_HP = 0;
            pokemon.EV_ATK = 252;
            pokemon.EV_DEF = 0;
            pokemon.EV_SPA = 0;
            pokemon.EV_SPD = 4;
            pokemon.EV_SPE = 252;
        }
    }

    private void FixRibbons(PKM pokemon)
    {
        // Clear ribbons that might cause issues
        if (pokemon is IRibbonSetCommon8 ribbon8)
        {
            // Keep only basic ribbons
        }
    }

    public string GetLegalityReport(PKM pokemon)
    {
        var la = new LegalityAnalysis(pokemon);
        return la.Report();
    }

    public bool IsLegal(PKM pokemon)
    {
        var la = new LegalityAnalysis(pokemon);
        return la.Valid;
    }

    public int GetGeneration(PKM pokemon)
    {
        var la = new LegalityAnalysis(pokemon);
        return la.Info.Generation;
    }

    public string? GetEncounterType(PKM pokemon)
    {
        var la = new LegalityAnalysis(pokemon);
        return la.EncounterOriginal?.GetType().Name;
    }
}
