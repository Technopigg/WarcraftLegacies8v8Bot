using LegaciesBot.Core;
using LegaciesBot.GameData;

namespace LegaciesBot.Services;

public class RealFactionRegistry : IFactionRegistry
{
    public IEnumerable<Faction> All => FactionRegistry.All;
}