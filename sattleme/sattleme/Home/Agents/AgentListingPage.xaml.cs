using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using sattleme.Services;
using sattleme.Home.chat;
using sattleme.Home.Rental;
using sattleme.Home.Profile; 

namespace sattleme.Home.Agents
{
    public partial class AgentListingPage : ContentPage
    {
        string currentUserId = "";
        public AgentListingPage()
        {
            InitializeComponent();
            LoadApprovedAgents();
        }

        private async void OnHomeTapped(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new HomePages.HomePage());
        }

        private async void OnChatTapped(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new ChatListPage());
        }

        private async void OnAddTapped(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new UploadRental());
        }

        private async void OnFavoriteTapped(object sender, System.EventArgs e)
        {
            await DisplayAlert("Navigation", "Favorite tapped", "OK");
        }

        private async void OnProfileTapped(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }

        // Loads approved agent documents from Firestore.
        async void LoadApprovedAgents()
        {
            try
            {
                var currentUser = FirebaseAuthService.Instance.CurrentUser;
                if (currentUser != null)
                {
                    currentUserId = currentUser.Uid;
                }

                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                // Query documents from the "agents" collection where "status" equals "approved"
                var docs = await FirebaseFirestoreService.QueryDocumentsAsync("agents", "status", "approved", idToken);

                if (docs == null || docs.Count == 0)
                {
                    await DisplayAlert("Info", "No approved agents found.", "OK");
                    AgentsCollectionView.ItemsSource = null;
                    return;
                }

                var agentList = new List<AgentListing>();

                foreach (var data in docs)
                {
                    string documentId = data.ContainsKey("documentId") ? data["documentId"].ToString() : "";
                    string photoUrl = data.ContainsKey("photoUrl") &&
                                      !string.IsNullOrWhiteSpace(data["photoUrl"].ToString())
                                      ? data["photoUrl"].ToString()
                                      : "default_profile.png";
                    string fullName = data.ContainsKey("fullName") &&
                                      !string.IsNullOrWhiteSpace(data["fullName"].ToString())
                                      ? data["fullName"].ToString()
                                      : "Unknown";
                    string speciality = data.ContainsKey("speciality") ? data["speciality"].ToString() : "";
                    string rate = data.ContainsKey("rate") ? data["rate"].ToString() : "";
                    // Load bio from Firestore
                    string bio = data.ContainsKey("bio") ? data["bio"].ToString() : "";
                    string userId = data.ContainsKey("userId") ? data["userId"].ToString() : "";

                    agentList.Add(new AgentListing
                    {
                        Id = documentId,
                        FullName = fullName,
                        PhotoUrl = photoUrl,
                        Speciality = speciality,
                        Rate = rate,
                        Bio = bio,
                        UserId = userId,
                        IsOwner = (userId == currentUserId)
                    });
                }

                AgentsCollectionView.ItemsSource = agentList;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error loading agents: " + ex.Message, "OK");
            }
        }

        // Called when an agent is selected from the list.
        async void OnAgentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedAgent = e.CurrentSelection.FirstOrDefault() as AgentListing;
            if (selectedAgent != null)
            {
                // Navigate to the AgentDetailsPage, passing the selected agent.
                await Navigation.PushAsync(new AgentDetailsPage(selectedAgent));
                // Clear the selection.
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        // Navigates to the BecomeAgentPage.
        async void OnBecomeAgentButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BecomeAgentPage());
        }

        // Handles deletion of an agent listing.
        async void OnDeleteAgentClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var agent = button.BindingContext as AgentListing;
            if (agent != null)
            {
                bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete your agent listing?", "Yes", "No");
                if (confirm)
                {
                    try
                    {
                        string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                        await FirebaseFirestoreService.DeleteDocumentAsync("agents", agent.Id, idToken);
                        await DisplayAlert("Success", "Agent listing deleted.", "OK");
                        LoadApprovedAgents(); // Refresh the list.
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", "Error deleting agent listing: " + ex.Message, "OK");
                    }
                }
            }
        }
    }


    // Model for an agent listing.
    public class AgentListing
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public string Speciality { get; set; }
        public string Rate { get; set; }
        public string Bio { get; set; }   // Bio information.
        public string UserId { get; set; }  // Owner's user id.
        public bool IsOwner { get; set; }   // True if current user's id equals UserId.
    }
}
