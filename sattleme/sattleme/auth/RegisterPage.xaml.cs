using System;
using System.Collections.Generic;
using Xamarin.Forms;
using sattleme.Services;
using sattleme.Home.HomePage;
using sattleme.Home.HomePages;

namespace sattleme.auth
{
    public partial class RegisterPage : ContentPage
    {
        FirebaseAuthService authService = FirebaseAuthService.Instance;
        SessionService sessionService = new SessionService();

        public RegisterPage()
        {
            InitializeComponent();
        }

        async void OnRegisterClicked(object sender, EventArgs e)
        {
            string fullName = FullNameEntry.Text?.Trim();
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            // Simple validations.
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Validation Error", "All fields are required.", "OK");
                return;
            }
            if (!email.Contains("@"))
            {
                await DisplayAlert("Validation Error", "Enter a valid email address.", "OK");
                return;
            }
            if (password.Length < 6)
            {
                await DisplayAlert("Validation Error", "Password should be at least 6 characters long.", "OK");
                return;
            }

            try
            {
                // Register the user (Firebase Auth).
                var uid = await authService.RegisterUserAsync(fullName, email, password);
                var user = FirebaseAuthService.Instance.CurrentUser;
                // Save session if needed.
                await sessionService.SaveUserSessionAsync(uid);

                // Now store user details (without photo) in Firestore.
                // We have an ID token in authService.IdToken, and user details in authService.CurrentUser.
                var userData = new Dictionary<string, object>
                {
                    { "uid", authService.CurrentUser.Uid },
                    { "fullName", authService.CurrentUser.DisplayName },
                    { "email", authService.CurrentUser.Email },
                    { "photoUrl", "" } // No photo yet
                };

                await FirebaseFirestoreService.AddDocumentAsync("users", user.Uid, userData, FirebaseAuthService.Instance.IdToken);

                // Navigate to HomePage or wherever you want.
                Application.Current.MainPage = new NavigationPage(new HomePage());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        async void OnGoToLoginButtonClicked(object sender, EventArgs e)
        {
            // Navigate back to the Login screen.
            await Navigation.PopAsync();
        }
    }
}
