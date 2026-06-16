using NetCord;
using NetCord.Rest;

namespace LegaciesBot.Services
{
    public static class EmbedFactory
    {
        private static readonly Color Green  = new(0x57F287);
        private static readonly Color Blue   = new(0x5865F2);
        private static readonly Color Yellow = new(0xFEE75C);
        private static readonly Color Red    = new(0xED4245);
        private static readonly Color Gray   = new(0x2B2D31);

        private static EmbedProperties Base() =>
            new EmbedProperties()
                .WithFooter(new EmbedFooterProperties().WithText("warcraftlegacies.com"));

        public static EmbedProperties Success(string title, string? description = null) =>
            Base().WithColor(Green).WithTitle(title)
                  .WithDescription(description);

        public static EmbedProperties Info(string title, string? description = null) =>
            Base().WithColor(Blue).WithTitle(title)
                  .WithDescription(description);

        public static EmbedProperties Warning(string title, string? description = null) =>
            Base().WithColor(Yellow).WithTitle(title)
                  .WithDescription(description);

        public static EmbedProperties Error(string title, string? description = null) =>
            Base().WithColor(Red).WithTitle(title)
                  .WithDescription(description);

        public static EmbedProperties Neutral(string title, string? description = null) =>
            Base().WithColor(Gray).WithTitle(title)
                  .WithDescription(description);
    }
}
