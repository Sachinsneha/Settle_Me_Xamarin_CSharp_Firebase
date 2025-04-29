using System;
using System.Collections.Generic;
using Xamarin.Forms;
using sattleme.Services;  // Ensure FirebaseFirestoreService & FirebaseAuthService are in this namespace

namespace sattleme.Home.Agents
{
    public partial class BecomeAgentPage : ContentPage
    {
        bool _isLoading = false;
        // Field to hold the user's photo URL.
        private string _photoUrl = "";

        public BecomeAgentPage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        // Loads the currently authenticated user info and pre-fills the read-only fields.
        private async void LoadUserInfo()
        {
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user != null)
            {
                // Always set the email since it's available.
                EmailEntry.Text = user.Email ?? "";

                // Check if FirebaseAuth profile has a display name and photo URL.
                if (!string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    NameEntry.Text = user.DisplayName;
                    if (user.PhotoUrl != null && !string.IsNullOrWhiteSpace(user.PhotoUrl.ToString()))
                    {
                        try
                        {
                            _photoUrl = user.PhotoUrl.ToString();
                            ProfileImage.Source = ImageSource.FromUri(new Uri(_photoUrl));
                        }
                        catch (Exception)
                        {
                            _photoUrl = "";
                            ProfileImage.Source = "default_profile.png";
                        }
                    }
                    else
                    {
                        _photoUrl = "";
                        ProfileImage.Source = "default_profile.png";
                    }
                }
                else
                {
                    // Otherwise, try loading from Firestore "users" collection.
                    try
                    {
                        string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                        var userData = await FirebaseFirestoreService.GetDocumentAsync("users", user.Uid, idToken);
                        if (userData != null)
                        {
                            NameEntry.Text = userData.ContainsKey("fullName") ? userData["fullName"].ToString() : "";
                            var photoUrlStr = userData.ContainsKey("photoUrl") ? userData["photoUrl"].ToString() : "";
                            if (!string.IsNullOrWhiteSpace(photoUrlStr))
                            {
                                try
                                {
                                    _photoUrl = photoUrlStr;
                                    ProfileImage.Source = ImageSource.FromUri(new Uri(photoUrlStr));
                                }
                                catch (Exception)
                                {
                                    _photoUrl = "";
                                    ProfileImage.Source = "default_profile.png";
                                }
                            }
                            else
                            {
                                _photoUrl = "";
                                ProfileImage.Source = "default_profile.png";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error fetching user data from Firestore: " + ex.Message);
                        _photoUrl = "";
                        ProfileImage.Source = "default_profile.png";
                    }
                }
            }
        }

        // Handles the submission of the agent application.
        async void OnSubmitAgentApplicationClicked(object sender, EventArgs e)
        {
            // Validate required fields.
            if (string.IsNullOrWhiteSpace(SpecialityEntry.Text) ||
                string.IsNullOrWhiteSpace(RateEntry.Text) ||
                string.IsNullOrWhiteSpace(ContactEntry.Text) ||
                string.IsNullOrWhiteSpace(BioEditor.Text))
            {
                await DisplayAlert("Validation Error", "All fields are required", "OK");
                return;
            }

            _isLoading = true;
            try
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user == null)
                {
                    await DisplayAlert("Error", "User not authenticated", "OK");
                    return;
                }

                // Get a valid ID token from your FirebaseAuth service.
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();

                // Prepare the agent application data.
                var agentData = new Dictionary<string, object>
                {
                    { "fullName", NameEntry.Text },
                    { "email", EmailEntry.Text },
                    { "speciality", SpecialityEntry.Text },
                    { "rate", RateEntry.Text },
                    { "contact", ContactEntry.Text },
                    { "bio", BioEditor.Text },
                    // Use the captured _photoUrl (which might be an empty string if not available)
                    { "photoUrl", _photoUrl },
                    { "userId", user.Uid },
                    { "timestamp", DateTime.UtcNow },
                    { "status", "pending" }
                };

                // Save the application data to Firestore under the "agents" collection.
                // Using the user's UID as the document ID ensures only one application per user.
                await FirebaseFirestoreService.AddDocumentAsync("agents", user.Uid, agentData, idToken);
                await DisplayAlert("Success", "Application submitted. We will contact you soon.", "OK");

                // Clear all fields except Name and Email.
                SpecialityEntry.Text = "";
                RateEntry.Text = "";
                ContactEntry.Text = "";
                BioEditor.Text = "";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error submitting application: " + ex.Message, "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }
    }
}
