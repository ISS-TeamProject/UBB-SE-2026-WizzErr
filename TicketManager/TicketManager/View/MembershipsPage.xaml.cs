using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    public sealed partial class MembershipsPage : Page
    {
        public MembershipViewModel ViewModel { get; }

        public MembershipsPage()
        {
            this.InitializeComponent();

            var dbFactory = new DatabaseConnectionFactory();
            var membershipRepo = new MembershipRepository(dbFactory);
            var userRepo = new UserRepository(dbFactory, membershipRepo);
            var service = new MembershipService(userRepo, membershipRepo);

            ViewModel = new MembershipViewModel(service);
        }

        private async void PurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            // Dacă nu e logat, îl trimitem la Login
            if (UserSession.CurrentUser == null)
            {
                this.Frame.Navigate(typeof(AuthPage));
                return;
            }

            if (sender is Button btn && btn.Tag is int membershipId)
            {
                try
                {
                    ViewModel.ExecutePurchase(membershipId);

                    var dialog = new ContentDialog
                    {
                        Title = "Membership updated",
                        Content = "Your membership purchase was completed successfully.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };

                    await dialog.ShowAsync();
                }
                catch
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Purchase failed",
                        Content = "Membership purchase could not be completed. Please try again.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };

                    await dialog.ShowAsync();
                }
            }
        }
    }
}
