using System.Text.Json;

public class PlayerDataService
{
    private const string FilePath = "playerprefs.json";

    private Dictionary<ulong, List<string>> _prefs 
        = new Dictionary<ulong, List<string>>();

    public PlayerDataService()
    {
        Load();
    }

    public List<string> GetPreferences(ulong discordId)
    {
        return _prefs.TryGetValue(discordId, out var prefs)
            ? prefs
            : new List<string>();
    }

    public void SetPreferences(ulong discordId, List<string> prefs)
    {
        _prefs[discordId] = prefs;
        Save();
    }

    private void Load()
    {
        if (!File.Exists(FilePath))
            return;

        var json = File.ReadAllText(FilePath);
        _prefs = JsonSerializer.Deserialize<Dictionary<ulong, List<string>>>(json)
                 ?? new Dictionary<ulong, List<string>>();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_prefs, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(FilePath, json);
    }
}