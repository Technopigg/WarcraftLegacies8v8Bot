public class PlayerDataServiceTests
{
    private string CreateTempPrefsFile(string? json = null)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, json ?? "{}");
        return path;
    }

    [Fact]
    public void GetPreferences_ReturnsEmptyList_WhenUserNotFound()
    {
        string file = CreateTempPrefsFile();
        var service = new PlayerDataService(file);

        var prefs = service.GetPreferences(123);

        Assert.Empty(prefs);
    }

    [Fact]
    public void SetPreferences_SavesToFile()
    {
        string file = CreateTempPrefsFile();
        var service = new PlayerDataService(file);

        service.SetPreferences(1, new() { "A", "B" });

        string json = File.ReadAllText(file);
        Assert.Contains("A", json);
        Assert.Contains("B", json);
    }

    [Fact]
    public void SetPreferences_OverwritesExisting()
    {
        string file = CreateTempPrefsFile(@"{ ""1"": [""Old""] }");
        var service = new PlayerDataService(file);

        service.SetPreferences(1, new() { "New1", "New2" });

        var prefs = service.GetPreferences(1);

        Assert.Equal(2, prefs.Count);
        Assert.Contains("New1", prefs);
        Assert.Contains("New2", prefs);
        Assert.DoesNotContain("Old", prefs);
    }

    [Fact]
    public void Load_ReadsExistingFile()
    {
        string file = CreateTempPrefsFile(@"{ ""5"": [""X"", ""Y""] }");

        var service = new PlayerDataService(file);

        var prefs = service.GetPreferences(5);

        Assert.Equal(2, prefs.Count);
        Assert.Contains("X", prefs);
        Assert.Contains("Y", prefs);
    }

    [Fact]
    public void Load_CreatesEmptyPrefs_WhenFileMissing()
    {
        string file = Path.Combine(Path.GetTempPath(), "missing_prefs.json");
        if (File.Exists(file))
            File.Delete(file);

        var service = new PlayerDataService(file);

        Assert.Empty(service.GetPreferences(1));
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void MultipleUsers_AreStoredIndependently()
    {
        string file = CreateTempPrefsFile();
        var service = new PlayerDataService(file);

        service.SetPreferences(1, new() { "A" });
        service.SetPreferences(2, new() { "B", "C" });

        var p1 = service.GetPreferences(1);
        var p2 = service.GetPreferences(2);

        Assert.Single(p1);
        Assert.Equal(2, p2.Count);
        Assert.Contains("A", p1);
        Assert.Contains("B", p2);
        Assert.Contains("C", p2);
    }

    [Fact]
    public void Save_WritesIndentedJson()
    {
        string file = CreateTempPrefsFile();
        var service = new PlayerDataService(file);

        service.SetPreferences(1, new() { "A" });

        string json = File.ReadAllText(file);

        Assert.Contains("\n", json);
    }
}