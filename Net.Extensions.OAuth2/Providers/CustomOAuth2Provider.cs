using Net.Extensions.OAuth2.Interfaces;
using Net.Extensions.OAuth2.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Net.Extensions.OAuth2.Providers
{
	// Clase base reusable para cualquier proveedor OAuth2 estándar
	public abstract class CustomOAuth2Provider : IAuthProvider
	{
		protected OAuth2Options Options { get; }

		protected OAuth2Token? Token;
		protected AuthUser? User;

		public bool IsAuthenticated => Token != null;
		public AuthUser? CurrentUser => User;

		protected CustomOAuth2Provider(OAuth2Options options)
		{
			Options = options;
		}
        public OAuth2Token? Token => _token;


        // Flujo general para login
        public virtual async Task<AuthUser?> LoginAsync()
		{
			var authUrl = BuildAuthorizationUrl();
			var code = await OAuth2Helper.GetCodeViaLocalServerAsync(authUrl, Options.RedirectUri);
			Token = await ExchangeCodeForTokenAsync(code);
			User = await GetUserInfoAsync(Token.AccessToken);
			return User;
		}

		// Construye la URL de autorización con parámetros comunes
		protected virtual string BuildAuthorizationUrl()
		{
			var scope = Uri.EscapeDataString(string.Join(" ", Options.Scopes));
			var url = $"{Options.AuthorizationEndpoint}?" +
				$"response_type=code" +
				$"&client_id={Uri.EscapeDataString(Options.ClientId)}" +
				$"&redirect_uri={Uri.EscapeDataString(Options.RedirectUri)}" +
				$"&scope={scope}";
			return url;
		}

		// Intercambia código por token
		protected virtual async Task<OAuth2Token> ExchangeCodeForTokenAsync(string code)
		{
			using var client = new HttpClient();
			var body = new Dictionary<string, string>
			{
				["grant_type"] = "authorization_code",
				["code"] = code,
				["redirect_uri"] = Options.RedirectUri,
				["client_id"] = Options.ClientId,
			};

			if (!string.IsNullOrWhiteSpace(Options.ClientSecret))
			{
				body["client_secret"] = Options.ClientSecret;
			}

			var content = new FormUrlEncodedContent(body);
			var response = await client.PostAsync(Options.TokenEndpoint, content);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<OAuth2Token>(json)!;
		}

		// Obtiene la info del usuario; se debe implementar según proveedor
		public abstract Task<AuthUser> GetUserInfoAsync(string accessToken);

		public virtual Task LogoutAsync()
		{
			Token = null;
			User = null;
			return Task.CompletedTask;
		}
	}
}
