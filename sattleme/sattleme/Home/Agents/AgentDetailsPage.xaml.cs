using sattleme.Home.Rental;
using System;
using Xamarin.Forms;
using static sattleme.Home.Agents.AgentListingPage;

namespace sattleme.Home.Agents
{
    public partial class AgentDetailsPage : ContentPage
    {
        private AgentListing _agent;
        private bool _isPaid = false;

        public AgentDetailsPage(AgentListing agent)
        {
            InitializeComponent();
            _agent = agent;
            LoadAgentDetails();
        }

        void LoadAgentDetails()
        {
            AgentName.Text = _agent.FullName;
            AgentSpeciality.Text = _agent.Speciality;
            AgentBio.Text = _agent.Bio;

            if (!string.IsNullOrWhiteSpace(_agent.PhotoUrl))
            {
                try
                {
                    AgentImage.Source = ImageSource.FromUri(new Uri(_agent.PhotoUrl));
                }
                catch (Exception)
                {
                    AgentImage.Source = "default_profile.png";
                }
            }
            else
            {
                AgentImage.Source = "default_profile.png";
            }

            UpdatePayButton();
        }

        void UpdatePayButton()
        {
            if (_isPaid)
                PayButton.Text = "Chat";
            else
                PayButton.Text = $"Pay ${_agent.Rate} to Chat";
        }

        async void OnPayButtonClicked(object sender, EventArgs e)
        {
            if (!_isPaid)
            {
                var paymentPage = new PaymentPage();
                paymentPage.PaymentCompleted += PaymentPage_PaymentCompleted;
                await Navigation.PushAsync(paymentPage);
            }
            else
            {
                await Navigation.PushAsync(new ChatPage(_agent.UserId, _agent.FullName, _agent.PhotoUrl));
            }
        }

        void PaymentPage_PaymentCompleted(object sender, EventArgs e)
        {
            _isPaid = true;
            Device.BeginInvokeOnMainThread(() => UpdatePayButton());
        }
    }
}
