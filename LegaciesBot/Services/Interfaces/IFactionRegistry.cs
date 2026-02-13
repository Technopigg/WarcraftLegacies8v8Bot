using LegaciesBot.Core;


namespace LegaciesBot.Services;

public interface IFactionRegistry
{
    IEnumerable<Faction> All { get; }
}