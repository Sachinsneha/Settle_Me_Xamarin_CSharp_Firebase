using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using sattleme.Services;
using sattleme.auth.Models;
using sattleme.Home.Rental;
using sattleme.Home.chat;
using sattleme.Home.HomePage;
using sattleme.Home.HomePages;

namespace sattleme.Home.Profile
{
    public partial class ProfilePage : ContentPage
    {
        FirebaseAuthService authService = FirebaseAuthService.Instance;

        public ProfilePage()
        {
            InitializeComponent();
            LoadProfile();
        }


        async void LoadProfile()
        {
            var currentUser = authService.CurrentUser;
            if (currentUser != null && !string.IsNullOrEmpty(authService.IdToken))
            {
                try
                {
                    var userData = await FirebaseFirestoreService.GetDocumentAsync(
                        "users", currentUser.Uid, authService.IdToken);

                    if (userData != null)
                    {
                        NameLabel.Text = userData.ContainsKey("fullName") ? userData["fullName"].ToString() : "No Name";
                        EmailLabel.Text = userData.ContainsKey("email") ? userData["email"].ToString() : "No Email";

                        if (userData.ContainsKey("photoUrl"))
                        {
                            string photoUrl = userData["photoUrl"].ToString();
                            Console.WriteLine($"🔥 Debug: Loaded photoUrl = {photoUrl}");

                            if (!string.IsNullOrWhiteSpace(photoUrl))
                            {
                                ProfileImage.Source = ImageSource.FromUri(new Uri(photoUrl));
                            }
                            else
                            {
                                ProfileImage.Source = "default_profile.png"; 
                            }
                        }
                        else
                        {
                            ProfileImage.Source = "default_profile.png";
                        }
                    }
                    else
                    {
                        NameLabel.Text = currentUser.DisplayName ?? "No Name";
                        EmailLabel.Text = currentUser.Email ?? "No Email";
                        ProfileImage.Source = "default_profile.png";
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error loading profile: {ex.Message}", "OK");
                }
            }
            else
            {
                NameLabel.Text = "Not logged in";
                EmailLabel.Text = "";
                ProfileImage.Source = "default_profile.png";
            }
        }
      
        async void OnUploadPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var file = await MediaPicker.PickPhotoAsync();
                if (file != null)
                {
                    var user = authService.CurrentUser;
                    if (user == null || string.IsNullOrEmpty(authService.IdToken))
                    {
                        await DisplayAlert("Error", "User not authenticated", "OK");
                        return;
                    }

                    using (var stream = await file.OpenReadAsync())
                    {
                        string fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName)}";
                        string downloadUrl = await FirebaseStorageService.UploadFileAsync(
                            "profilePictures",
                            user.Uid,
                            fileName,
                            stream,
                            authService.IdToken 
                        );

                        var updateData = new Dictionary<string, object>
                {
                    { "photoUrl", downloadUrl }
                };

                        await FirebaseFirestoreService.UpdateDocumentAsync(
                            "users",
                            user.Uid,
                            updateData,
                            authService.IdToken
                        );

                        ProfileImage.Source = downloadUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error uploading photo: {ex.Message}", "OK");
            }
        }

        async void OnHomeTapped(object sender, EventArgs e) => await Navigation.PushAsync(new HomePages.HomePage());
        async void OnChatTapped(object sender, EventArgs e) => await Navigation.PushAsync(new ChatListPage());
        async void OnAddTapped(object sender, EventArgs e) => await Navigation.PushAsync(new UploadRental());
        async void OnFavoriteTapped(object sender, EventArgs e) => await Navigation.PushAsync(new NotificationsPage());
        async void OnProfileTapped(object sender, EventArgs e) => await Navigation.PushAsync(new ProfilePage());

     
        async void OnLogoutClicked(object sender, EventArgs e)
        {
            authService.CurrentUser = null;
            authService.IdToken = null;
            Application.Current.MainPage = new NavigationPage(new sattleme.auth.LoginPage());
        }
    }
}
