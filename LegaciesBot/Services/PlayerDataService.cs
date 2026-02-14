using System.Text.Json;

public class PlayerDataService
{
    private readonly string _filePath;

    private Dictionary<ulong, List<string>> _prefs
        = new Dictionary<ulong, List<string>>();

    public PlayerDataService(string? filePath = null)
    {
        _filePath = filePath ?? "playerprefs.json";
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
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        _prefs = JsonSerializer.Deserialize<Dictionary<ulong, List<string>>>(json)
                 ?? new Dictionary<ulong, List<string>>();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_prefs, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}