using LegaciesBot.Core;

namespace LegaciesBot.Services
{
    public class NicknameService
    {
        private readonly PlayerRegistryService _players;

        public NicknameService(PlayerRegistryService players)
        {
            _players = players;
        }

        public ulong? ResolvePlayerId(string input)
        {
            var player = _players.Resolve(input);
            return player?.DiscordId;
        }
    }
}