using LegaciesBot.Core;
using NetCord.Services.Commands;
using LegaciesBot.Services;
using LegaciesBot.Seasons;

namespace LegaciesBot.Discord
{
    public class StatsCommands : CommandModule<CommandContext>
    {
        private readonly PlayerStatsService _playerStats;
        private readonly PlayerRegistryService _playerRegistry;
        private readonly NicknameService _nicknames;
        private readonly SeasonService _seasons;

        public StatsCommands()
        {
            _playerStats = GlobalServices.PlayerStatsService;
            _playerRegistry = GlobalServices.PlayerRegistryService;
            _nicknames = GlobalServices.NicknameService;
            _seasons = GlobalServices.SeasonService;
        }

        [Command("stats")]
        public async Task Stats(string? input = null, ulong testUserId = 0)
        {
            await this.Context.Message.ReplyAsync(
                "Season 5 player stats are on the site: **https://warcraftlegacies.com/players**");
        }

        [Command("leaderboard")]
        public async Task Leaderboard(string? mode = null, int count = 10)
        {
            await this.Context.Message.ReplyAsync(
                "Season 5 leaderboard is on the site: **https://warcraftlegacies.com/leaderboard**");
        }

        [Command("compare")]
        public async Task Compare(string name1, string name2, string? mode = null)
        {
            await this.Context.Message.ReplyAsync(
                "Season 5 player stats are on the site: **https://warcraftlegacies.com/players**");
        }
    }
}
