using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using sattleme.Services; // Contains FirebaseFirestoreService & FirebaseAuthService
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using sattleme.Home.Rental;

namespace sattleme.Home.Ride
{
    public partial class RiderTabPage : ContentPage
    {
        ObservableCollection<Rider> _allRiders = new ObservableCollection<Rider>();
        ObservableCollection<Rider> _filteredRiders = new ObservableCollection<Rider>();

        List<string> availabilityOptions = new List<string> { "All", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        List<string> colorOptions = new List<string> { "All", "Red", "Blue", "Green", "Black", "White", "N/A" };

        public RiderTabPage()
        {
            InitializeComponent();

            PickerAvailability.ItemsSource = availabilityOptions;
            PickerAvailability.SelectedIndex = 0;
            PickerAvailability.SelectedIndexChanged += OnFilterChanged;

            PickerColor.ItemsSource = colorOptions;
            PickerColor.SelectedIndex = 0;
            PickerColor.SelectedIndexChanged += OnFilterChanged;

            RidersListView.ItemsSource = _filteredRiders;
        }
        async void OnBecomeRiderClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BecomeRiderPage());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRidersAsync();
        }

        async Task LoadRidersAsync()
        {
            try
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user == null) return;

                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                // Query "riderApplications" where status equals "approved"
                var results = await FirebaseFirestoreService.QueryDocumentsAsync("riderApplications", "status", "approved", idToken);
                _allRiders.Clear();
                if (results != null && results.Any())
                {
                    foreach (var doc in results)
                    {
                        Rider rider = new Rider();
                        rider.DocumentId = doc.ContainsKey("documentId") ? doc["documentId"].ToString() : "";
                        rider.FullName = doc.ContainsKey("fullName") ? doc["fullName"].ToString() : "Unknown";
                        rider.PhotoUrl = doc.ContainsKey("photoUrl") ? doc["photoUrl"].ToString() : "";
                        rider.VehicleType = doc.ContainsKey("vehicleType") ? doc["vehicleType"].ToString() : "N/A";
                        rider.Color = doc.ContainsKey("color") ? doc["color"].ToString() : "N/A";
                        rider.UserId = doc.ContainsKey("userId") ? doc["userId"].ToString() : "riderUnknown";

                        // Parse availability array.
                        if (doc.ContainsKey("availability"))
                        {
                            try
                            {
                                var availabilityToken = doc["availability"] as JToken;
                                if (availabilityToken != null)
                                {
                                    rider.Availability = availabilityToken.ToObject<List<string>>();
                                }
                                else
                                {
                                    rider.Availability = new List<string>();
                                }
                            }
                            catch
                            {
                                rider.Availability = new List<string>();
                            }
                        }
                        else
                        {
                            rider.Availability = new List<string>();
                        }
                        _allRiders.Add(rider);
                    }
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load riders: " + ex.Message, "OK");
            }
            finally
            {
                RidersRefreshView.IsRefreshing = false;
            }
        }

        void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        void ApplyFilters()
        {
            string selectedAvailability = PickerAvailability.SelectedItem?.ToString() ?? "All";
            string selectedColor = PickerColor.SelectedItem?.ToString() ?? "All";

            var filtered = _allRiders.Where(r =>
            {
                bool matchesAvailability = true;
                bool matchesColor = true;
                if (selectedAvailability != "All")
                {
                    matchesAvailability = r.Availability != null && r.Availability.Contains(selectedAvailability);
                }
                if (selectedColor != "All")
                {
                    matchesColor = !string.IsNullOrEmpty(r.Color) &&
                                   r.Color.Equals(selectedColor, StringComparison.OrdinalIgnoreCase);
                }
                return matchesAvailability && matchesColor;
            }).ToList();

            _filteredRiders.Clear();
            foreach (var rider in filtered)
            {
                rider.AvailabilityText = (rider.Availability != null && rider.Availability.Any())
                    ? string.Join(", ", rider.Availability)
                    : "";
                _filteredRiders.Add(rider);
            }
            NoRidersLabel.IsVisible = _filteredRiders.Count == 0;
        }

        async void OnRefresh(object sender, EventArgs e)
        {
            await LoadRidersAsync();
        }

        async void OnMessageButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.CommandParameter is Rider rider)
            {
                var currentUser = FirebaseAuthService.Instance.CurrentUser;
                if (currentUser == null)
                {
                    await DisplayAlert("Error", "User not authenticated", "OK");
                    return;
                }
                string currentUserId = currentUser.Uid;
                string riderId = rider.UserId;
                string conversationId = string.Compare(currentUserId, riderId) < 0
                    ? $"{currentUserId}_{riderId}"
                    : $"{riderId}_{currentUserId}";

                await Navigation.PushAsync(new ChatPage(riderId, rider.FullName, rider.PhotoUrl));
            }
        }
    }
   


    public class Rider
    {
        public string DocumentId { get; set; }
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public List<string> Availability { get; set; }
        public string VehicleType { get; set; }
        public string Color { get; set; }
        public string UserId { get; set; }
        public string AvailabilityText { get; set; }
    }
}
