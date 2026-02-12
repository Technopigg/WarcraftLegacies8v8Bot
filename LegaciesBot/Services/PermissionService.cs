using System.Text.Json;

namespace LegaciesBot.Services
{
    public class PermissionService
    {
        private const string FilePath = "permissions.json";

        public PermissionData Data { get; private set; }

        public PermissionService()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                Data = JsonSerializer.Deserialize<PermissionData>(json) ?? new PermissionData();
            }
            else
            {
                Data = new PermissionData();
                Save();
            }
        }

        public bool IsAdmin(ulong userId)
        {
            return Data.Admins.Contains(userId);
        }

        public bool IsMod(ulong userId)
        {
            return Data.Mods.Contains(userId) || IsAdmin(userId);
        }
        
        public void AddMod(ulong userId)
        {
            if (!Data.Mods.Contains(userId))
            {
                Data.Mods.Add(userId);
                Save();
            }
        }

        public void RemoveMod(ulong userId)
        {
            if (Data.Mods.Contains(userId))
            {
                Data.Mods.Remove(userId);
                Save();
            }
        }

        public void AddAdmin(ulong userId)
        {
            if (!Data.Admins.Contains(userId))
            {
                Data.Admins.Add(userId);
                Save();
            }
        }

        public void RemoveAdmin(ulong userId)
        {
            if (Data.Admins.Contains(userId))
            {
                Data.Admins.Remove(userId);
                Save();
            }
        }
        
        public void Save()
        {
            string json = JsonSerializer.Serialize(Data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }
    }
    
    public class PermissionData
    {
        public List<ulong> Admins { get; set; } = new();
        public List<ulong> Mods { get; set; } = new();
    }
}