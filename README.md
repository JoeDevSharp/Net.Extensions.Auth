# Net.Extensions.OAuth2

**Lightweight and extensible OAuth2 authentication framework for .NET 6+, designed to simplify integration with multiple providers and enable easy custom extensions.**

---

## Why use this framework?

- **Simplicity & Control:** No complex external servers or heavy libraries. Just pure .NET with HttpClient and your own rules.
- **Extensible:** Supports the most popular providers (Google, GitHub, Microsoft, Apple, Facebook, LinkedIn, Twitter, Keycloak) and allows you to easily create any custom provider.
- **Full OAuth2 Authorization Code Flow:** Includes PKCE, automatic authorization code capture via embedded local server, token management, and refresh handling.
- **Unified User Model:** Maps user info into a standard `AuthUser` model for straightforward consumption.
- **Compatible with Desktop, Web, and Service apps:** Designed for multi-scenario use with no external dependencies.

---

## How it works — Typical Authentication Flow

1. You create the provider with your client credentials and endpoint URLs.
2. Call `LoginAsync()`, which opens the browser to the provider login page.
3. The framework spins up a local HTTP server to catch the OAuth2 authorization code redirected back.
4. The code is automatically exchanged for an access token.
5. User information is retrieved and mapped into an `AuthUser` instance.
6. You can use the access token for API calls and the framework handles refreshing automatically if needed.
7. Call `LogoutAsync()` to clear session data.

---

## Supported Providers & Configuration

| Provider  | Default Scopes                       | Authorization URL                                                                                                                | Token URL                                                                                                                | User Info URL                                                                                                            |
| --------- | ------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------ |
| Google    | openid email profile                 | [https://accounts.google.com/o/oauth2/v2/auth](https://accounts.google.com/o/oauth2/v2/auth)                                     | [https://oauth2.googleapis.com/token](https://oauth2.googleapis.com/token)                                               | [https://openidconnect.googleapis.com/v1/userinfo](https://openidconnect.googleapis.com/v1/userinfo)                     |
| GitHub    | read\:user user\:email               | [https://github.com/login/oauth/authorize](https://github.com/login/oauth/authorize)                                             | [https://github.com/login/oauth/access_token](https://github.com/login/oauth/access_token)                               | [https://api.github.com/user](https://api.github.com/user)                                                               |
| Microsoft | user.read                            | [https://login.microsoftonline.com/common/oauth2/v2.0/authorize](https://login.microsoftonline.com/common/oauth2/v2.0/authorize) | [https://login.microsoftonline.com/common/oauth2/v2.0/token](https://login.microsoftonline.com/common/oauth2/v2.0/token) | [https://graph.microsoft.com/v1.0/me](https://graph.microsoft.com/v1.0/me)                                               |
| Apple     | name email                           | [https://appleid.apple.com/auth/authorize](https://appleid.apple.com/auth/authorize)                                             | [https://appleid.apple.com/auth/token](https://appleid.apple.com/auth/token)                                             | (User info contained in id_token JWT)                                                                                    |
| Facebook  | public_profile email                 | [https://www.facebook.com/v12.0/dialog/oauth](https://www.facebook.com/v12.0/dialog/oauth)                                       | [https://graph.facebook.com/v12.0/oauth/access_token](https://graph.facebook.com/v12.0/oauth/access_token)               | [https://graph.facebook.com/me?fields=id,name,email,picture](https://graph.facebook.com/me?fields=id,name,email,picture) |
| LinkedIn  | r_liteprofile r_emailaddress         | [https://www.linkedin.com/oauth/v2/authorization](https://www.linkedin.com/oauth/v2/authorization)                               | [https://www.linkedin.com/oauth/v2/accessToken](https://www.linkedin.com/oauth/v2/accessToken)                           | [https://api.linkedin.com/v2/me](https://api.linkedin.com/v2/me)                                                         |
| Twitter   | tweet.read users.read offline.access | [https://twitter.com/i/oauth2/authorize](https://twitter.com/i/oauth2/authorize)                                                 | [https://api.twitter.com/2/oauth2/token](https://api.twitter.com/2/oauth2/token)                                         | [https://api.twitter.com/2/users/me](https://api.twitter.com/2/users/me)                                                 |
| Keycloak  | openid profile email                 | Configurable per instance                                                                                                        | Configurable per instance                                                                                                | Configurable per instance                                                                                                |

_You can customize any provider by extending `CustomOAuth2Provider`._

---

## Basic Usage Example (Google)

```csharp
var provider = new GoogleProvider(
    clientId: "your-client-id",
    clientSecret: "your-client-secret",
    redirectUri: "http://localhost:60000/",
    scopes: new[] { "openid", "email", "profile" }
);

try
{
    var user = await provider.LoginAsync();

    if (user != null)
    {
        Console.WriteLine($"Authenticated user: {user.Username} - {user.Email}");
        // Here you can save tokens, call APIs with AccessToken, etc.
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Login failed: {ex.Message}");
}
```

> Note: `LoginAsync()` opens the browser and starts a local HTTP server to automatically capture the OAuth2 code.

---

## Creating a Custom Provider

For unsupported providers, extend `CustomOAuth2Provider` and override `GetUserInfoAsync` to adapt user data retrieval.

```csharp
public class MyCustomProvider : CustomOAuth2Provider
{
    public MyCustomProvider(OAuth2Options options) : base(options) { }

    public override async Task<AuthUser> GetUserInfoAsync(string accessToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = await client.GetStringAsync(Options.UserInfoEndpoint);
        var data = JsonDocument.Parse(json).RootElement;

        return new AuthUser
        {
            Id = data.GetProperty("id").GetString() ?? "",
            Username = data.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "",
            Email = data.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "",
            Picture = data.TryGetProperty("avatar", out var a) ? a.GetString() ?? "" : "",
            Roles = new List<string>(),
            Claims = data.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString())
        };
    }
}
```

---

## Important Interfaces and Models

### `IAuthProvider`

```csharp
public interface IAuthProvider
{
    bool IsAuthenticated { get; }
    AuthUser? CurrentUser { get; }
    Task<AuthUser?> LoginAsync();
    Task LogoutAsync();
}
```

### Core Models

- **OAuth2Options:** Client configuration, endpoints, and scopes.
- **OAuth2Token:** Access and refresh tokens, expiration, type, and scope.
- **AuthUser:** Authenticated user with Id, Username, Email, Picture, Roles, and Claims.

---

## Implementing the `IAuthProvider` Interface

Here is a simple full implementation example for a custom OAuth2 provider using `IAuthProvider`:

```csharp
public class SimpleCustomProvider : IAuthProvider
{
    private readonly OAuth2Options _options;
    private OAuth2Token? _token;
    public AuthUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public SimpleCustomProvider(OAuth2Options options)
    {
        _options = options;
    }

    public async Task<AuthUser?> LoginAsync()
    {
        // Build authorization URL with PKCE, scopes, clientId, redirectUri
        var authUrl = OAuth2Helper.BuildAuthorizationUrl(_options);

        // Open browser and start local server to capture code
        var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, _options.RedirectUri);

        if (string.IsNullOrEmpty(code))
            throw new Exception("Authorization code not received.");

        // Exchange code for token
        _token = await OAuth2Helper.ExchangeCodeForTokenAsync(code, _options);

        if (_token == null || string.IsNullOrEmpty(_token.AccessToken))
            throw new Exception("Failed to obtain access token.");

        // Get user info with token
        CurrentUser = await GetUserInfoAsync(_token.AccessToken);

        return CurrentUser;
    }

    public Task LogoutAsync()
    {
        _token = null;
        CurrentUser = null;
        return Task.CompletedTask;
    }

    private async Task<AuthUser> GetUserInfoAsync(string accessToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync(_options.UserInfoEndpoint);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonDocument.Parse(json).RootElement;

        return new AuthUser
        {
            Id = data.GetProperty("id").GetString() ?? "",
            Username = data.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "",
            Email = data.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "",
            Picture = data.TryGetProperty("picture", out var p) ? p.GetString() ?? "" : "",
            Roles = new List<string>(),
            Claims = data.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString())
        };
    }
}
```

### Usage example

```csharp
var options = new OAuth2Options
{
    ClientId = "your-client-id",
    ClientSecret = "your-client-secret",
    RedirectUri = "http://localhost:60000/",
    AuthorizationEndpoint = "https://provider.com/oauth2/authorize",
    TokenEndpoint = "https://provider.com/oauth2/token",
    UserInfoEndpoint = "https://provider.com/api/userinfo",
    Scopes = new[] { "openid", "profile", "email" }
};

var provider = new SimpleCustomProvider(options);

try
{
    var user = await provider.LoginAsync();
    Console.WriteLine($"User logged in: {user?.Username} ({user?.Email})");
}
catch (Exception ex)
{
    Console.WriteLine($"Login failed: {ex.Message}");
}
```

---

## Security & Best Practices

- **PKCE support:** protects OAuth2 flow in public and desktop apps.
- **Local server for redirect URI:** no external redirect URI setup needed.
- **Token refresh:** automatic handling to maintain session without frequent logins.
- **Error handling:** clear exceptions for easier debugging.
- **No automatic storage:** you control token persistence for your scenario.
- **No external dependencies:** pure .NET standard for maximum control and lightweight use.

---

## Requirements

- .NET 6 or higher
- System.Text.Json
- HttpClient
