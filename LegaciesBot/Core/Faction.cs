namespace LegaciesBot.Core;

public class Faction
{
    public string Name { get; }
    public TeamGroup Group { get; }
    public string SlotId { get; }

    public Faction(string name, TeamGroup group, string slotId = null)
    {
        Name = name;
        Group = group;
        SlotId = slotId ?? name;
    }
}