using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Forms;
using Xamarin.Essentials;

namespace sattleme.auth
{
    public partial class ForgotPasswordPage : ContentPage
    {
        private const string FirebaseApiKey = "AIzaSyDSeiyWAT0rvn3mD1C8bbsb-oqYOK-QQQ0";

        public ForgotPasswordPage()
        {
            InitializeComponent();
        }

        async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                await DisplayAlert("Error", "Please enter your email address.", "OK");
                return;
            }

            try
            {
                await SendPasswordResetEmailAsync(email);
                await DisplayAlert("Success", "Password reset email sent. Please check your email.", "OK");

                if (Device.RuntimePlatform == Device.Android)
                {
                    try
                    {
                        await Launcher.OpenAsync(new Uri("googlegmail://co"));
                    }
                    catch (Exception)
                    {
                        await Launcher.OpenAsync($"mailto:{email}");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to send reset email: {ex.Message}", "OK");
            }
        }

        async Task SendPasswordResetEmailAsync(string email)
        {
            var requestUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={FirebaseApiKey}";

            var payload = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);

            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception("Error sending reset email: " + errorContent);
                }
            }
        }

        async void OnGoToLoginButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync(animated: true);
        }
    }
}
