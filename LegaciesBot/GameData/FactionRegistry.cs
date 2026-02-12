using LegaciesBot.Core;

namespace LegaciesBot.GameData;

public static class FactionRegistry
{
    public static readonly List<Faction> All = new()
    {
        new("Lordaeron", TeamGroup.NorthAlliance),
        new("Quel'thalas", TeamGroup.NorthAlliance),
        new("Dalaran", TeamGroup.NorthAlliance, "DalaranSlot"),
        new("Gilneas", TeamGroup.NorthAlliance, "DalaranSlot"),
        
        new("Scourge", TeamGroup.BurningLegion),
        new("Legion", TeamGroup.BurningLegion),
        
        new("Stormwind", TeamGroup.SouthAlliance),
        new("Ironforge", TeamGroup.SouthAlliance),
        new("Kul'tiras", TeamGroup.SouthAlliance),

      
        new("Fel Horde", TeamGroup.FelHorde),
        new("Illidari", TeamGroup.FelHorde),
        
        new("Warsong", TeamGroup.Kalimdor, "WarsongSlot"),
        new("Frostwolf", TeamGroup.Kalimdor, "WarsongSlot"),
        new("Sentinels", TeamGroup.Kalimdor, "SentinelsSlot"),
        new("The Exodar", TeamGroup.Kalimdor, "SentinelsSlot"),
        new("Druids", TeamGroup.Kalimdor),
        
        new("An'qiraj", TeamGroup.OldGods),
        new("Black Empire", TeamGroup.OldGods),
        new("Skywall", TeamGroup.OldGods)
    };
}