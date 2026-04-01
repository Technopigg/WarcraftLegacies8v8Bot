using LegaciesBot.Core;
using LegaciesBot.Services;
using LegaciesBot.Services.Drafting;

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
    public void DraftEngine_RoutesCorrectly_ForAllDraftModes()
    {
        var rng = new Random(12345);
        var factionAssign = new RealFactionAssignmentService(new FactionRegistryStub());
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

            if (mode == DraftMode.CaptainDraft_AutoFaction ||
                mode == DraftMode.CaptainDraft_ManualFaction)
            {
                lobby.TeamAPicks = lobby.Players
                    .Take(8)
                    .Select(p => p.DiscordId)
                    .ToList();

                lobby.TeamBPicks = lobby.Players
                    .Skip(8)
                    .Take(8)
                    .Select(p => p.DiscordId)
                    .ToList();
            }

            var (teamA, teamB) = engine.RunDraft(lobby);

            Assert.NotNull(teamA);
            Assert.NotNull(teamB);

            Assert.Equal(8, teamA.Players.Count);
            Assert.Equal(8, teamB.Players.Count);

            Assert.Empty(teamA.Players.Intersect(teamB.Players));
        }
    }
}
