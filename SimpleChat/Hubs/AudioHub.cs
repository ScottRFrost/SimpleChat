using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SimpleChat.Data;
using SimpleChat.Models;

namespace SimpleChat.Hubs;

public class AudioHub : Hub
{
    private readonly ApplicationDbContext _db;
    private static readonly Dictionary<string, HashSet<string>> _textChannels = new() { { "general", new() } };
    private static readonly Dictionary<string, HashSet<string>> _voiceChannels = new() { { "Lobby", new() } };

    public AudioHub(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task JoinTextChannel(string channelName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"text_{channelName}");
        lock (_textChannels)
        {
            if (!_textChannels.ContainsKey(channelName))
                _textChannels[channelName] = new HashSet<string>();
            _textChannels[channelName].Add(Context.ConnectionId);
        }
        await BroadcastChannels();

        // Load history and send to caller
        var history = await _db.ChatMessages
            .Where(m => m.ChannelName == channelName)
            .OrderByDescending(m => m.Timestamp)
            .Take(250)
            .OrderBy(m => m.Timestamp)
            .Select(m => new { m.Username, m.ChannelName, m.Content, m.Timestamp })
            .ToListAsync();

        await Clients.Caller.SendAsync("ReceiveChatHistory", channelName, history);
    }

    public async Task JoinVoiceChannel(string channelName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"voice_{channelName}");
        lock (_voiceChannels)
        {
            if (!_voiceChannels.ContainsKey(channelName))
                _voiceChannels[channelName] = new HashSet<string>();
            _voiceChannels[channelName].Add(Context.ConnectionId);
        }
        await Clients.OthersInGroup($"voice_{channelName}").SendAsync("ReceiveSignal", Context.ConnectionId, "JOIN");
        await BroadcastChannels();
    }

    public async Task LeaveVoiceChannel(string channelName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice_{channelName}");
        lock (_voiceChannels)
        {
            if (_voiceChannels.ContainsKey(channelName))
            {
                _voiceChannels[channelName].Remove(Context.ConnectionId);
                if (_voiceChannels[channelName].Count == 0 && channelName != "Lobby")
                    _voiceChannels.Remove(channelName);
            }
        }
        await BroadcastChannels();
    }

    public async Task SendSignal(string remoteId, string signal, string channelName)
    {
        if (remoteId == "all")
        {
            await Clients.OthersInGroup($"voice_{channelName}").SendAsync("ReceiveSignal", Context.ConnectionId, signal);
        }
        else
        {
            await Clients.Client(remoteId).SendAsync("ReceiveSignal", Context.ConnectionId, signal);
        }
    }

    public async Task SendChatMessage(string channelName, string message, string userName)
    {
        var chatMsg = new ChatMessage
        {
            ChannelName = channelName,
            Content = message,
            Username = userName,
            Timestamp = DateTime.UtcNow
        };

        _db.ChatMessages.Add(chatMsg);
        
        // Keep only last 250
        var count = await _db.ChatMessages.CountAsync(m => m.ChannelName == channelName);
        if (count >= 250)
        {
            var toDelete = await _db.ChatMessages
                .Where(m => m.ChannelName == channelName)
                .OrderBy(m => m.Timestamp)
                .Take(count - 249)
                .ToListAsync();
            _db.ChatMessages.RemoveRange(toDelete);
        }

        await _db.SaveChangesAsync();

        await Clients.Group($"text_{channelName}").SendAsync("ReceiveChatMessage", userName, channelName, message, chatMsg.Timestamp);
    }

    public async Task CreateChannel(string name, bool isVoice)
    {
        if (isVoice)
        {
            lock (_voiceChannels) if (!_voiceChannels.ContainsKey(name)) _voiceChannels[name] = new();
        }
        else
        {
            lock (_textChannels) if (!_textChannels.ContainsKey(name)) _textChannels[name] = new();
        }
        await BroadcastChannels();
    }

    private async Task BroadcastChannels()
    {
        await Clients.All.SendAsync("UpdateChannels", _textChannels.Keys.ToList(), _voiceChannels.Keys.ToList());
    }

    public override async Task OnConnectedAsync()
    {
        await BroadcastChannels();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_textChannels)
        {
            foreach (var channel in _textChannels.Keys.ToList()) _textChannels[channel].Remove(Context.ConnectionId);
        }
        lock (_voiceChannels)
        {
            foreach (var channel in _voiceChannels.Keys.ToList()) _voiceChannels[channel].Remove(Context.ConnectionId);
        }
        await BroadcastChannels();
        await base.OnDisconnectedAsync(exception);
    }
}
