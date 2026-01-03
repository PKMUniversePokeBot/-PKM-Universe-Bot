// PKM Universe Bot - Auto Legality Mod
// Written by PKM Universe - 2025

using PKHeX.Core;
using PKMUniverse.Core.Logging;

namespace PKMUniverse.Legality;

public class AutoLegalityMod
{
    private readonly LegalityConfig _config;

    public AutoLegalityMod(LegalityConfig config)
    {
        _config = config;
    }

    public PKM? MakeLegal(PKM pokemon, ITrainerInfo trainer)
    {
        try
        {
            // First check if already legal
            var la = new LegalityAnalysis(pokemon);
            if (la.Valid)
                return pokemon;

            Logger.Info("AutoMod", $"Attempting to legalize {pokemon.Species}");

            // Try to make a legal version
            var clone = pokemon.Clone();

            // Apply trainer info
            ApplyTrainerInfo(clone, trainer);

            // Fix common issues
            FixEncounter(clone);
            FixStats(clone);
            FixMoves(clone);
            FixAbility(clone);
            FixBall(clone);
            FixLanguage(clone);
            FixHandler(clone);
            FixMemories(clone);

            // Check result
            la = new LegalityAnalysis(clone);
            if (la.Valid)
            {
                Logger.Info("AutoMod", $"Successfully legalized {clone.Species}");
                return clone;
            }

            // Try regenerating from scratch
            var regenerated = RegeneratePokemon(pokemon, trainer);
            if (regenerated != null)
            {
                la = new LegalityAnalysis(regenerated);
                if (la.Valid)
                {
                    Logger.Info("AutoMod", $"Regenerated legal {regenerated.Species}");
                    return regenerated;
                }
            }

            Logger.Warning("AutoMod", $"Could not legalize {pokemon.Species}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error("AutoMod", $"Legalization error: {ex.Message}");
            return null;
        }
    }

    private void ApplyTrainerInfo(PKM pokemon, ITrainerInfo trainer)
    {
        pokemon.OriginalTrainerName = trainer.OT;
        pokemon.TID16 = trainer.TID16;
        pokemon.SID16 = trainer.SID16;
        pokemon.OriginalTrainerGender = (byte)trainer.Gender;
        pokemon.Language = trainer.Language;
        pokemon.Version = trainer.Version;
    }

    private void FixEncounter(PKM pokemon)
    {
        // Ensure met conditions are valid
        if (pokemon.MetDate == null)
            pokemon.MetDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-Random.Shared.Next(1, 365)));

        // Fix met location if needed
        if (pokemon.MetLocation == 0)
            pokemon.MetLocation = GetDefaultMetLocation(pokemon);

        // Fix egg met conditions
        if (pokemon.WasEgg)
        {
            if (pokemon.EggMetDate == null)
                pokemon.EggMetDate = pokemon.MetDate?.AddDays(-1);

            if (pokemon.EggLocation == 0)
                pokemon.EggLocation = GetDefaultEggLocation(pokemon);
        }
    }

    private void FixStats(PKM pokemon)
    {
        // Ensure stats are calculated
        pokemon.RefreshChecksum();

        // Fix height/weight for Gen8+
        if (pokemon is IScaledSize scaled)
        {
            if (scaled.HeightScalar == 0)
                scaled.HeightScalar = (byte)Random.Shared.Next(1, 255);
            if (scaled.WeightScalar == 0)
                scaled.WeightScalar = (byte)Random.Shared.Next(1, 255);
        }

        // Fix EVs if over limit
        var totalEVs = pokemon.EV_HP + pokemon.EV_ATK + pokemon.EV_DEF +
                       pokemon.EV_SPA + pokemon.EV_SPD + pokemon.EV_SPE;
        if (totalEVs > 510)
        {
            ScaleEVs(pokemon, 510);
        }
    }

    private void ScaleEVs(PKM pokemon, int maxTotal)
    {
        var total = pokemon.EV_HP + pokemon.EV_ATK + pokemon.EV_DEF +
                    pokemon.EV_SPA + pokemon.EV_SPD + pokemon.EV_SPE;

        if (total <= maxTotal) return;

        var ratio = (double)maxTotal / total;
        pokemon.EV_HP = (int)(pokemon.EV_HP * ratio);
        pokemon.EV_ATK = (int)(pokemon.EV_ATK * ratio);
        pokemon.EV_DEF = (int)(pokemon.EV_DEF * ratio);
        pokemon.EV_SPA = (int)(pokemon.EV_SPA * ratio);
        pokemon.EV_SPD = (int)(pokemon.EV_SPD * ratio);
        pokemon.EV_SPE = (int)(pokemon.EV_SPE * ratio);
    }

    private void FixMoves(PKM pokemon)
    {
        // Heal PP
        pokemon.HealPP();

        // Fix PP ups
        pokemon.Move1_PPUps = 3;
        pokemon.Move2_PPUps = 3;
        pokemon.Move3_PPUps = 3;
        pokemon.Move4_PPUps = 3;

        // Ensure at least one move (Struggle)
        if (pokemon.Move1 == 0 && pokemon.Move2 == 0 && pokemon.Move3 == 0 && pokemon.Move4 == 0)
        {
            pokemon.Move1 = 165; // Struggle
        }
    }

    private void FixAbility(PKM pokemon)
    {
        var pi = pokemon.PersonalInfo;

        // Check if current ability is valid
        var abilityNum = pokemon.AbilityNumber;
        var abilityIndex = abilityNum >> 1;

        if (abilityIndex >= pi.AbilityCount)
        {
            // Reset to first ability
            pokemon.AbilityNumber = 1;
            pokemon.RefreshAbility(0);
        }

        // Fix hidden ability flag
        if (!_config.AllowHiddenAbility && pokemon.AbilityNumber == 4)
        {
            pokemon.AbilityNumber = 1;
            pokemon.RefreshAbility(0);
        }
    }

    private void FixBall(PKM pokemon)
    {
        // Ensure ball is valid for species/form
        var ball = pokemon.Ball;
        if (!IsValidBall(pokemon, ball))
        {
            pokemon.Ball = (byte)Ball.Poke; // Default to Poke Ball
        }
    }

    private bool IsValidBall(PKM pokemon, int ball)
    {
        // Basic validation - most Pokemon can be in most balls
        if (ball < 1 || ball > 26) return false;

        // Special cases
        if (pokemon.Species == (int)Species.Shedinja && ball != (int)Ball.Poke)
            return false;

        return true;
    }

    private void FixLanguage(PKM pokemon)
    {
        if (pokemon.Language == 0)
            pokemon.Language = (int)LanguageID.English;
    }

    private void FixHandler(PKM pokemon)
    {
        if (pokemon is IHandlerLanguage hl && hl.HandlingTrainerLanguage == 0)
            hl.HandlingTrainerLanguage = (byte)LanguageID.English;
    }

    private void FixMemories(PKM pokemon)
    {
        if (pokemon is IMemoryOT memOT)
        {
            if (memOT.OriginalTrainerMemory == 0)
            {
                memOT.OriginalTrainerMemory = 1;
                memOT.OriginalTrainerMemoryIntensity = 1;
                memOT.OriginalTrainerMemoryFeeling = 1;
            }
        }
    }

    private ushort GetDefaultMetLocation(PKM pokemon)
    {
        // Return common met locations based on game
        return pokemon.Version switch
        {
            GameVersion.SL or GameVersion.VL => 6, // Poco Path
            GameVersion.SW or GameVersion.SH => 12, // Route 1
            GameVersion.BD or GameVersion.SP => 200, // Twinleaf Town
            GameVersion.PLA => 6, // Obsidian Fieldlands
            _ => 1
        };
    }

    private ushort GetDefaultEggLocation(PKM pokemon)
    {
        return pokemon.Version switch
        {
            GameVersion.SL or GameVersion.VL => 50, // Picnic
            GameVersion.SW or GameVersion.SH => 60002, // Daycare
            GameVersion.BD or GameVersion.SP => 60002, // Daycare
            _ => 60002
        };
    }

    private PKM? RegeneratePokemon(PKM original, ITrainerInfo trainer)
    {
        try
        {
            // Create a fresh copy and try to fix it with a different approach
            var clone = original.Clone();

            // Apply trainer info
            ApplyTrainerInfo(clone, trainer);

            // Set a valid met location for current generation
            clone.MetLocation = GetDefaultMetLocation(clone);
            clone.MetDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-Random.Shared.Next(1, 365)));

            var metLevel = Math.Max(1, Math.Min((int)clone.CurrentLevel, 100));
            clone.MetLevel = (byte)metLevel;

            // Clear potentially problematic data
            clone.FatefulEncounter = false;

            // Ensure valid ball
            if (clone.Ball == 0 || clone.Ball > 26)
                clone.Ball = (byte)Ball.Poke;

            // Ensure valid nature
            var natureValue = (int)clone.Nature;
            if (natureValue > 24 || natureValue < 0)
                clone.Nature = Nature.Hardy;

            // Refresh everything
            clone.RefreshChecksum();

            // Check if this fixed it
            var la = new LegalityAnalysis(clone);
            if (la.Valid)
                return clone;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
