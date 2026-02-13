public class PlayerDataServiceTests
{
    private string CreateTempPrefsFile(string? json = null)
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, json ?? "{}");
        return path;
    }

    private PlayerDataService CreateServiceWithFile(string filePath)
    {
        typeof(PlayerDataService)
            .GetField("FilePath", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .SetValue(null, filePath);

        return new PlayerDataService();
    }

    [Fact]
    public void GetPreferences_ReturnsEmptyList_WhenUserNotFound()
    {
        string file = CreateTempPrefsFile();
        var service = CreateServiceWithFile(file);

        var prefs = service.GetPreferences(123);

        Assert.Empty(prefs);
    }

    [Fact]
    public void SetPreferences_SavesToFile()
    {
        string file = CreateTempPrefsFile();
        var service = CreateServiceWithFile(file);

        service.SetPreferences(1, new() { "A", "B" });

        string json = File.ReadAllText(file);
        Assert.Contains("A", json);
        Assert.Contains("B", json);
    }

    [Fact]
    public void SetPreferences_OverwritesExisting()
    {
        string file = CreateTempPrefsFile(@"{ ""1"": [""Old""] }");
        var service = CreateServiceWithFile(file);

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

        var service = CreateServiceWithFile(file);

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

        var service = CreateServiceWithFile(file);

        Assert.Empty(service.GetPreferences(1));
        Assert.False(File.Exists(file)); 
    }

    [Fact]
    public void MultipleUsers_AreStoredIndependently()
    {
        string file = CreateTempPrefsFile();
        var service = CreateServiceWithFile(file);

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
        var service = CreateServiceWithFile(file);

        service.SetPreferences(1, new() { "A" });

        string json = File.ReadAllText(file);

        Assert.Contains("\n", json); 
    }
}