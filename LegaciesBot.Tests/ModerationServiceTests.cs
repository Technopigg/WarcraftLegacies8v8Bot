using LegaciesBot.Moderation;

namespace LegaciesBot.Tests;

public class ModerationServiceTests
{
    private ModerationService CreateService()
    {
        if (File.Exists("moderation.json"))
            File.Delete("moderation.json");

        return new ModerationService(
            TimeSpan.FromMinutes(5), 
            3                       
        );
    }

    [Fact]
    public void AddWarning_AddsWarning()
    {
        var service = CreateService();

        bool autoBanned = service.AddWarning(5, 1, "test", null);

        var warnings = service.GetActiveWarnings(5);

        Assert.False(autoBanned);
        Assert.Single(warnings);
        Assert.Equal("test", warnings[0].Reason);
        Assert.Equal((ulong)1, warnings[0].ModeratorId);
    }

    [Fact]
    public void AddWarning_AutoBans_WhenThresholdReached()
    {
        var service = CreateService();

        service.AddWarning(5, 1, "a", null);
        service.AddWarning(5, 1, "b", null);
        bool autoBanned = service.AddWarning(5, 1, "c", null);

        Assert.True(autoBanned);
        Assert.True(service.IsBanned(5));
    }

    [Fact]
    public void RemoveWarning_RemovesCorrectIndex()
    {
        var service = CreateService();

        service.AddWarning(5, 1, "a", null);
        service.AddWarning(5, 1, "b", null);
        service.AddWarning(5, 1, "c", null);

        bool removed = service.RemoveWarning(5, 1);

        var warnings = service.GetActiveWarnings(5);

        Assert.True(removed);
        Assert.Equal(2, warnings.Count);
        Assert.Equal("a", warnings[0].Reason);
        Assert.Equal("c", warnings[1].Reason);
    }

    [Fact]
    public void RemoveWarning_AutoUnbans_WhenBelowThreshold()
    {
        var service = CreateService();

        service.AddWarning(5, 1, "a", null);
        service.AddWarning(5, 1, "b", null);
        service.AddWarning(5, 1, "c", null);

        Assert.True(service.IsBanned(5));

        service.RemoveWarning(5, 2); 

        Assert.False(service.IsBanned(5));
    }

    [Fact]
    public void AddWarning_WithExpiry_ExpiresCorrectly()
    {
        var service = CreateService();

        service.AddWarning(5, 1, "test", TimeSpan.FromMilliseconds(200));

        Assert.Single(service.GetActiveWarnings(5));

        Thread.Sleep(300);

        Assert.Empty(service.GetActiveWarnings(5));
    }

    [Fact]
    public void ManualBan_BansUser()
    {
        var service = CreateService();

        service.AddBan(5, 1, "bad");

        Assert.True(service.IsBanned(5));
    }

    [Fact]
    public void ManualUnban_UnbansUser()
    {
        var service = CreateService();

        service.AddBan(5, 1, "bad");

        bool removed = service.RemoveBan(5);

        Assert.True(removed);
        Assert.False(service.IsBanned(5));
    }

    [Fact]
    public void GetActiveWarnings_FiltersExpired()
    {
        var service = CreateService();

        service.AddWarning(5, 1, "valid", null);
        service.AddWarning(5, 1, "expired", TimeSpan.FromMilliseconds(200));

        Thread.Sleep(300);

        var warnings = service.GetActiveWarnings(5);

        Assert.Single(warnings);
        Assert.Equal("valid", warnings[0].Reason);
    }

    [Fact]
    public void IsBanned_ReturnsFalse_WhenNeverBanned()
    {
        var service = CreateService();

        Assert.False(service.IsBanned(5));
    }
}
