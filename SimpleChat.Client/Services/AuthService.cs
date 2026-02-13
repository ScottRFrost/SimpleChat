using System.Net.Http.Json;
using Blazored.LocalStorage;
using SimpleChat.Client.Models;

namespace SimpleChat.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string SessionKey = "user_session";

    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public async Task<bool> Register(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Login(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        if (response.IsSuccessStatusCode)
        {
            var session = await response.Content.ReadFromJsonAsync<UserSession>();
            if (session != null)
            {
                if (OperatingSystem.IsBrowser())
                {
                    try
                    {
                        await _localStorage.SetItemAsync(SessionKey, session);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                OnAuthStateChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public async Task Logout()
    {
        if (OperatingSystem.IsBrowser())
        {
            try
            {
                await _localStorage.RemoveItemAsync(SessionKey);
            }
            catch
            {
                // Ignore if localStorage is unavailable
            }
        }
        OnAuthStateChanged?.Invoke();
    }

    public async Task<UserSession?> GetSession()
    {
        if (!OperatingSystem.IsBrowser())
        {
            return null;
        }

        try
        {
            return await _localStorage.GetItemAsync<UserSession>(SessionKey);
        }
        catch
        {
            return null;
        }
    }
}
