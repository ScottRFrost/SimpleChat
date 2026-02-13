namespace SimpleChat.Client.Models;

public class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class UserSession
{
    public required string Username { get; set; }
    public required string Token { get; set; } // We might not use JWT yet, but good for future.
}
