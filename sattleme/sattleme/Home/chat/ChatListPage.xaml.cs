using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using sattleme.Services;
using sattleme.Home.chat;
using sattleme.Home.Rental;
using sattleme.Home.Profile;

namespace sattleme.Home.chat

{
    public partial class ChatListPage : ContentPage
    {

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
        public ChatListPage()
        {
            InitializeComponent();
            LoadChats();
        }

        private async void LoadChats()
        {
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                string currentUserId = FirebaseAuthService.Instance.CurrentUser.Uid;

                var sentMessages = await FirebaseFirestoreService.QueryDocumentsAsync("chat", "senderId", currentUserId, idToken);
                var receivedMessages = await FirebaseFirestoreService.QueryDocumentsAsync("chat", "receiverId", currentUserId, idToken);

                var allMessages = new List<Dictionary<string, object>>();
                allMessages.AddRange(sentMessages);
                allMessages.AddRange(receivedMessages);

                var grouped = allMessages.GroupBy(msg => msg.ContainsKey("conversationId") ? msg["conversationId"].ToString() : "");

                var chatSummaries = new List<ChatSummary>();

                foreach (var group in grouped)
                {
                    var latestMessage = group.OrderByDescending(m =>
                    {
                        DateTime ts = DateTime.MinValue;
                        DateTime.TryParse(m.ContainsKey("timestamp") ? m["timestamp"].ToString() : "", out ts);
                        return ts;
                    }).FirstOrDefault();

                    if (latestMessage == null) continue;

                    string conversationId = group.Key;
                    string[] parts = conversationId.Split('_');
                    string partnerId = parts.FirstOrDefault(id => id != currentUserId);
                    if (string.IsNullOrEmpty(partnerId))
                        partnerId = currentUserId; 

                    var partnerData = await FirebaseFirestoreService.GetDocumentAsync("users", partnerId, idToken);
                    string partnerName = partnerData != null && partnerData.ContainsKey("fullName") ? partnerData["fullName"].ToString() : "Unknown";
                    string partnerPhoto = partnerData != null && partnerData.ContainsKey("photoUrl") ? partnerData["photoUrl"].ToString() : "default_user.png";

                    string lastMessage = latestMessage.ContainsKey("messageText") ? latestMessage["messageText"].ToString() : "";
                    DateTime timestamp = DateTime.MinValue;
                    DateTime.TryParse(latestMessage.ContainsKey("timestamp") ? latestMessage["timestamp"].ToString() : "", out timestamp);

                    chatSummaries.Add(new ChatSummary
                    {
                        ConversationId = conversationId,
                        PartnerId = partnerId,
                        PartnerName = partnerName,
                        PartnerPhoto = partnerPhoto,
                        LastMessage = lastMessage,
                        Timestamp = timestamp
                    });
                }

                chatSummaries = chatSummaries.OrderByDescending(c => c.Timestamp).ToList();
                ChatsCollectionView.ItemsSource = chatSummaries;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading chats: {ex.Message}", "OK");
            }
        }



        private async void OnChatSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ChatSummary selectedChat)
            {
                await Navigation.PushAsync(new ChatPage(selectedChat.PartnerId, selectedChat.PartnerName, selectedChat.PartnerPhoto));
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        public class ChatSummary
        {
            public string ConversationId { get; set; }
            public string PartnerId { get; set; }
            public string PartnerName { get; set; }
            public string PartnerPhoto { get; set; }
            public string LastMessage { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
