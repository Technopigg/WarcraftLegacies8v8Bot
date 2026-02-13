using LegaciesBot.GameData;

namespace LegaciesBot.Services;

public class RealDefaultPreferences : IDefaultPreferences
{
    public List<string> Factions => DefaultPreferences.Factions;
}