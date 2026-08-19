using System.Net;
using System.Text;
using LegaciesBot.Services;

namespace LegaciesBot.Tests;

public class LinkSuggestionTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, (HttpStatusCode, string)> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (code, body) = responder(request);
            return Task.FromResult(new HttpResponseMessage(code)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static SiteApiService Site(Func<HttpRequestMessage, (HttpStatusCode, string)> responder) =>
        new(new HttpClient(new FakeHandler(responder)), "https://test.local", "token");

    private static (HttpStatusCode, string) Route(HttpRequestMessage req, bool linked, string searchJson)
    {
        var path = req.RequestUri!.AbsolutePath;
        if (path.Contains("/by-discord/"))
            return linked ? (HttpStatusCode.OK, "{\"player_key\":\"royce\",\"display_name\":\"Royce\"}") : (HttpStatusCode.NotFound, "{}");
        if (path.Contains("/players/search"))
            return (HttpStatusCode.OK, searchJson);
        return (HttpStatusCode.NotFound, "{}");
    }

    [Fact]
    public async Task Suggests_single_match_when_unlinked()
    {
        var site = Site(req => Route(req, linked: false,
            "{\"results\":[{\"battletag\":\"Royce#1989\",\"display_name\":\"Royce\"}]}"));

        var embed = await LinkSuggestion.BuildAsync(site, 42, "royce");

        Assert.NotNull(embed);
        Assert.Equal("Is this you?", embed!.Title);
        Assert.Contains("Royce#1989", embed.Description);
    }

    [Fact]
    public async Task No_suggestion_when_already_linked()
    {
        var site = Site(req => Route(req, linked: true, "{\"results\":[]}"));
        Assert.Null(await LinkSuggestion.BuildAsync(site, 42, "royce"));
    }

    [Fact]
    public async Task No_suggestion_when_no_match()
    {
        var site = Site(req => Route(req, linked: false, "{\"results\":[]}"));
        Assert.Null(await LinkSuggestion.BuildAsync(site, 42, "nobody"));
    }

    [Fact]
    public async Task Lists_candidates_when_ambiguous()
    {
        var site = Site(req => Route(req, linked: false,
            "{\"results\":[{\"battletag\":\"Royce#1989\",\"display_name\":\"Royce\"},{\"battletag\":\"Royce#2222\",\"display_name\":\"Royce\"}]}"));

        var embed = await LinkSuggestion.BuildAsync(site, 42, "royce");

        Assert.NotNull(embed);
        Assert.Equal("Which one is you?", embed!.Title);
        Assert.Contains("Royce#1989", embed.Description);
        Assert.Contains("Royce#2222", embed.Description);
    }

    [Fact]
    public async Task No_suggestion_when_site_unavailable()
    {
        // by-discord returns 500 => Unavailable (not NotFound); must not suggest.
        var site = Site(_ => (HttpStatusCode.InternalServerError, "{}"));
        Assert.Null(await LinkSuggestion.BuildAsync(site, 42, "royce"));
    }
}
