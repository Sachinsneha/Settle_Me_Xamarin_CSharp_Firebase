using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using sattleme.Services;
using sattleme.Home.HomePages;
using sattleme.Home.Profile;

namespace sattleme.Home.Rental
{
    public partial class AllListingsPage : ContentPage
    {
        public AllListingsPage()
        {
            InitializeComponent();
            LoadAllListings();
        }

        async void LoadAllListings()
        {
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                var docs = await FirebaseFirestoreService.QueryAllDocumentsAsync("rentalListings", idToken);

                var listings = new List<ListingItem>();
                foreach (var data in docs)
                {
                    var allImageUrls = new List<string>();
                    if (data.ContainsKey("imageUrls"))
                    {
                        var imageArray = data["imageUrls"] as IEnumerable<object>;
                        if (imageArray != null && imageArray.Any())
                        {
                            foreach (var item in imageArray)
                                allImageUrls.Add(item.ToString());
                        }
                    }

                    string firstImage = allImageUrls.FirstOrDefault() ?? "default_rental_image.png";

                    var listing = new ListingItem
                    {
                        Title = data.ContainsKey("name") ? data["name"].ToString() : "",
                        Price = data.ContainsKey("price") ? data["price"].ToString() : "",
                        Address = data.ContainsKey("address") ? data["address"].ToString() : "",
                        Description = data.ContainsKey("description") ? data["description"].ToString() : "",
                        UserFullName = data.ContainsKey("userFullName") ? data["userFullName"].ToString() : "",
                        UserId = data.ContainsKey("userId") ? data["userId"].ToString() : "",
                        UserPhoto = "",
                        ImageUrl = firstImage,
                        ImageUrls = allImageUrls
                    };

                    if (data.ContainsKey("timestamp"))
                    {
                        DateTime timestamp;
                        if (DateTime.TryParse(data["timestamp"].ToString(), out timestamp))
                        {
                            listing.Timestamp = timestamp;
                        }
                    }

                    listings.Add(listing);
                }

                listings = listings.OrderByDescending(l => l.Timestamp).ToList();
                AllListingsCollectionView.ItemsSource = listings;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error loading listings: {ex.Message}", "OK");
            }
        }

        private async void OnListingSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ListingItem selectedListing)
            {
                await Navigation.PushAsync(new ListingDetailPage(selectedListing));
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        public class ListingItem
        {
            public string Title { get; set; }
            public string Price { get; set; }
            public string Address { get; set; }
            public string Description { get; set; }
            public string ImageUrl { get; set; }
            public List<string> ImageUrls { get; set; } = new List<string>();
            public string UserFullName { get; set; }
            public string UserId { get; set; }
            public string UserPhoto { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private async void OnHomeTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HomePages.HomePage());
        }

        private async void OnChatTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Navigation", "Chat tapped", "OK");
        }

        private async void OnAddTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UploadRental());
        }

        private async void OnFavoriteTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Navigation", "Notifications tapped", "OK");
        }

        private async void OnProfileTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }
    }
}
