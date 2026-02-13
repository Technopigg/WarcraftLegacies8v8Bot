using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LegaciesBot.Services
{
    public class PermissionService
    {
        private string FilePath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "permissions.json"));

        public PermissionData Data { get; private set; }

        public PermissionService()
        {
            Load();
        }
        
        public PermissionService(string filePath)
        {
            FilePath = filePath;
            Load();
        }
        
        private void Load()
        {
            Console.WriteLine("=== PermissionService Debug ===");
            Console.WriteLine("Working directory: " + Directory.GetCurrentDirectory());
            Console.WriteLine("Permissions file path: " + FilePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            if (File.Exists(FilePath))
            {
                Console.WriteLine("permissions.json FOUND. Loading...");

                string json = File.ReadAllText(FilePath);
                Data = JsonSerializer.Deserialize<PermissionData>(json, options) ?? new PermissionData();
            }
            else
            {
                Console.WriteLine("permissions.json NOT FOUND. Creating new file...");
                Data = new PermissionData();
                Save();
            }

            Console.WriteLine("Loaded Admins: " + string.Join(", ", Data.Admins));
            Console.WriteLine("Loaded Mods: " + string.Join(", ", Data.Mods));
            Console.WriteLine("================================");
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