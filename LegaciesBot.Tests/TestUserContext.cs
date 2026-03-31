using LegaciesBot.Services;

namespace LegaciesBot.Tests
{
    public class TestUserContext : IUserContext
    {
        public ulong UserId { get; set; }
    }
}