using System;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sattleme.Home.Agents
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PaymentPage : ContentPage
    {
        // Event to notify that the payment has been successfully completed.
        public event EventHandler PaymentCompleted;

        public PaymentPage()
        {
            InitializeComponent();
        }

        private async void OnPayClicked(object sender, EventArgs e)
        {
            // Validate that all fields are entered.
            if (string.IsNullOrWhiteSpace(CardNumberEntry.Text) ||
                string.IsNullOrWhiteSpace(ExpiryEntry.Text) ||
                string.IsNullOrWhiteSpace(CVVEntry.Text) ||
                string.IsNullOrWhiteSpace(CardHolderNameEntry.Text))
            {
                await DisplayAlert("Error", "All fields are required.", "OK");
                return;
            }

            // Validate card number (should be 16 digits).
            if (!Regex.IsMatch(CardNumberEntry.Text, @"^\d{16}$"))
            {
                await DisplayAlert("Error", "Card Number must be 16 digits.", "OK");
                return;
            }

            // Validate expiry date (format MM/YY).
            // Validate expiry date (accepts "M/YY" or "MM/YY" formats)
            if (!Regex.IsMatch(ExpiryEntry.Text.Trim(), @"^(0?[1-9]|1[0-2])\/\d{2}$"))
            {
                await DisplayAlert("Error", "Expiry must be in MM/YY format.", "OK");
                return;
            }


            // Validate CVV (3 digits).
            if (!Regex.IsMatch(CVVEntry.Text, @"^\d{3}$"))
            {
                await DisplayAlert("Error", "CVV must be 3 digits.", "OK");
                return;
            }

            // Simulate payment processing (replace with real integration if needed).
            await DisplayAlert("Payment", "Payment processed successfully.", "OK");

            // Notify subscribers that payment is complete.
            PaymentCompleted?.Invoke(this, EventArgs.Empty);

            // Return to the previous page.
            await Navigation.PopAsync();
        }
    }
}
