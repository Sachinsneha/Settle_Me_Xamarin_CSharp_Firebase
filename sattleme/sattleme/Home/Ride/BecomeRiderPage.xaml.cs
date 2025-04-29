using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using sattleme.Services;  
namespace sattleme.Home.Ride
{
    public partial class BecomeRiderPage : ContentPage
    {
        bool _isLoading = false;
        private string _riderDocId;  
        private string _vehiclePhotoUrl = "";
        private string _applicationStatus; 
        private readonly HashSet<string> _selectedDays = new HashSet<string>();
        private readonly string[] _days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private FileResult _selectedImage; 

        public BecomeRiderPage()
        {
            InitializeComponent();
            LoadUserInfo();
            InitializeDays();
            LoadRiderData();
        }

       
        private async void LoadUserInfo()
        {
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user != null)
            {
                EmailEntry.Text = user.Email ?? "";
                if (!string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    NameEntry.Text = user.DisplayName;
                }
                else
                {
                    try
                    {
                        string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                        var userData = await FirebaseFirestoreService.GetDocumentAsync("users", user.Uid, idToken);
                        if (userData != null && userData.ContainsKey("fullName"))
                        {
                            NameEntry.Text = userData["fullName"].ToString();
                        }
                    }
                    catch { }
                }
            }
        }

        private void InitializeDays()
        {
            DaysFlexLayout.Children.Clear();
            foreach (var day in _days)
            {
                var button = new Button
                {
                    Text = day,
                    BackgroundColor = Color.LightGray,
                    Margin = new Thickness(4),
                    WidthRequest = 50,
                    HeightRequest = 40
                };
                button.Clicked += (s, e) =>
                {
                    if (_selectedDays.Contains(day))
                    {
                        _selectedDays.Remove(day);
                        button.BackgroundColor = Color.LightGray;
                    }
                    else
                    {
                        _selectedDays.Add(day);
                        button.BackgroundColor = Color.Green;
                    }
                };
                DaysFlexLayout.Children.Add(button);
            }
        }

        
        private void LockNonAvailabilityFields()
        {
            NameEntry.IsEnabled = false;
            EmailEntry.IsEnabled = false;
            ContactEntry.IsEnabled = false;
            NumberPlateEntry.IsEnabled = false;
            VehicleTypeEntry.IsEnabled = false;
            ColorEntry.IsEnabled = false;
            UploadPhotoButton.IsEnabled = false;
        }

        private async void LoadRiderData()
        {
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user == null)
                return;
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var results = await FirebaseFirestoreService.QueryDocumentsAsync("riderApplications", "userId", user.Uid, idToken);
                if (results != null && results.Any())
                {
                    var data = results.First();
                    _riderDocId = data.ContainsKey("documentId") ? data["documentId"].ToString() : null;
                    _applicationStatus = data.ContainsKey("status") ? data["status"].ToString() : null;
                    ContactEntry.Text = data.ContainsKey("contact") ? data["contact"].ToString() : "";
                    NumberPlateEntry.Text = data.ContainsKey("numberPlate") ? data["numberPlate"].ToString() : "";
                    VehicleTypeEntry.Text = data.ContainsKey("vehicleType") ? data["vehicleType"].ToString() : "";
                    ColorEntry.Text = data.ContainsKey("color") ? data["color"].ToString() : "";

                    if (data.ContainsKey("availability"))
                    {
                        var availability = data["availability"] as Newtonsoft.Json.Linq.JArray;
                        if (availability != null)
                        {
                            foreach (var day in availability)
                            {
                                string d = day.ToString();
                                _selectedDays.Add(d);
                                foreach (var child in DaysFlexLayout.Children)
                                {
                                    if (child is Button btn && btn.Text == d)
                                        btn.BackgroundColor = Color.Green;
                                }
                            }
                        }
                    }
                    if (data.ContainsKey("photoUrl"))
                    {
                        _vehiclePhotoUrl = data["photoUrl"].ToString();
                        if (!string.IsNullOrWhiteSpace(_vehiclePhotoUrl))
                        {
                            VehicleImage.Source = ImageSource.FromUri(new Uri(_vehiclePhotoUrl));
                        }
                    }
                    DeleteButton.IsVisible = true;
                    LockNonAvailabilityFields();
                    SubmitButton.Text = "Update Availability";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to load rider data: " + ex.Message, "OK");
            }
        }

        private async void OnUploadPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                _selectedImage = await MediaPicker.PickPhotoAsync();
                if (_selectedImage != null)
                {
                    VehicleImage.Source = ImageSource.FromFile(_selectedImage.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error picking image: " + ex.Message, "OK");
            }
        }

        private async void OnSubmitClicked(object sender, EventArgs e)
        {
            if (!_selectedDays.Any())
            {
                await DisplayAlert("Validation", "Please select at least one availability day", "OK");
                return;
            }
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user == null)
            {
                await DisplayAlert("Error", "User not authenticated", "OK");
                return;
            }
            string formName = NameEntry.Text?.Trim() ?? "";
            string authName = (user.DisplayName ?? "").Trim();
            string formEmail = EmailEntry.Text?.Trim() ?? "";
            string authEmail = (user.Email ?? "").Trim();
            bool nameMatches = string.IsNullOrEmpty(authName) || string.Equals(formName, authName, StringComparison.OrdinalIgnoreCase);
            bool emailMatches = string.Equals(formEmail, authEmail, StringComparison.OrdinalIgnoreCase);
            if (!nameMatches || !emailMatches)
            {
                await DisplayAlert("Validation", "Full Name and Email must match your login credentials", "OK");
                return;
            }
            _isLoading = true;
            SubmitButton.IsEnabled = false;
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                if (!string.IsNullOrEmpty(_riderDocId))
                {
                    var updateData = new Dictionary<string, object>
                    {
                        { "availability", _selectedDays.ToList() },
                        { "timestamp", DateTime.UtcNow }
                    };
                    await FirebaseFirestoreService.UpdateDocumentAsync("riderApplications", _riderDocId, updateData, idToken);
                    await DisplayAlert("Success", "Availability updated", "OK");
                }
                else
                {
                    // Upload the vehicle photo if selected.
                    string photoUrl = _vehiclePhotoUrl;
                    if (_selectedImage != null)
                    {
                        using (var stream = await _selectedImage.OpenReadAsync())
                        {
                            string fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(_selectedImage.FileName)}";
                            // This call uses your storage service to upload to the folder "vehicle_images/{userId}"
                            photoUrl = await FirebaseStorageService.UploadFileAsync("vehicle_images", user.Uid, fileName, stream, idToken);
                        }
                    }
                    var riderData = new Dictionary<string, object>
                    {
                        { "fullName", NameEntry.Text },
                        { "email", EmailEntry.Text },
                        { "contact", ContactEntry.Text },
                        { "numberPlate", NumberPlateEntry.Text },
                        { "vehicleType", VehicleTypeEntry.Text },
                        { "color", ColorEntry.Text },
                        { "photoUrl", photoUrl },
                        { "availability", _selectedDays.ToList() },
                        { "userId", user.Uid },
                        { "timestamp", DateTime.UtcNow },
                        { "status", "pending" }
                    };
                    string docId = Guid.NewGuid().ToString();
                    await FirebaseFirestoreService.AddDocumentAsync("riderApplications", docId, riderData, idToken);
                    _riderDocId = docId;
                    DeleteButton.IsVisible = true;
                    await DisplayAlert("Success", "Application submitted", "OK");
                }
                LockNonAvailabilityFields();
                SubmitButton.Text = "Update Availability";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error submitting application: " + ex.Message, "OK");
            }
            finally
            {
                _isLoading = false;
                SubmitButton.IsEnabled = true;
            }
        }

        // Handle deletion of the rider application.
        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_riderDocId))
                return;
            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to delete your application?", "Yes", "No");
            if (!confirm)
                return;
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                await FirebaseFirestoreService.DeleteDocumentAsync("riderApplications", _riderDocId, idToken);
                _riderDocId = null;
                _applicationStatus = null;
                ContactEntry.Text = "";
                NumberPlateEntry.Text = "";
                VehicleTypeEntry.Text = "";
                ColorEntry.Text = "";
                _selectedDays.Clear();
                foreach (var child in DaysFlexLayout.Children)
                {
                    if (child is Button btn)
                        btn.BackgroundColor = Color.LightGray;
                }
                _selectedImage = null;
                VehicleImage.Source = "default_vehicle.png";
                DeleteButton.IsVisible = false;
                SubmitButton.Text = "Submit Application";
                await DisplayAlert("Success", "Application deleted", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error deleting application: " + ex.Message, "OK");
            }
        }
    }
}
