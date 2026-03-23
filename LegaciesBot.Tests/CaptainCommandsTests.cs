using LegaciesBot.Core;
using LegaciesBot.Commands;
using LegaciesBot.Services;
using LegaciesBot.Services.CaptainDraft;
using Moq;

namespace LegaciesBot.Tests;

public class CaptainCommandsTests
{
    private Lobby CreateLobby()
    {
        var lobby = new Lobby();
        var registry = new PlayerRegistryService(null);
        for (int i = 0; i < 16; i++)
        {
            ulong id = (ulong)(i + 1);
            var p = registry.GetOrCreate(id);
            p.Name = $"Player{i + 1}";
            p.Elo = 1500;
            lobby.Players.Add(p);
        }

        lobby.DraftMode = DraftMode.CaptainDraft_AutoFaction;

        return lobby;
    }

    private CaptainCommands CreateCommands(Lobby lobby, Mock<ICaptainDraftService> draftMock)
    {
        var lobbyService = new Mock<ILobbyService>();
        lobbyService.Setup(l => l.CurrentLobby).Returns(lobby);

        return new CaptainCommands(lobbyService.Object, draftMock.Object);
    }

    [Fact]
    public void ClaimCaptain_Succeeds_WhenSlotAvailable()
    {
        var lobby = CreateLobby();

        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.TryClaimCaptain(lobby, 1)).Returns(true);

        var commands = CreateCommands(lobby, draftMock);

        var result = commands.ClaimCaptain(1);

        Assert.Equal("You are now a captain.", result);
    }

    [Fact]
    public void ClaimCaptain_Fails_WhenTwoCaptainsExist()
    {
        var lobby = CreateLobby();

        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.TryClaimCaptain(lobby, 1)).Returns(false);

        var commands = CreateCommands(lobby, draftMock);

        var result = commands.ClaimCaptain(1);

        Assert.Equal("Two captains already exist.", result);
    }

    [Fact]
    public void Draft_Fails_WhenNotCaptainTurn()
    {
        var lobby = CreateLobby();

        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.IsCaptainTurn(lobby, 1)).Returns(false);

        var commands = CreateCommands(lobby, draftMock);

        var result = commands.Draft(1, 5);

        Assert.Equal("It is not your turn to pick.", result);
    }

    [Fact]
    public void Draft_Fails_WhenPickInvalid()
    {
        var lobby = CreateLobby();

        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.IsCaptainTurn(lobby, 1)).Returns(true);
        draftMock.Setup(d => d.TryPick(lobby, 1, 5)).Returns(false);

        var commands = CreateCommands(lobby, draftMock);

        var result = commands.Draft(1, 5);

        Assert.Equal("Invalid pick.", result);
    }

    [Fact]
    public void Draft_Succeeds_WhenValidPick()
    {
        var lobby = CreateLobby();

        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.IsCaptainTurn(lobby, 1)).Returns(true);
        draftMock.Setup(d => d.TryPick(lobby, 1, 5)).Returns(true);
        draftMock.Setup(d => d.DraftComplete(lobby)).Returns(false);

        var commands = CreateCommands(lobby, draftMock);

        var result = commands.Draft(1, 5);

        Assert.Equal("Pick successful.", result);
    }

    [Fact]
    public void Draft_ReportsCompletion_WhenDraftEnds()
    {
        var lobby = CreateLobby();

        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.IsCaptainTurn(lobby, 1)).Returns(true);
        draftMock.Setup(d => d.TryPick(lobby, 1, 5)).Returns(true);
        draftMock.Setup(d => d.DraftComplete(lobby)).Returns(true);

        var commands = CreateCommands(lobby, draftMock);

        var result = commands.Draft(1, 5);

        Assert.Equal("Draft complete! Teams are locked.", result);
    }
}