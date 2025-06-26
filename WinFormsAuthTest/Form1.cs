
using Net.Extensions.OAuth2;
using Net.Extensions.OAuth2.Interfaces;
using Net.Extensions.OAuth2.Models;
using Net.Extensions.OAuth2.Providers;
using Net.Extensions.OAuth2.Providers.Net.Extensions.OAuth2.Providers;

namespace WinFormsAuthTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async Task<AuthUser?> Connect(IAuthProvider provider)
        {
            AuthContext.RegisterProvider(provider);

            var user = await AuthContext.LoginAsync();

            if (user is null)
            {
                return null;
            }

            return user;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var provider = new GoogleProvider(
                "550344567807-kd6kvfomtl9kjr7j4ro1ba8dhgjf02ap.apps.googleusercontent.com",
                "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",
                "http://localhost:60000/"
                );
            var user = await Connect(provider);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var provider = new GithubProvider(
                "Ov23li0UlNQ2NMmc2qd2",
                "4ebfae36af64935a9b04e01af64dc0a2f58c1098",
                "http://localhost:60000/"
            );

            var user = await Connect(provider);
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            var provider = new MicrosoftProvider(
                clientId: "f8bcc3f3-eca7-41f8-afe2-6b0dcd9335be",
                clientSecret: "AM_8Q~AubxG_ejpElOEJwiXxvifXDunYLq3eXcik",
                redirectUri: "http://localhost:60000/",
                scopes: new[] { "openid", "profile", "email" }
            );

            var user = await Connect(provider);
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            var provider = new AppleProvider(
               clientId: "f8bcc3f3-eca7-41f8-afe2-6b0dcd9335be",
               clientSecret: "AM_8Q~AubxG_ejpElOEJwiXxvifXDunYLq3eXcik",
               redirectUri: "http://localhost:60000/",
               scopes: new[] { "openid", "profile", "email" }
           );

            var user = await Connect(provider);
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            var provider = new FacebookProvider(
               clientId: "584342851386955",
               clientSecret: "ddf72d99ee4cc4b808e7eed4d5f0d55c",
               redirectUri: "http://localhost:60000/",
               scopes: new[] { "public_profile", "email" }
           );

            var user = await Connect(provider);
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            var provider = new LinkedInProvider(
               clientId: "78v0lkqnqimcap",
               clientSecret: "WPL_AP1.Z3inVkxugyZDCEkJ./CwMHA==",
               redirectUri: "http://localhost:60000/",
               scopes: new[] { "r_liteprofile", "r_emailaddress" }
           );

            var user = await Connect(provider);
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            var provider = new KeycloakProvider(
                baseUrl: "http://localhost:60000/",
                realm: "test",
                clientId: "78v0lkqnqimcap",
                clientSecret: "WPL_AP1.Z3inVkxugyZDCEkJ./CwMHA==",
                redirectUri: "http://localhost:60000/",
                scopes: new[] { "r_liteprofile", "r_emailaddress" }
           );

            var user = await Connect(provider);
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            var provider = new TwitterProvider(
                clientId: "R2lKWk9xam00dFVHWXNRbWE4SjQ6MTpjaQ",
                clientSecret: "bbXTBKWfIZO4F-zrYAOwikV8f9ISlZQTvlOPW6Pe4dOfmUxjP0",
                redirectUri: "http://localhost:60000/",
                scopes: new[] { "users.read"}
            );

            var user = await Connect(provider);
        }
    }
}
