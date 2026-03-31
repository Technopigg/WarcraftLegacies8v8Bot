using System.Text.Json;

namespace LegaciesBot.Moderation
{
    public class ModerationService
    {
        private readonly string _filePath = "moderation.json";
        private readonly TimeSpan _defaultWarningDuration;
        private readonly int _warningThresholdForBan;

        private ModerationData _data;

        private readonly HashSet<ulong> _admins = new();
        private readonly HashSet<ulong> _mods = new();
        public List<ulong> GetAdmins()
        {
            return _admins.ToList();
        }

        public List<ulong> GetMods()
        {
            return _mods.ToList();
        }

        public ModerationService(TimeSpan defaultWarningDuration, int warningThresholdForBan)
        {
            _defaultWarningDuration = defaultWarningDuration;
            _warningThresholdForBan = warningThresholdForBan;

            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonSerializer.Deserialize<ModerationData>(json) ?? new ModerationData();
            }
            else
            {
                _data = new ModerationData();
                Save();
            }

            if (_data.Admins != null)
            {
                foreach (var id in _data.Admins)
                    _admins.Add(id);
            }

            if (_data.Mods != null)
            {
                foreach (var id in _data.Mods)
                    _mods.Add(id);
            }

            CleanupExpiredWarnings();
        }

        private void Save()
        {
            _data.Admins = _admins.ToList();
            _data.Mods = _mods.ToList();

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }

        private UserModerationEntry GetOrCreateUser(ulong userId)
        {
            if (!_data.Users.TryGetValue(userId, out var entry))
            {
                entry = new UserModerationEntry();
                _data.Users[userId] = entry;
            }

            return entry;
        }

        public bool AddWarning(ulong userId, ulong moderatorId, string reason, TimeSpan? durationOverride = null)
        {
            CleanupExpiredWarnings();

            var entry = GetOrCreateUser(userId);
            var now = DateTime.UtcNow;
            var duration = durationOverride ?? _defaultWarningDuration;

            entry.Warnings.Add(new WarningEntry
            {
                ModeratorId = moderatorId,
                Reason = reason,
                IssuedAtUtc = now,
                ExpiresAtUtc = duration > TimeSpan.Zero ? now.Add(duration) : null
            });

            Save();
            return AutoBanIfThresholdReached(userId, moderatorId, "Reached warning threshold");
        }

        public bool RemoveWarning(ulong userId, int index)
        {
            if (!_data.Users.TryGetValue(userId, out var entry))
                return false;

            CleanupExpiredWarningsFor(entry);

            if (index < 0 || index >= entry.Warnings.Count)
                return false;

            entry.Warnings.RemoveAt(index);
            Save();

            AutoUnbanIfBelowThreshold(userId);
            return true;
        }

        public void AddBan(ulong userId, ulong moderatorId, string reason)
        {
            var entry = GetOrCreateUser(userId);
            entry.Bans.Add(new BanEntry
            {
                ModeratorId = moderatorId,
                Reason = reason,
                IssuedAtUtc = DateTime.UtcNow
            });
            Save();
        }

        public bool RemoveBan(ulong userId)
        {
            if (!_data.Users.TryGetValue(userId, out var entry))
                return false;

            if (entry.Bans.Count == 0)
                return false;

            entry.Bans.Clear();
            Save();
            return true;
        }

        public bool IsBanned(ulong userId)
        {
            if (!_data.Users.TryGetValue(userId, out var entry))
                return false;

            return entry.Bans.Count > 0;
        }

        public IReadOnlyList<WarningEntry> GetActiveWarnings(ulong userId)
        {
            if (!_data.Users.TryGetValue(userId, out var entry))
                return Array.Empty<WarningEntry>();

            CleanupExpiredWarningsFor(entry);
            return entry.Warnings;
        }

        private bool AutoBanIfThresholdReached(ulong userId, ulong moderatorId, string reason)
        {
            var warnings = GetActiveWarnings(userId);
            if (warnings.Count >= _warningThresholdForBan && !IsBanned(userId))
            {
                AddBan(userId, moderatorId, reason);
                return true;
            }
            return false;
        }

        private void AutoUnbanIfBelowThreshold(ulong userId)
        {
            var warnings = GetActiveWarnings(userId);
            if (warnings.Count < _warningThresholdForBan)
            {
                RemoveBan(userId);
            }
        }

        private void CleanupExpiredWarnings()
        {
            foreach (var entry in _data.Users.Values)
            {
                CleanupExpiredWarningsFor(entry);
            }
            Save();
        }

        private void CleanupExpiredWarningsFor(UserModerationEntry entry)
        {
            var now = DateTime.UtcNow;
            entry.Warnings = entry.Warnings
                .Where(w => w.ExpiresAtUtc == null || w.ExpiresAtUtc > now)
                .ToList();
        }

        public bool IsAdmin(ulong userId)
        {
            return _admins.Contains(userId);
        }

        public bool IsModerator(ulong userId)
        {
            return _mods.Contains(userId) || _admins.Contains(userId);
        }

        public void AddAdmin(ulong userId)
        {
            _admins.Add(userId);
            Save();
        }

        public void AddModerator(ulong userId)
        {
            _mods.Add(userId);
            Save();
        }

        public void RemoveAdmin(ulong userId)
        {
            _admins.Remove(userId);
            Save();
        }

        public void RemoveModerator(ulong userId)
        {
            _mods.Remove(userId);
            Save();
        }
    }
}
