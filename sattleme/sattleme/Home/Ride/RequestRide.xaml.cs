using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xamarin.Forms;
using sattleme.Services; // Contains FirebaseFirestoreService & FirebaseAuthService

namespace sattleme.Home.Ride
{
    public partial class RequestRide : ContentPage
    {
        public string RideRequestDocId { get; set; }

        // Hidden pickers for date and time using opacity and minimal height so they remain focusable.
        DatePicker _datePicker;
        TimePicker _timePicker;

        // Hold selected date and time.
        DateTime? _selectedDate = null;
        TimeSpan? _selectedTime = null;

        // If updating an existing request, its doc ID.
        string _rideRequestDocId = null;

        public RequestRide()
        {
            InitializeComponent();

            // Initialize hidden DatePicker.
            _datePicker = new DatePicker
            {
                Opacity = 0,
                HeightRequest = 1
            };
            _datePicker.DateSelected += OnDateSelected;

            // Initialize hidden TimePicker.
            _timePicker = new TimePicker
            {
                Opacity = 0,
                HeightRequest = 1
            };
            _timePicker.PropertyChanged += OnTimePropertyChanged;

            // Add the pickers to the inner layout.
            if (Content is ScrollView scrollView && scrollView.Content is Layout<View> layout)
            {
                layout.Children.Add(_datePicker);
                layout.Children.Add(_timePicker);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserInfoAsync();
        }

        async Task LoadUserInfoAsync()
        {
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user == null)
                return;

            // Set email from auth.
            UserEmailLabel.Text = user.Email ?? "";

            // Get name and photo URL.
            string displayName = user.DisplayName;
            string photoUrl = user.PhotoUrl; // Assuming PhotoUrl property exists.

            // If missing, try loading from Firestore "users" document.
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var userData = await FirebaseFirestoreService.GetDocumentAsync("users", user.Uid, idToken);
                if (userData != null)
                {
                    if (string.IsNullOrWhiteSpace(displayName) && userData.ContainsKey("fullName"))
                        displayName = userData["fullName"].ToString();
                    if (string.IsNullOrWhiteSpace(photoUrl) && userData.ContainsKey("photoUrl"))
                        photoUrl = userData["photoUrl"].ToString();
                }
            }
            catch { }

            UserNameLabel.Text = string.IsNullOrWhiteSpace(displayName) ? "Unknown User" : displayName;
            ProfileImage.Source = !string.IsNullOrWhiteSpace(photoUrl) ? photoUrl : "default_profile.png";
        }

        void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            _selectedDate = e.NewDate;
            DateEntry.Text = e.NewDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
            UpdateDayMonthLabel();
        }

        void OnTimePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Time")
            {
                _selectedTime = _timePicker.Time;
                TimeEntry.Text = _timePicker.Time.ToString(@"hh\:mm");
            }
        }

        void PickDate_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                _datePicker.Focus();
            });
        }

        void PickTime_Clicked(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                _timePicker.Focus();
            });
        }

        void UpdateDayMonthLabel()
        {
            if (_selectedDate.HasValue)
            {
                string day = _selectedDate.Value.ToString("dddd", CultureInfo.InvariantCulture);
                string month = _selectedDate.Value.ToString("MMMM", CultureInfo.InvariantCulture);
                DayMonthLabel.Text = $"Day: {day}   Month: {month}";
            }
            else
            {
                DayMonthLabel.Text = "";
            }
        }

        async void SubmitRequest_Clicked(object sender, EventArgs e)
        {
            // Validate form fields.
            if (string.IsNullOrWhiteSpace(PickupEntry.Text) ||
                string.IsNullOrWhiteSpace(DropEntry.Text) ||
                string.IsNullOrWhiteSpace(DateEntry.Text) ||
                string.IsNullOrWhiteSpace(TimeEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please complete all fields.", "OK");
                return;
            }
            if (!_selectedDate.HasValue || !_selectedTime.HasValue)
            {
                await DisplayAlert("Validation Error", "Please select both date and time.", "OK");
                return;
            }

            // Combine date and time into a single DateTime.
            DateTime rideDateTime = new DateTime(
                _selectedDate.Value.Year,
                _selectedDate.Value.Month,
                _selectedDate.Value.Day,
                _selectedTime.Value.Hours,
                _selectedTime.Value.Minutes,
                0);

            // Get ride day and month strings.
            string rideDay = _selectedDate.Value.ToString("dddd", CultureInfo.InvariantCulture);
            string rideMonth = _selectedDate.Value.ToString("MMMM", CultureInfo.InvariantCulture);

            try
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user == null)
                {
                    await DisplayAlert("Error", "User not authenticated", "OK");
                    return;
                }
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();

                // Create ride request data.
                var rideData = new Dictionary<string, object>
                {
                    { "userId", user.Uid },
                    { "name", user.DisplayName ?? "" },
                    { "email", user.Email ?? "" },
                    { "photoUrl", user.PhotoUrl ?? "" },
                    { "pickupLocation", PickupEntry.Text.Trim() },
                    { "dropLocation", DropEntry.Text.Trim() },
                    { "rideDate", _selectedDate.Value },
                    { "rideTime", rideDateTime },
                    { "rideDay", rideDay },
                    { "rideMonth", rideMonth },
                    { "createdAt", DateTime.UtcNow }
                };

                if (string.IsNullOrEmpty(_rideRequestDocId))
                {
                    // Create a new document in the "rideRequested" collection.
                    string docId = Guid.NewGuid().ToString();
                    await FirebaseFirestoreService.AddDocumentAsync("rideRequested", docId, rideData, idToken);
                    _rideRequestDocId = docId;
                    await DisplayAlert("Success", "Ride request submitted", "OK");
                }
                else
                {
                    // Update existing document.
                    await FirebaseFirestoreService.UpdateDocumentAsync("rideRequested", _rideRequestDocId, new Dictionary<string, object>
                    {
                        { "pickupLocation", PickupEntry.Text.Trim() },
                        { "dropLocation", DropEntry.Text.Trim() },
                        { "rideDate", _selectedDate.Value },
                        { "rideTime", rideDateTime },
                        { "rideDay", rideDay },
                        { "rideMonth", rideMonth },
                    }, idToken);
                    await DisplayAlert("Success", "Ride request updated", "OK");
                }

                // Optionally clear the form or navigate back.
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to submit ride request: " + ex.Message, "OK");
            }
        }
    }
}
