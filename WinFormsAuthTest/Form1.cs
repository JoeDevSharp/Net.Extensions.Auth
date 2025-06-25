using Net.Extensions.Auth.Core;
using Net.Extensions.Auth.Providers.OAuth2;
using System.Threading.Tasks;

namespace WinFormsAuthTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Connect().Wait();    
        }

        private async Task Connect()
        {
            var provider = new OAuth2Provider(new OAuth2Options
            {
                ClientId = "550344567807-kd6kvfomtl9kjr7j4ro1ba8dhgjf02ap.apps.googleusercontent.com",
                ClientSecret = "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",
                RedirectUri = "http://localhost:54321/",
                AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
                TokenEndpoint = "https://oauth2.googleapis.com/token",
                UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo",
                Scopes = new[] { "openid", "email", "profile" }
            });

            AuthContext.RegisterProvider(provider);

            var user = await AuthContext.LoginAsync();

            if (user != null)
                Console.WriteLine($"Bienvenido {user.Username} ({user.Email})");
            else
                Console.WriteLine("Falló la autenticación.");
        }
    }
}
