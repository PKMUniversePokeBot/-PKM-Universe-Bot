// PKM Universe Bot - Switch Bot Configuration
// Written by PKM Universe - 2025

namespace PKMUniverse.Core.Config;

public class SwitchBotConfig
{
    public string Name { get; set; } = "Bot";
    public string IP { get; set; } = "192.168.1.1";
    public int Port { get; set; } = 6000;
    public GameVersion Game { get; set; } = GameVersion.LegendsZA;
    public bool Enabled { get; set; } = true;
}

public enum GameVersion
{
    ScarletViolet,
    LegendsZA,
    LegendsArceus,
    BrilliantDiamondShiningPearl,
    SwordShield
}
