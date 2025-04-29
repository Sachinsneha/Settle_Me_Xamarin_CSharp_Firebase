using Xamarin.Forms;
using sattleme.Services;
using sattleme.Home.chat;

namespace sattleme.Home.Rental
{
    public partial class ListingDetailPage : ContentPage
    {
        private AllListingsPage.ListingItem _listing;

        public ListingDetailPage(AllListingsPage.ListingItem listing)
        {
            InitializeComponent();
            _listing = listing;
            BindingContext = _listing;
            LoadUserPhotoIfNeeded();
        }

        private async void LoadUserPhotoIfNeeded()
        {
            if (string.IsNullOrEmpty(_listing.UserPhoto) && !string.IsNullOrEmpty(_listing.UserId))
            {
                try
                {
                    string idToken = await FirebaseAuthService.Instance.GetValidIdTokenAsync();
                    var userData = await FirebaseFirestoreService.GetDocumentAsync("users", _listing.UserId, idToken);
                    if (userData != null && userData.ContainsKey("photoUrl"))
                    {
                        _listing.UserPhoto = userData["photoUrl"].ToString();
                        BindingContext = null;
                        BindingContext = _listing;
                    }
                }
                catch (System.Exception ex)
                {
                    await DisplayAlert("Error", $"Error loading user photo: {ex.Message}", "OK");
                }
            }
        }

        private async void OnMessageClicked(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new ChatPage(_listing.UserId, _listing.UserFullName, _listing.UserPhoto));
        }
    }
}
