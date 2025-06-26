
using Net.Extensions.OAuth2;
using Net.Extensions.OAuth2.Providers;

namespace WinFormsAuthTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
           await Connect();
        }

        private async Task Connect()
        {
            //var provider = new GoogleProvider(
            //    "550344567807-kd6kvfomtl9kjr7j4ro1ba8dhgjf02ap.apps.googleusercontent.com",
            //    "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",
            //    "http://localhost:60000/"
            //    );

            //var provider = new GithubProvider(
            //    "Ov23li0UlNQ2NMmc2qd2",
            //    "4ebfae36af64935a9b04e01af64dc0a2f58c1098",
            //    "http://localhost:60000/"
            //);

            var provider = new MicrosoftProvider(
                clientId: "f8bcc3f3-eca7-41f8-afe2-6b0dcd9335be",
                clientSecret: "AM_8Q~AubxG_ejpElOEJwiXxvifXDunYLq3eXcik",
                redirectUri: "http://localhost:60000/",
                scopes: new[] { "openid", "profile", "email" }
            );

            AuthContext.RegisterProvider(provider);

            var user = await AuthContext.LoginAsync();

            if (user != null)
                Console.WriteLine($"Bienvenido {user.Username} ({user.Email})");
            else
                Console.WriteLine("Falló la autenticación.");
        }
    }
}
