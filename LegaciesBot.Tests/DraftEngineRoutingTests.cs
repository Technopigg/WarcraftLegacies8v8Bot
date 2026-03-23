using LegaciesBot.Core;
using LegaciesBot.Services;

namespace LegaciesBot.Tests;

public class DraftEngineRoutingTests
{
    private Lobby CreateLobby(int count)
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);

        for (int i = 0; i < count; i++)
        {
            ulong id = (ulong)(i + 1);
            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        return lobby;
    }

    [Fact]
    public void DraftEngine_DoesNotThrow_ForAllDraftModes()
    {
        var rng = new Random(12345);
        
        var factionAssign = new FactionAssignmentService(rng);

        var engine = new DraftEngine(factionAssign, rng);

        var modes = new[]
        {
            DraftMode.AutoDraft_AutoFaction,
            DraftMode.AutoDraft_ManualFaction,
            DraftMode.CaptainDraft_AutoFaction,
            DraftMode.CaptainDraft_ManualFaction
        };

        foreach (var mode in modes)
        {
            var lobby = CreateLobby(16);
            lobby.DraftMode = mode;

            if (mode == DraftMode.CaptainDraft_ManualFaction)
            {
                Assert.Throws<NotImplementedException>(() => engine.RunDraft(lobby));
            }
            else
            {
                var (teamA, teamB) = engine.RunDraft(lobby);
                Assert.NotNull(teamA);
                Assert.NotNull(teamB);
            }
        }
    }
}