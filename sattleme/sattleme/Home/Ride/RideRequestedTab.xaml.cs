using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using sattleme.Services; 
using Newtonsoft.Json;
using sattleme.Home.Rental;

namespace sattleme.Home.Ride
{
    public partial class RideRequestedPage : ContentPage
    {
        ObservableCollection<RideRequestModel> _rideRequests = new ObservableCollection<RideRequestModel>();

        public RideRequestedPage()
        {
            InitializeComponent();
            RideRequestsListView.ItemsSource = _rideRequests;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRideRequestsAsync();
        }

        async void OnRefresh(object sender, EventArgs e)
        {
            await LoadRideRequestsAsync();
            RideRequestsListView.IsRefreshing = false;
        }

        async Task LoadRideRequestsAsync()
        {
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var docs = await FirebaseFirestoreService.QueryAllDocumentsAsync("rideRequested", idToken);

                if (docs == null || !docs.Any())
                {
                    await DisplayAlert("Info", "No ride requests found.", "OK");
                    _rideRequests.Clear();
                    return;
                }

                var sortedDocs = docs.OrderByDescending(doc =>
                {
                    DateTime createdAt = DateTime.MinValue;
                    if (doc.ContainsKey("createdAt") && doc["createdAt"] != null)
                        createdAt = ParseFirestoreTimestamp(doc["createdAt"]);
                    if (createdAt == DateTime.MinValue && doc.ContainsKey("rideDate") && doc["rideDate"] != null)
                        createdAt = ParseFirestoreTimestamp(doc["rideDate"]);
                    return createdAt == DateTime.MinValue ? DateTime.UtcNow : createdAt;
                });

                _rideRequests.Clear();
                bool isApproved = await IsApprovedRiderAsync();

                foreach (var doc in sortedDocs)
                {
                    var model = new RideRequestModel
                    {
                        DocumentId = doc.ContainsKey("documentId") ? doc["documentId"].ToString() : "",
                        Name = "",
                        PhotoUrl = "",
                        PickupLocation = doc.ContainsKey("pickupLocation") ? doc["pickupLocation"].ToString() : "",
                        DropLocation = doc.ContainsKey("dropLocation") ? doc["dropLocation"].ToString() : "",
                        UserId = doc.ContainsKey("userId") ? doc["userId"].ToString() : ""
                    };

                    if (!string.IsNullOrWhiteSpace(model.UserId))
                    {
                        try
                        {
                            var userData = await FirebaseFirestoreService.GetDocumentAsync("users", model.UserId, idToken);
                            if (userData != null)
                            {
                                model.Name = userData.ContainsKey("fullName") ? userData["fullName"].ToString() : "Unknown";
                                model.PhotoUrl = userData.ContainsKey("photoUrl") ? userData["photoUrl"].ToString() : "default_avatar.png";
                            }
                            else
                            {
                                model.Name = "Unknown";
                                model.PhotoUrl = "default_avatar.png";
                            }
                        }
                        catch
                        {
                            model.Name = "Unknown";
                            model.PhotoUrl = "default_avatar.png";
                        }
                    }
                    else
                    {
                        model.Name = "Unknown";
                        model.PhotoUrl = "default_avatar.png";
                    }

                    if (doc.ContainsKey("rideDate") && doc["rideDate"] != null)
                    {
                        DateTime rideDate = ParseFirestoreTimestamp(doc["rideDate"]);
                        if (rideDate != DateTime.MinValue)
                        {
                            model.RideDate = rideDate;
                            model.DateStr = rideDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                            model.RideDay = rideDate.ToString("dddd", CultureInfo.InvariantCulture);
                            model.RideMonth = rideDate.ToString("MMMM", CultureInfo.InvariantCulture);
                        }
                    }
                    if (doc.ContainsKey("rideTime") && doc["rideTime"] != null)
                    {
                        DateTime rideTime = ParseFirestoreTimestamp(doc["rideTime"]);
                        if (rideTime != DateTime.MinValue)
                        {
                            model.RideTime = rideTime;
                            model.TimeStr = rideTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                        }
                    }
                    string currentUserId = FirebaseAuthService.Instance.CurrentUser?.Uid ?? "";
                    model.IsOwner = (model.UserId == currentUserId);
                    model.ShowChatButton = isApproved;
                    _rideRequests.Add(model);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load ride requests: " + ex.Message, "OK");
            }
        }

        async Task<bool> IsApprovedRiderAsync()
        {
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user == null)
                return false;
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var results = await FirebaseFirestoreService.QueryDocumentsAsync("riderApplications", "userId", user.Uid, idToken);
                if (results != null && results.Any())
                    return results.Any(doc => doc.ContainsKey("status") && doc["status"].ToString() == "approved");
            }
            catch { }
            return false;
        }

        private DateTime ParseFirestoreTimestamp(object timestampObj)
        {
            if (timestampObj == null)
                return DateTime.MinValue;
            if (timestampObj is DateTime dt)
                return dt;
            if (timestampObj is string ts)
            {
                if (DateTime.TryParse(ts, null, DateTimeStyles.RoundtripKind, out DateTime parsed))
                    return parsed;
            }
            if (DateTime.TryParse(timestampObj.ToString(), null, DateTimeStyles.RoundtripKind, out DateTime fallback))
                return fallback;
            return DateTime.MinValue;
        }

        async void OnSwipeItemInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is RideRequestModel model)
            {
                if (!model.IsOwner)
                {
                    await DisplayAlert("Permission Denied", "You can only delete your own ride request.", "OK");
                    return;
                }

                bool confirm = await DisplayAlert("Confirm", "Delete this ride request?", "Yes", "No");
                if (confirm)
                {
                    try
                    {
                        string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                        await FirebaseFirestoreService.DeleteDocumentAsync("rideRequested", model.DocumentId, idToken);
                        _rideRequests.Remove(model);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", "Failed to delete ride request: " + ex.Message, "OK");
                    }
                }
            }
        }

        async void OnUserTapped(object sender, EventArgs e)
        {
            if ((sender as VisualElement)?.BindingContext is RideRequestModel model)
            {
                try
                {
                    await Navigation.PushAsync(new ChatPage(model.UserId, model.Name, model.PhotoUrl));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Failed to open chat: " + ex.Message, "OK");
                }
            }
        }

        async void OnEditClicked(object sender, EventArgs e)
        {
            if ((sender as ImageButton)?.CommandParameter is RideRequestModel model)
            {
                await Navigation.PushAsync(new RequestRide { RideRequestDocId = model.DocumentId });
            }
        }

        async void OnChatClicked(object sender, EventArgs e)
        {
            if ((sender as Button)?.CommandParameter is RideRequestModel model)
            {
                await Navigation.PushAsync(new ChatPage(model.UserId, model.Name, model.PhotoUrl));
            }
        }

        async void OnRequestRideClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RequestRide());
        }
    }

    public class RideRequestModel
    {
        public string DocumentId { get; set; }
        public string Name { get; set; }
        public string PhotoUrl { get; set; }
        public string PickupLocation { get; set; }
        public string DropLocation { get; set; }
        public DateTime? RideDate { get; set; }
        public DateTime? RideTime { get; set; }
        public string DateStr { get; set; }
        public string TimeStr { get; set; }
        public string RideDay { get; set; }
        public string RideMonth { get; set; }
        public string UserId { get; set; }
        public bool IsOwner { get; set; }
        public bool ShowChatButton { get; set; }
    }

    public class RideRequestTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OwnerTemplate { get; set; }
        public DataTemplate NonOwnerTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var model = item as RideRequestModel;
            return (model != null && model.IsOwner) ? OwnerTemplate : NonOwnerTemplate;
        }
    }
}
