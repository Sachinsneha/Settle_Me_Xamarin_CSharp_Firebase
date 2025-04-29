using System;
using Xamarin.Forms;
using sattleme.Services;
using sattleme.Home.HomePage;
using sattleme.Home.HomePages;

namespace sattleme.auth
{
    public partial class LoginPage : ContentPage
    {
        FirebaseAuthService authService = FirebaseAuthService.Instance;
        SessionService sessionService = new SessionService();

        public LoginPage()
        {
            InitializeComponent();
        }

        async void OnLoginClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Validation Error", "Both fields are required.", "OK");
                return;
            }
            if (!email.Contains("@"))
            {
                await DisplayAlert("Validation Error", "Enter a valid email address.", "OK");
                return;
            }

            try
            {
                var uid = await authService.LoginUserAsync(email, password);

                await sessionService.SaveUserSessionAsync(uid);

                Application.Current.MainPage = new NavigationPage(new HomePage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        async void OnRegisterButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }

        async void OnForgotPasswordButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ForgotPasswordPage());
        }
    }
}
