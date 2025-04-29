using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Essentials;
using sattleme.Services;

namespace sattleme.Home.Rental
{
    public partial class UploadRental : ContentPage
    {
        List<FileResult> _imageFiles = new List<FileResult>();
        bool _isLoading = false;

        public UploadRental()
        {
            InitializeComponent();
            LoadMyListings();
        }

        async void OnSelectImagesClicked(object sender, EventArgs e)
        {
            try
            {
                var file = await MediaPicker.PickPhotoAsync();
                if (file != null)
                {
                    _imageFiles.Add(file);
                    var imagePaths = _imageFiles.Select(f => f.FullPath).ToList();
                    ImagesCollectionView.ItemsSource = imagePaths;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error picking images: " + ex.Message, "OK");
            }
        }

        async void OnPostRentalClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
                string.IsNullOrWhiteSpace(AddressEntry.Text) ||
                string.IsNullOrWhiteSpace(PriceEntry.Text) ||
                string.IsNullOrWhiteSpace(DescriptionEditor.Text))
            {
                await DisplayAlert("Validation Error", "All fields are required", "OK");
                return;
            }
            if (_imageFiles == null || _imageFiles.Count == 0)
            {
                await DisplayAlert("Validation Error", "Please select at least one image", "OK");
                return;
            }

            _isLoading = true;
            List<string> imageUrls = new List<string>();

            try
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user == null || string.IsNullOrEmpty(await FirebaseAuthService.Instance.GetValidIdTokenAsync()))
                {
                    await DisplayAlert("Error", "User not authenticated", "OK");
                    return;
                }

                var userData = await FirebaseFirestoreService.GetDocumentAsync("users", user.Uid, await FirebaseAuthService.Instance.GetValidIdTokenAsync());
                string userName = userData?["fullName"]?.ToString() ?? "Unknown";
                string userEmail = userData?["email"]?.ToString() ?? "No Email";

                foreach (var file in _imageFiles)
                {
                    using (var stream = await file.OpenReadAsync())
                    {
                        string fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName)}";
                        string downloadUrl = await FirebaseStorageService.UploadFileAsync(
                            "rentalPictures",
                            user.Uid,
                            fileName,
                            stream,
                            await FirebaseAuthService.Instance.GetValidIdTokenAsync()
                        );
                        imageUrls.Add(downloadUrl);
                    }
                }

                var listingData = new Dictionary<string, object>
                {
                    { "name", NameEntry.Text },
                    { "address", AddressEntry.Text },
                    { "price", PriceEntry.Text },
                    { "description", DescriptionEditor.Text },
                    { "imageUrls", imageUrls },
                    { "userId", user.Uid },
                    { "userEmail", userEmail },
                    { "userFullName", userName },
                    { "timestamp", DateTime.UtcNow }
                };

                await FirebaseFirestoreService.AddDocumentAsync("rentalListings", Guid.NewGuid().ToString(), listingData, await FirebaseAuthService.Instance.GetValidIdTokenAsync());
                await DisplayAlert("Success", "Listing Posted!", "OK");

                NameEntry.Text = "";
                AddressEntry.Text = "";
                PriceEntry.Text = "";
                DescriptionEditor.Text = "";
                _imageFiles.Clear();
                ImagesCollectionView.ItemsSource = null;
                LoadMyListings();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error posting listing: " + ex.Message, "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        async void LoadMyListings()
        {
            var user = FirebaseAuthService.Instance.CurrentUser;
            if (user == null)
                return;

            try
            {
                var docs = await FirebaseFirestoreService.QueryDocumentsAsync(
                    "rentalListings",
                    "userId",
                    user.Uid,
                    await FirebaseAuthService.Instance.GetValidIdTokenAsync()
                );

                if (docs == null || docs.Count == 0)
                {
                    Console.WriteLine("No listings found for this user.");
                    ListingsCollectionView.ItemsSource = null;
                    return;
                }

                var listings = new List<RentalListing>();
                foreach (var data in docs)
                {
                    string documentId = data.ContainsKey("documentId") ? data["documentId"].ToString() : "";
                    string imageUrl = "default_rental_image.png";
                    if (data.ContainsKey("imageUrls"))
                    {
                        var imageArray = data["imageUrls"] as IEnumerable<object>;
                        if (imageArray != null && imageArray.Any())
                        {
                            imageUrl = imageArray.First().ToString();
                        }
                    }

                    var listing = new RentalListing
                    {
                        Id = documentId,
                        Title = data.ContainsKey("name") ? data["name"].ToString() : "",
                        Address = data.ContainsKey("address") ? data["address"].ToString() : "",
                        Price = data.ContainsKey("price") ? data["price"].ToString() : "",
                        Description = data.ContainsKey("description") ? data["description"].ToString() : "",
                        ImageUrl = imageUrl
                    };

                    listing.EditCommand = new Command(async () => await EditListing(listing));
                    listing.DeleteCommand = new Command(async () => await DeleteListing(listing));
                    listings.Add(listing);
                }

                ListingsCollectionView.ItemsSource = listings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading listings: {ex.Message}");
                await DisplayAlert("Error", $"Error loading listings: {ex.Message}", "OK");
            }
        }

        async System.Threading.Tasks.Task EditListing(RentalListing listing)
        {
            string newName = await DisplayPromptAsync("Edit Name", "Enter new name", initialValue: listing.Title);
            if (newName == null) return;
            string newAddress = await DisplayPromptAsync("Edit Address", "Enter new address", initialValue: listing.Address);
            if (newAddress == null) return;
            string newPrice = await DisplayPromptAsync("Edit Price", "Enter new price", initialValue: listing.Price);
            if (newPrice == null) return;
            string newDescription = await DisplayPromptAsync("Edit Description", "Enter new description", initialValue: listing.Description);
            if (newDescription == null) return;

            var updatedData = new Dictionary<string, object>
            {
                { "name", newName },
                { "address", newAddress },
                { "price", newPrice },
                { "description", newDescription }
            };

            try
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                await FirebaseFirestoreService.UpdateDocumentAsync("rentalListings", listing.Id, updatedData, idToken);
                await DisplayAlert("Success", "Listing updated!", "OK");
                LoadMyListings();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error updating listing: " + ex.Message, "OK");
            }
        }

        async System.Threading.Tasks.Task DeleteListing(RentalListing listing)
        {
            bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this listing?", "Yes", "No");
            if (!confirm) return;

            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                await FirebaseFirestoreService.DeleteDocumentAsync("rentalListings", listing.Id, idToken);
                await DisplayAlert("Success", "Listing deleted!", "OK");
                LoadMyListings();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error deleting listing: " + ex.Message, "OK");
            }
        }

        public class RentalListing
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Address { get; set; }
            public string Price { get; set; }
            public string Description { get; set; }
            public string ImageUrl { get; set; }
            public Command EditCommand { get; set; }
            public Command DeleteCommand { get; set; }
        }
    }
}
