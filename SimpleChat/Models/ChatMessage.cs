namespace SimpleChat.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string ChannelName { get; set; }
    public required string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
