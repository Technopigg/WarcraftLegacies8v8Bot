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
            p.Name = $"Player{id}";
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

        var playerRegistry = new PlayerRegistryService(null);
        var nicknameService = new NicknameService(playerRegistry);
    
        playerRegistry.GetOrCreate(5).Name = "Player5";
        playerRegistry.GetOrCreate(99).Name = "99";

        return new CaptainCommands(lobbyService.Object, draftMock.Object, nicknameService, playerRegistry);
    }

    [Fact]
    public void ClaimCaptain_Fails_WhenPlayerNotInLobby()
    {
        var lobby = new Lobby(); 
        var draftMock = new Mock<ICaptainDraftService>();
        var commands = CreateCommands(lobby, draftMock);

        var result = commands.ClaimCaptain(99); 

        Assert.Equal("You must join the lobby with `!j` before you can claim captain.", result);
    }

    [Fact]
    public void Draft_Fails_WhenTargetNotInLobby()
    {
        var lobby = CreateLobby(); 
        var draftMock = new Mock<ICaptainDraftService>();
        var commands = CreateCommands(lobby, draftMock);
        
        var result = commands.Draft("99", 1); 

        Assert.Equal("That player is not in the lobby.", result);
    }

    [Fact]
    public void ClaimCaptain_Succeeds_WhenInLobby()
    {
        var lobby = CreateLobby(); 
        var draftMock = new Mock<ICaptainDraftService>();
        draftMock.Setup(d => d.TryClaimCaptain(lobby, 1)).Returns(true).Callback<Lobby, ulong>((l, id) => l.CaptainA = id);

        var commands = CreateCommands(lobby, draftMock);
        var result = commands.ClaimCaptain(1);

        Assert.Contains("You are now a captain!", result);
    }
}
