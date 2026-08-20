using LegaciesBot.Core;
using LegaciesBot.Discord;
using NetCord.Rest;

namespace LegaciesBot.Services
{
    /// <summary>
    /// Keeps a single, always-current "Live Lobby" message in a dedicated channel and edits it
    /// in place as the lobby changes, so players don't have to spam <c>!lobby</c>.
    ///
    /// Design notes:
    /// - Polls the lobby and only edits when the roster actually changed (rate-limit safe).
    /// - On startup it adopts its own existing board message (found by the title marker) instead
    ///   of posting a duplicate, so bot restarts/deploys don't litter the channel.
    /// - If the message was deleted, the next refresh reposts and re-pins it.
    /// - Off entirely when the channel id is 0.
    /// </summary>
    public class LobbyBoardService
    {
        public const string BoardTitle = "🎯 Live Lobby";

        private readonly RestClient _rest;
        private readonly ulong _channelId;
        private readonly Func<Lobby> _getLobby;

        private ulong? _messageId;
        private bool _lookedForExisting;
        private string _lastSignature = "";

        public LobbyBoardService(RestClient rest, ulong channelId, Func<Lobby> getLobby)
        {
            _rest = rest;
            _channelId = channelId;
            _getLobby = getLobby;
        }

        public bool Enabled => _channelId != 0;

        public async Task RefreshAsync()
        {
            if (!Enabled)
                return;

            var lobby = _getLobby();
            var signature = Signature(lobby);
            if (signature == _lastSignature && _messageId != null)
                return;

            await EnsureMessageAsync(Render(lobby));
            _lastSignature = signature;
        }

        // Everything that changes what the board should show. Same signature => skip the edit.
        public static string Signature(Lobby l) =>
            $"{l.Players.Count}|{string.Join(',', l.Players.Select(p => p.DiscordId))}|{l.GameNumber}|{l.IsLocked}|{l.DraftStarted}";

        public static EmbedProperties Render(Lobby l)
        {
            if (l.Players.Count == 0)
            {
                return EmbedFactory.Info(BoardTitle,
                    "The lobby is empty. Type `!j` to start one.");
            }

            var lines = l.Players.Select((p, i) => $"`{i + 1,2}.` {p.DisplayName()}");
            string state = l.IsLocked ? " (drafting)" : "";
            string body = string.Join("\n", lines)
                + $"\n\n**{l.Players.Count}/16**{state}. Type `!j` to join, `!l` to leave.";

            return EmbedFactory.Info($"{BoardTitle} ({l.Players.Count}/16)", body);
        }

        private async Task EnsureMessageAsync(EmbedProperties embed)
        {
            if (_messageId == null && !_lookedForExisting)
            {
                _lookedForExisting = true;
                await TryAdoptExistingAsync();
            }

            if (_messageId != null)
            {
                try
                {
                    await _rest.ModifyMessageAsync(_channelId, _messageId.Value, m => m.WithEmbeds([embed]));
                    return;
                }
                catch
                {
                    // The board message was deleted — drop the id and repost below.
                    _messageId = null;
                }
            }

            var posted = await _rest.SendMessageAsync(_channelId,
                new MessageProperties().WithEmbeds([embed]));
            _messageId = posted.Id;

            try
            {
                await _rest.PinMessageAsync(_channelId, posted.Id);
            }
            catch
            {
                // Missing "Manage Messages" just means it isn't pinned — the board still works.
            }
        }

        private async Task TryAdoptExistingAsync()
        {
            try
            {
                int scanned = 0;
                await foreach (var message in _rest.GetMessagesAsync(_channelId))
                {
                    if (message.Embeds.Any(e => e.Title != null && e.Title.StartsWith(BoardTitle, StringComparison.Ordinal)))
                    {
                        _messageId = message.Id;
                        return;
                    }

                    if (++scanned >= 40)
                        return;
                }
            }
            catch
            {
                // Channel not readable — we'll just post a fresh board.
            }
        }
    }
}
