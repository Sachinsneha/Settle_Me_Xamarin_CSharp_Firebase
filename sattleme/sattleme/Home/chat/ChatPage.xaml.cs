using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using sattleme.Services;

namespace sattleme.Home.Rental
{
    public partial class ChatPage : ContentPage
    {
        public string PartnerName { get; set; }
        public string PartnerPhoto { get; set; }
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        private string _conversationId;
        private string _partnerId;
        private string _currentUserId;

        public ChatPage(string partnerId, string partnerName, string partnerPhoto)
        {
            InitializeComponent();
            _partnerId = partnerId;
            PartnerName = partnerName;
            PartnerPhoto = partnerPhoto;
            _currentUserId = FirebaseAuthService.Instance.CurrentUser.Uid;
            _conversationId = BuildConversationId(_currentUserId, _partnerId);

            BindingContext = new { PartnerName = PartnerName, PartnerPhoto = PartnerPhoto, Messages = Messages };

            LoadMessages();
        }

        private string BuildConversationId(string userId1, string userId2)
        {
            var ids = new List<string> { userId1, userId2 };
            ids.Sort(); 
            return string.Join("_", ids);
        }

        private async void LoadMessages()
        {
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var docs = await FirebaseFirestoreService.QueryDocumentsAsync("chat", "conversationId", _conversationId, idToken);

                var messages = new List<ChatMessage>();
                foreach (var data in docs)
                {
                    string messageText = data.ContainsKey("messageText") ? data["messageText"].ToString() : "";
                    string senderId = data.ContainsKey("senderId") ? data["senderId"].ToString() : "";
                    string timestampStr = data.ContainsKey("timestamp") ? data["timestamp"].ToString() : "";
                    DateTime timestamp = DateTime.MinValue;
                    if (DateTime.TryParse(timestampStr, out DateTime dt))
                        timestamp = dt;

                    bool isSent = senderId == _currentUserId;
                    string bgColor = isSent ? "#DCF8C6" : "#FFFFFF";

                    messages.Add(new ChatMessage
                    {
                        MessageText = messageText,
                        SenderId = senderId,
                        Timestamp = timestamp,
                        BackgroundColor = bgColor,
                        IsSent = isSent
                    });
                }

                Messages = messages.OrderBy(m => m.Timestamp).ToList();
                BindingContext = new { PartnerName = PartnerName, PartnerPhoto = PartnerPhoto, Messages = Messages };
                MessagesCollectionView.ItemsSource = Messages;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading messages: {ex.Message}", "OK");
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            string text = MessageEntry.Text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var data = new Dictionary<string, object>
                {
                    { "conversationId", _conversationId },
                    { "senderId", _currentUserId },
                    { "receiverId", _partnerId },
                    { "messageText", text },
                    { "timestamp", DateTime.UtcNow }
                };

                string docId = Guid.NewGuid().ToString();
                await FirebaseFirestoreService.AddDocumentAsync("chat", docId, data, idToken);

                MessageEntry.Text = "";
                LoadMessages();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error sending message: {ex.Message}", "OK");
            }
        }

        public class ChatMessage
        {
            public string MessageText { get; set; }
            public string SenderId { get; set; }
            public DateTime Timestamp { get; set; }
            public string BackgroundColor { get; set; }
            public bool IsSent { get; set; }
        }
    }
}
