using System.Text;

namespace LegaciesBot.GameData;

/// <summary>
/// Result of parsing a free-form faction list from a user: the factions we
/// recognised (canonical names, in order, de-duplicated) and the tokens we
/// could not match (so the bot can tell the player instead of silently
/// dropping them).
/// </summary>
public sealed record FactionParseResult(IReadOnlyList<string> Accepted, IReadOnlyList<string> Unknown);

/// <summary>
/// Forgiving faction parser. The old logic split on spaces and required an exact,
/// full-name match, so "Fel Horde", commas, quotes and abbreviations were all
/// dropped without a word to the player. This one:
///  - tolerates commas and quotes,
///  - matches multi-word names ("Fel Horde", "The Exodar", "Black Empire"),
///  - accepts common aliases and abbreviations (illidan, draenei, quel, fel...),
///  - forgives a one-character typo (iroforge -> Ironforge),
///  - and reports whatever it could not understand.
/// </summary>
public static class FactionParser
{
    // Normalised alias -> canonical faction name. Normalised = lowercase, letters/digits only.
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["illidan"] = "Illidari",
        ["illidani"] = "Illidari",
        ["illidary"] = "Illidari",
        ["exodar"] = "The Exodar",
        ["draenei"] = "The Exodar",
        ["draenai"] = "The Exodar",
        ["quel"] = "Quel'thalas",
        ["belf"] = "Quel'thalas",
        ["bloodelf"] = "Quel'thalas",
        ["bloodelves"] = "Quel'thalas",
        ["fel"] = "Fel Horde",
        ["kt"] = "Kul'tiras",
        ["kul"] = "Kul'tiras",
        ["tiras"] = "Kul'tiras",
        ["aq"] = "An'qiraj",
        ["qiraj"] = "An'qiraj",
        ["fw"] = "Frostwolf",
        ["iron"] = "Ironforge",
        ["druid"] = "Druids",
    };

    // Normalised key -> canonical name, built once from the registry plus the aliases above.
    private static readonly Dictionary<string, string> Lookup = BuildLookup();

    private static Dictionary<string, string> BuildLookup()
    {
        var map = new Dictionary<string, string>();
        foreach (var faction in FactionRegistry.All)
            map[Normalize(faction.Name)] = faction.Name;
        foreach (var (alias, canonical) in Aliases)
            map.TryAdd(Normalize(alias), canonical);
        return map;
    }

    /// <summary>The canonical faction names, in registry order.</summary>
    public static IReadOnlyList<string> CanonicalNames =>
        FactionRegistry.All.Select(f => f.Name).ToList();

    public static FactionParseResult Parse(string? input)
    {
        var accepted = new List<string>();
        var unknown = new List<string>();
        if (string.IsNullOrWhiteSpace(input))
            return new FactionParseResult(accepted, unknown);

        var tokens = input
            .Replace(',', ' ')
            .Replace('"', ' ')
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var i = 0;
        while (i < tokens.Length)
        {
            var matched = false;

            // Greedy: try the longest multi-word name first ("The Exodar" before "The").
            for (var span = Math.Min(3, tokens.Length - i); span >= 1 && !matched; span--)
            {
                var key = Normalize(string.Concat(tokens.Skip(i).Take(span)));
                if (key.Length > 0 && Lookup.TryGetValue(key, out var canonical))
                {
                    Add(accepted, seen, canonical);
                    i += span;
                    matched = true;
                }
            }

            if (matched)
                continue;

            // One-token typo tolerance (iroforge -> ironforge), tight so it can't guess wildly.
            var single = Normalize(tokens[i]);
            if (single.Length >= 3 && TryFuzzy(single, out var fuzzy))
            {
                Add(accepted, seen, fuzzy);
            }
            else
            {
                var raw = tokens[i].Trim('\'', '.', ';', ':');
                if (raw.Length > 0 && !unknown.Contains(raw, StringComparer.OrdinalIgnoreCase))
                    unknown.Add(raw);
            }

            i++;
        }

        return new FactionParseResult(accepted, unknown);
    }

    private static void Add(List<string> accepted, HashSet<string> seen, string canonical)
    {
        if (seen.Add(canonical))
            accepted.Add(canonical);
    }

    private static bool TryFuzzy(string token, out string canonical)
    {
        canonical = string.Empty;
        var best = int.MaxValue;
        foreach (var (key, name) in Lookup)
        {
            // Only compare against keys of similar length to avoid silly matches.
            if (Math.Abs(key.Length - token.Length) > 1)
                continue;
            var d = Levenshtein(token, key);
            if (d < best)
            {
                best = d;
                canonical = name;
            }
        }
        return best <= 1;
    }

    private static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    private static int Levenshtein(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
