using NetCord;
using NetCord.Rest;

namespace LegaciesBot.Services;

public interface IGatewayClient
{
    Task<ITextChannel?> GetTextChannelAsync(ulong channelId);

    Task<RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false);

    Task<Role> CreateRoleAsync(ulong guildId, string name);

    Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId);

    Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId);

    Task DeleteRoleAsync(ulong guildId, ulong roleId);
}