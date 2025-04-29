using sattleme.auth;
using sattleme.Home.HomePage;
using sattleme.Home.Ride;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sattleme
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new LoginPage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
