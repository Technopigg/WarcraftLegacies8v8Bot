using System.Diagnostics;
using NetCord.Services.Commands;

namespace LegaciesBot.Discord
{
    /// <summary>
    /// `!version` — lets admins confirm a deploy actually landed. The build time comes from
    /// the bot's own DLL (written by `dotnet publish` during the Docker build), so it changes
    /// on every deploy even if nobody remembers to bump <see cref="Version"/>.
    /// </summary>
    public class InfoCommands : CommandModule<CommandContext>
    {
        // Bump this when shipping something players should notice.
        public const string Version = "7v7bot 5.1 (Season 5: 7v7, Horde vs Night Elves, !forcejoin)";

        [Command("version")]
        [Command("v")]
        public async Task ShowVersion()
        {
            var builtAt = File.GetLastWriteTimeUtc(typeof(InfoCommands).Assembly.Location);
            var startedAt = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            await Context.Message.ReplyAsync(BuildVersionText(builtAt, startedAt));
        }

        // Discord renders <t:unix:f> as a local date/time for each reader, <t:unix:R> as "2 hours ago".
        public static string BuildVersionText(DateTime builtAtUtc, DateTime startedAtUtc)
        {
            long built = new DateTimeOffset(builtAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
            long started = new DateTimeOffset(startedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();
            return $"**{Version}**\n" +
                   $"Built: <t:{built}:f> (<t:{built}:R>)\n" +
                   $"Online since: <t:{started}:f> (<t:{started}:R>)";
        }
    }
}
