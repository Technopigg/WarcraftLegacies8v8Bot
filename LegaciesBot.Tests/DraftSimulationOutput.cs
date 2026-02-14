using System;
using System.Collections.Generic;
using System.Linq;
using LegaciesBot.Core;
using LegaciesBot.GameData;
using LegaciesBot.Services;
using Xunit;
using Xunit.Abstractions;

public class DraftSimulationOutput
{
    private readonly ITestOutputHelper _output;

    public DraftSimulationOutput(ITestOutputHelper output)
    {
        _output = output;
    }

    private static Player CreatePlayer(ulong id, string name, int elo, IEnumerable<string> prefs)
    {
        var p = new Player(id, name, elo);
        p.FactionPreferences = prefs.ToList();
        return p;
    }

    private static List<Player> CreatePlayers()
    {
        // Helper to get faction names safely from registry
        string F(string name) => FactionRegistry.All.First(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)).Name;

        var allFactionNames = FactionRegistry.All.Select(f => f.Name).ToList();

        var players = new List<Player>();

        ulong id = 1;

        // 1. Nick – overridden: Dalaran, Legion, Druids
        players.Add(CreatePlayer(id++, "Nick", 1500, new[]
        {
            F("Dalaran"),
            F("Legion"),
            F("Druids")
        }));

        // 2. Boggywoggy (fel aq sw lord Druids scourge)
        players.Add(CreatePlayer(id++, "Boggywoggy", 1450, new[]
        {
            F("Fel Horde"),
            F("An'qiraj"),
            F("Stormwind"),
            F("Lordaeron"),
            F("Druids"),
            F("Scourge")
        }));

        // 3. Konan (warsong AQ Illidan sents scourge fel kt)
        players.Add(CreatePlayer(id++, "Konan", 1500, new[]
        {
            F("Warsong"),
            F("An'qiraj"),
            F("Illidari"),
            F("Sentinels"),
            F("Scourge"),
            F("Fel Horde"),
            F("Kul'tiras")
        }));

        // 4. Dia (everything except scourge gilneas and sunfury)
        var diaPrefs = allFactionNames
            .Where(n =>
                !string.Equals(n, "Scourge", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(n, "Gilneas", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(n, "Sunfury", StringComparison.OrdinalIgnoreCase))
            .ToList();
        players.Add(CreatePlayer(id++, "Dia", 1550, diaPrefs));

        // 5. Helsac (lord skywall sw)
        players.Add(CreatePlayer(id++, "Helsac", 1400, new[]
        {
            F("Lordaeron"),
            F("Skywall"),
            F("Stormwind")
        }));

        // 6. Grom (dwarfs, sw, dranaei Druids lord kt Illidan gilneas sents BE legion)
        players.Add(CreatePlayer(id++, "Grom", 1520, new[]
        {
            F("Ironforge"),
            F("Stormwind"),
            F("The Exodar"),
            F("Druids"),
            F("Lordaeron"),
            F("Kul'tiras"),
            F("Illidari"),
            F("Gilneas"),
            F("Sentinels"),
            F("Black Empire"),
            F("Legion")
        }));

        // 7. Linaz (skywall scourge aq sents dranaei quel Illidan fel dalaran dwarfs kt)
        players.Add(CreatePlayer(id++, "Linaz", 1480, new[]
        {
            F("Skywall"),
            F("Scourge"),
            F("An'qiraj"),
            F("Sentinels"),
            F("The Exodar"),
            F("Quel'thalas"),
            F("Illidari"),
            F("Fel Horde"),
            F("Dalaran"),
            F("Ironforge"),
            F("Kul'tiras")
        }));

        // 8. Theg (lord sents dwarfs)
        players.Add(CreatePlayer(id++, "Theg", 1420, new[]
        {
            F("Lordaeron"),
            F("Sentinels"),
            F("Ironforge")
        }));

        // 9. Technopig (dalaran quel kt Illidan sw)
        players.Add(CreatePlayer(id++, "Technopig", 1490, new[]
        {
            F("Dalaran"),
            F("Quel'thalas"),
            F("Kul'tiras"),
            F("Illidari"),
            F("Stormwind")
        }));

        // 10. Enclop (warsong skywall dalaran scourge kt)
        players.Add(CreatePlayer(id++, "Enclop", 1460, new[]
        {
            F("Warsong"),
            F("Skywall"),
            F("Dalaran"),
            F("Scourge"),
            F("Kul'tiras")
        }));

        // 11. Lukas (gilneas lord quel fw fel Kt)
        players.Add(CreatePlayer(id++, "Lukas", 1440, new[]
        {
            F("Gilneas"),
            F("Lordaeron"),
            F("Quel'thalas"),
            F("Frostwolf"),
            F("Fel Horde"),
            F("Kul'tiras")
        }));

        // 12. Alan (Illidan legion Druids quel lord)
        players.Add(CreatePlayer(id++, "Alan", 1510, new[]
        {
            F("Illidari"),
            F("Legion"),
            F("Druids"),
            F("Quel'thalas"),
            F("Lordaeron")
        }));

        // 13. Royce (fel scourge cows Kt lord)
        players.Add(CreatePlayer(id++, "Royce", 1470, new[]
        {
            F("Fel Horde"),
            F("Scourge"),
            F("Frostwolf"),
            F("Kul'tiras"),
            F("Lordaeron")
        }));

        // 14. Petertros (Kt lord sw dranaei quel)
        players.Add(CreatePlayer(id++, "Petertros", 1430, new[]
        {
            F("Kul'tiras"),
            F("Lordaeron"),
            F("Stormwind"),
            F("The Exodar"),
            F("Quel'thalas")
        }));

        // 15. Dragozer (dalaran scourge fel warsong sents sw sunfur)
        players.Add(CreatePlayer(id++, "Dragozer", 1505, new[]
        {
            F("Dalaran"),
            F("Scourge"),
            F("Fel Horde"),
            F("Warsong"),
            F("Sentinels"),
            F("Stormwind"),
            F("Sunfury")
        }));

        // 16. Madsen (lord sw warsong sunfury gilneas skywall dranaei)
        players.Add(CreatePlayer(id++, "Madsen", 1455, new[]
        {
            F("Lordaeron"),
            F("Stormwind"),
            F("Warsong"),
            F("Sunfury"),
            F("Gilneas"),
            F("Skywall"),
            F("The Exodar")
        }));

        return players;
    }

    [Fact]
    public void RunDraftSimulationAndPrintResults_WithRealPreferences()
    {
        var players = CreatePlayers();

        for (int run = 0; run < 20; run++)
        {
            _output.WriteLine($"==================== RUN {run + 1} ====================");

            var rng = new Random(1000 + run);
            var assignment = new FactionAssignmentService(rng);
            var engine = new DraftEngine(assignment, rng);

            var (teamA, teamB) = engine.RunDraft(players);

            PrintTeamWithPreferenceRanks("TEAM A", teamA);
            PrintTeamWithPreferenceRanks("TEAM B", teamB);

            _output.WriteLine("");
        }
    }

    private void PrintTeamWithPreferenceRanks(string label, dynamic team)
    {
        _output.WriteLine($"\n{label}:");

        for (int i = 0; i < team.Players.Count; i++)
        {
            var p = (Player)team.Players[i];
            var f = (Faction)team.AssignedFactions[i];

            var prefs = p.FactionPreferences;
            var idx = prefs.FindIndex(name =>
                string.Equals(name, f.Name, StringComparison.OrdinalIgnoreCase));

            string rankText = idx >= 0
                ? $"preference rank: {idx + 1} of {prefs.Count}"
                : "preference rank: NOT IN LIST";

            _output.WriteLine(
                $"  {p.Name} (Elo {p.Elo}) → {f.Name} " +
                $"[{f.Group}, Slot={f.SlotId}] — {rankText}"
            );
        }
    }
}