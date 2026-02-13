using System.IO;
using LegaciesBot.Services;
using Xunit;

public class PermissionServiceTests
{
    private string CreateTempFile()
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, "{}"); // empty JSON object
        return path;
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_WhenUserIsAdmin()
    {
        var service = new PermissionService(CreateTempFile());
        service.Data.Admins.Add(100);

        Assert.True(service.IsAdmin(100));
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenUserIsNotAdmin()
    {
        var service = new PermissionService(CreateTempFile());

        Assert.False(service.IsAdmin(999));
    }

    [Fact]
    public void IsMod_ReturnsTrue_WhenUserIsMod()
    {
        var service = new PermissionService(CreateTempFile());
        service.Data.Mods.Add(200);

        Assert.True(service.IsMod(200));
    }

    [Fact]
    public void IsMod_ReturnsTrue_WhenUserIsAdmin()
    {
        var service = new PermissionService(CreateTempFile());
        service.Data.Admins.Add(300);

        Assert.True(service.IsMod(300));
    }

    [Fact]
    public void IsMod_ReturnsFalse_WhenUserIsNeither()
    {
        var service = new PermissionService(CreateTempFile());

        Assert.False(service.IsMod(777));
    }

    [Fact]
    public void AddAdmin_AddsUser_AndSavesToFile()
    {
        string file = CreateTempFile();
        var service = new PermissionService(file);

        service.AddAdmin(123);

        Assert.Contains(123ul, service.Data.Admins);

        string json = File.ReadAllText(file);
        Assert.Contains("123", json);
    }

    [Fact]
    public void RemoveAdmin_RemovesUser_AndSavesToFile()
    {
        string file = CreateTempFile();
        var service = new PermissionService(file);

        service.Data.Admins.Add(456);
        service.Save();

        service.RemoveAdmin(456);

        Assert.DoesNotContain(456ul, service.Data.Admins);

        string json = File.ReadAllText(file);
        Assert.DoesNotContain("456", json);
    }
    
    [Fact]
    public void AddMod_AddsUser_AndSavesToFile()
    {
        string file = CreateTempFile();
        var service = new PermissionService(file);

        service.AddMod(789);

        Assert.Contains(789ul, service.Data.Mods);

        string json = File.ReadAllText(file);
        Assert.Contains("789", json);
    }

    [Fact]
    public void RemoveMod_RemovesUser_AndSavesToFile()
    {
        string file = CreateTempFile();
        var service = new PermissionService(file);

        service.Data.Mods.Add(987);
        service.Save();

        service.RemoveMod(987);

        Assert.DoesNotContain(987ul, service.Data.Mods);

        string json = File.ReadAllText(file);
        Assert.DoesNotContain("987", json);
    }

    [Fact]
    public void Constructor_LoadsExistingFile()
    {
        string file = CreateTempFile();

        File.WriteAllText(file, @"{
            ""Admins"": [111],
            ""Mods"": [222]
        }");

        var service = new PermissionService(file);

        Assert.Contains(111ul, service.Data.Admins);
        Assert.Contains(222ul, service.Data.Mods);
    }

    [Fact]
    public void Constructor_CreatesFile_WhenMissing()
    {
        string file = Path.Combine(Path.GetTempPath(), "missing_permissions.json");
        if (File.Exists(file))
            File.Delete(file);

        var service = new PermissionService(file);

        Assert.True(File.Exists(file));
        Assert.NotNull(service.Data);
    }
}