namespace LegaciesBot.Discord
{
    /// <summary>
    /// Reads a command's usage line out of the same CommandList that !bothelp prints.
    ///
    /// Why from that file: a command called without its required arguments used to answer
    /// nothing at all, because the router reports a parameter mismatch and nobody handled
    /// it. Nine commands behaved that way (!compare, !nickname, !warns, !draft, !ban,
    /// !unban, !warn, !removewarn, !assignf), so the player could not tell whether the bot
    /// was dead or they had typed it wrong. Writing usage strings a second time here would
    /// just mean two copies to drift apart, and CommandList already has one line per
    /// command in the shape "!cmd &lt;args&gt; — description".
    /// </summary>
    public static class CommandUsage
    {
        private static readonly object Lock = new();
        private static Dictionary<string, List<string>>? _byCommand;

        public static string? For(string word)
        {
            var table = Load();
            return table.TryGetValue(word.ToLowerInvariant(), out var lines)
                ? string.Join("\n", lines)
                : null;
        }

        private static Dictionary<string, List<string>> Load()
        {
            lock (Lock)
            {
                if (_byCommand is not null) return _byCommand;

                var table = new Dictionary<string, List<string>>();
                var path = Path.Combine(AppContext.BaseDirectory, "CommandList");
                if (File.Exists(path))
                {
                    foreach (var raw in File.ReadAllLines(path))
                    {
                        var line = raw.Trim();
                        if (!line.StartsWith('!')) continue;

                        // Only the part before the dash names commands; the description may
                        // mention other commands and must not claim their usage line.
                        var dash = line.IndexOf('—');
                        var head = dash > 0 ? line.Substring(0, dash) : line;

                        foreach (var token in head.Split(new[] { ' ', '/', ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!token.StartsWith('!')) continue;
                            var name = token.Substring(1).ToLowerInvariant();
                            if (name.Length == 0) continue;
                            if (!table.TryGetValue(name, out var list))
                            {
                                list = new List<string>();
                                table[name] = list;
                            }
                            if (!list.Contains(line)) list.Add(line);
                        }
                    }
                }

                _byCommand = table;
                return table;
            }
        }
    }
}
