using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace LegaciesBot.Services;

public class RealGatewayClient : IGatewayClient
{
    private readonly GatewayClient _client;

    public RealGatewayClient(GatewayClient client)
    {
        _client = client;
    }

    public async Task<ITextChannel?> GetTextChannelAsync(ulong channelId)
    {
        var channel = await _client.Rest.GetChannelAsync(channelId);

        if (channel is NetCord.TextChannel text)
            return new RealTextChannel(text);

        return null;
    }

    public Task<RestGuild> GetGuildAsync(ulong guildId, bool withCounts = false)
    {
        return _client.Rest.GetGuildAsync(guildId, withCounts);
    }

    public Task<Role> CreateRoleAsync(ulong guildId, string name)
    {
        return _client.Rest.CreateGuildRoleAsync(guildId, new RoleProperties
        {
            Name = name
        });
    }

    public Task AddRoleToMemberAsync(ulong guildId, ulong userId, ulong roleId)
    {
        return _client.Rest.AddGuildUserRoleAsync(guildId, userId, roleId);
    }

    public Task RemoveRoleFromMemberAsync(ulong guildId, ulong userId, ulong roleId)
    {
        return _client.Rest.RemoveGuildUserRoleAsync(guildId, userId, roleId);
    }

    public Task DeleteRoleAsync(ulong guildId, ulong roleId)
    {
        return _client.Rest.DeleteGuildRoleAsync(guildId, roleId);
    }
}