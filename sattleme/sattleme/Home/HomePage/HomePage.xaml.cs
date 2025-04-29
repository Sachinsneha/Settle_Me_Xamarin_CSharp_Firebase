using sattleme.Home.Agents;
using sattleme.Home.chat;
using sattleme.Home.Profile;
using sattleme.Home.Rental;
using sattleme.Home.Ride;
using sattleme.Home.ToDo;
using sattleme.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace sattleme.Home.HomePages
{
    public partial class HomePage : ContentPage
    {
        public ObservableCollection<AllListingsPage.ListingItem> TopListings { get; set; } = new ObservableCollection<AllListingsPage.ListingItem>();

        public ICommand BookRideCommand { get; set; }
        public ICommand TaskManagerCommand { get; set; }
        public ICommand FindAgentCommand { get; set; }

        public HomePage()
        {
            InitializeComponent();
            BookRideCommand = new Command(async () => await Navigation.PushAsync(new PickUpServiceTabbedPage()));

            TaskManagerCommand = new Command(async () => await Navigation.PushAsync(new ToDoScreenPage(null)));
            FindAgentCommand = new Command(async () => await Navigation.PushAsync(new AgentListingPage()));

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTopListings();
        }

     
        async Task LoadTopListings()
        {
            try
            {
                string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                
                var docs = await FirebaseFirestoreService.QueryAllDocumentsAsync("rentalListings", idToken);
                var listings = new List<AllListingsPage.ListingItem>();

                foreach (var data in docs)
                {
                    var allImageUrls = new List<string>();
                    if (data.ContainsKey("imageUrls"))
                    {
                        var imageArray = data["imageUrls"] as IEnumerable<object>;
                        if (imageArray != null && imageArray.Any())
                        {
                            foreach (var item in imageArray)
                            {
                                allImageUrls.Add(item.ToString());
                            }
                        }
                    }
                    string firstImage = allImageUrls.FirstOrDefault() ?? "default_rental_image.png";

                    var listing = new AllListingsPage.ListingItem
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

                TopListings.Clear();
                foreach (var item in listings.Take(2))
                {
                    TopListings.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                await DisplayAlert("Error", $"Error loading top listings: {ex.Message}", "OK");
            }
        }

        private async void TopListing_Tapped(object sender, System.EventArgs e)
        {
            if (e is TappedEventArgs tappedEvent && tappedEvent.Parameter is AllListingsPage.ListingItem listing)
            {
                await Navigation.PushAsync(new ListingDetailPage(listing));
            }
        }

        private async void TapGestureRecognizer_Tapped(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new AllListingsPage());
        }

        private void OnHomeTapped(object sender, System.EventArgs e)
        {
        }

        private async void OnChatTapped(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new ChatListPage());       }

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
    }
}
