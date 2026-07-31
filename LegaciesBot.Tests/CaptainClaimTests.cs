using LegaciesBot.Core;
using LegaciesBot.Services.CaptainDraft;

namespace LegaciesBot.Tests;

/// <summary>
/// Claiming a captain slot, tested against the real service.
///
/// These replace CaptainCommandsTests, which constructed a 52-line copy of
/// CaptainCommands that lived inside the test project under the production
/// namespace. The compiler bound the tests to the copy (CS0436), so they
/// proved nothing about the shipped command — and the copy had already
/// drifted, asserting a message with backticks that production sends without.
///
/// CaptainCommands itself resolves everything from GlobalServices and replies
/// through a NetCord Context, so it cannot be constructed in a test. The rule
/// it enforces lives in CaptainDraftService, and that is what these cover —
/// which is also the only coverage TryClaimCaptain has ever had.
/// </summary>
public class CaptainClaimTests
{
    private static CaptainDraftService CreateService() => new();

    [Fact]
    public void FirstClaimTakesTheFirstSlot()
    {
        var lobby = new Lobby();
        var service = CreateService();

        Assert.True(service.TryClaimCaptain(lobby, 1));
        Assert.Equal((ulong)1, lobby.CaptainA);
        Assert.Null(lobby.CaptainB);
    }

    [Fact]
    public void SecondClaimTakesTheSecondSlot()
    {
        var lobby = new Lobby();
        var service = CreateService();

        Assert.True(service.TryClaimCaptain(lobby, 1));
        Assert.True(service.TryClaimCaptain(lobby, 2));
        Assert.Equal((ulong)1, lobby.CaptainA);
        Assert.Equal((ulong)2, lobby.CaptainB);
    }

    [Fact]
    public void ThirdClaimIsRefused()
    {
        var lobby = new Lobby();
        var service = CreateService();
        service.TryClaimCaptain(lobby, 1);
        service.TryClaimCaptain(lobby, 2);

        Assert.False(service.TryClaimCaptain(lobby, 3));
        Assert.Equal((ulong)1, lobby.CaptainA);
        Assert.Equal((ulong)2, lobby.CaptainB);
    }

    [Fact]
    public void TheSameCaptainCannotTakeBothSlots()
    {
        // The command refuses this, but it is the only caller and the service
        // used to hand the second slot to whoever asked twice.
        var lobby = new Lobby();
        var service = CreateService();
        Assert.True(service.TryClaimCaptain(lobby, 1));

        Assert.False(service.TryClaimCaptain(lobby, 1));
        Assert.Null(lobby.CaptainB);
    }

    [Fact]
    public void ClaimingIsRefusedOnceBothSlotsAreTaken_EvenByACaptain()
    {
        var lobby = new Lobby();
        var service = CreateService();
        service.TryClaimCaptain(lobby, 1);
        service.TryClaimCaptain(lobby, 2);

        Assert.False(service.TryClaimCaptain(lobby, 2));
    }
}
