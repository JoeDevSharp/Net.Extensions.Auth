Sure, here’s a clear and concise README in English for your OAuth2 framework:

---

# Net.Extensions.OAuth2

Lightweight and extensible OAuth2 authentication framework for .NET, supporting multiple popular providers and enabling easy custom provider implementation.

---

## Features

- Built-in support for **Google**, **GitHub**, **Microsoft**, **Apple**, **Facebook**, **LinkedIn**, **Twitter**, **Keycloak**.
- OAuth2 authorization code flow with PKCE and token refresh handling.
- Standardized user info mapping to `AuthUser`.
- Common interface `IAuthProvider` for unified handling.
- Abstract base class `CustomOAuth2Provider` to simplify custom provider creation.
- Compatible with .NET 6+ and uses standard HttpClient.

---

## Supported Providers

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

---

## Basic usage example with GoogleProvider

```csharp
var provider = new GoogleProvider(
    clientId: "your-client-id",
    clientSecret: "your-client-secret",
    redirectUri: "http://localhost:60000/",
    scopes: new[] { "openid", "email", "profile" }
);

var user = await provider.LoginAsync();

Console.WriteLine($"Authenticated user: {user?.Username} - {user?.Email}");
```

---

## Creating a Custom Provider

To support any other provider, extend the `CustomOAuth2Provider` base class and override the `GetUserInfoAsync` method.

### Simple example

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

## `IAuthProvider` Interface

```csharp
public interface IAuthProvider
{
    bool IsAuthenticated { get; }
    AuthUser? CurrentUser { get; }
    Task<AuthUser?> LoginAsync();
    Task LogoutAsync();
}
```

---

## Main Models

### OAuth2Options

- `ClientId`
- `ClientSecret`
- `RedirectUri`
- `AuthorizationEndpoint`
- `TokenEndpoint`
- `UserInfoEndpoint`
- `Scopes`

### OAuth2Token

- `AccessToken`
- `RefreshToken`
- `Scope`
- `TokenType`
- `ExpiresAt`

### AuthUser

- `Id`
- `Username`
- `Email`
- `Picture`
- `Roles`
- `Claims`

---

## Helpers

- `OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, redirectUri)`
  Runs a local HTTP server to catch authorization code from redirect.

- `OAuth2Helper.ExchangeCodeForTokenAsync(code, options)`
  Exchanges authorization code for OAuth2 token.

---

## Requirements

- .NET 6 or higher
- `System.Text.Json`
- `HttpClient`

---

## License

MIT — free to use and modify.

---

If you want, I can help prepare a GitHub repo with ready-to-use examples and tests. Want me to do that?
