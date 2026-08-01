using System.Text.RegularExpressions;

namespace LegaciesBot.Tests;

/// <summary>
/// Every warcraftlegacies.com link the bot puts in front of a player has to be
/// a page that exists.
///
/// Three commands pointed at /leaderboard and two at /players. Neither route
/// has ever existed — the leaderboard lives at /rankings, and /players only
/// answers with a battletag after it. So !stats with no argument, !stats for
/// an unknown name, !leaderboard and !season all offered a 404 as the place to
/// go next.
///
/// The list below is checked against the source rather than against the live
/// site, so it fails in CI without a network call. Adding a route here is a
/// deliberate act: confirm it answers 200 first.
/// </summary>
public class SiteLinkTests
{
    private static readonly string[] KnownPaths =
    {
        "",              // the site root
        "/rankings",
        "/ratings",
        "/matches",
        "/replays",
        "/maps",
        "/stats",
        "/guides",
        "/lobbies",
        "/upload",
        "/balance",
        "/factions",
    };

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "LegaciesBot")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }

    private static IEnumerable<(string File, string Url)> SiteLinksInSource()
    {
        var root = Path.Combine(RepositoryRoot(), "LegaciesBot");
        var pattern = new Regex(@"https://warcraftlegacies\.com(/[A-Za-z0-9\-/_]*)?");

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match m in pattern.Matches(text))
            {
                yield return (Path.GetFileName(file), m.Groups[1].Value.TrimEnd('/'));
            }
        }
    }

    [Fact]
    public void EverySiteLinkTheBotPostsIsAPageThatExists()
    {
        var broken = SiteLinksInSource()
            .Where(link => !KnownPaths.Contains(link.Url))
            .Select(link => $"{link.File}: {link.Url}")
            .Distinct()
            .ToList();

        Assert.Empty(broken);
    }

    [Fact]
    public void TheBotActuallyLinksToTheSiteSomewhere()
    {
        // Guards the test itself: a regex that stopped matching would make the
        // check above pass by finding nothing at all.
        Assert.NotEmpty(SiteLinksInSource());
    }
}
