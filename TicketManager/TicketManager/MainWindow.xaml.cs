using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager
{
    public sealed partial class MainWindow : Window
    {
        private const string AccountNavTag = "Account";
        private readonly IAuthService authService;

        public MainWindow()
        {
            this.InitializeComponent();

            authService = App.AuthService;

            App.NavigationService.Initialize(ContentFrame);

            ContentFrame.Navigated += ContentFrame_Navigated;

            NavigateToSearch();
            UpdateNavigationAvailability();
            TopNav.SelectedItem = null;
        }

        private void UpdateNavigationAvailability()
        {
            bool isAuthenticated = UserSession.CurrentUser != null;

            foreach (var item in TopNav.MenuItems.OfType<NavigationViewItem>())
            {
                string tag = item.Tag?.ToString() ?? string.Empty;
                bool isSearchItem = tag.EndsWith("FlightSearchPage", StringComparison.OrdinalIgnoreCase);
                item.IsEnabled = isAuthenticated || isSearchItem;
            }

            if (AccountMenuItem != null)
            {
                AccountMenuItem.IsEnabled = true;
            }
        }

        public void NavigateTo(Type pageType)
        {
            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }
        }

        public void NavigateToAuth()
        {
            NavigateTo(typeof(View.AuthPage));
        }

        public void NavigateToSearch()
        {
            NavigateTo(typeof(View.FlightSearchPage));
        }

        public void NavigateToDashboard()
        {
        }

        public void NavigateToMemberships()
        {
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (TopNav == null || TopNav.MenuItems == null)
            {
                return;
            }

            UpdateNavigationAvailability();

            bool itemFound = false;
            string pageName = e.SourcePageType.Name;

            foreach (NavigationViewItem item in TopNav.MenuItems)
            {
                if (item.Tag?.ToString()?.EndsWith(pageName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    TopNav.SelectedItem = item;
                    itemFound = true;
                    break;
                }
            }

            if (!itemFound || pageName == "AuthPage")
            {
                TopNav.SelectedItem = null;
            }
        }

        private async void TopNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            UpdateNavigationAvailability();

            var navItemTag = args.InvokedItemContainer?.Tag?.ToString();
            if (navItemTag == AccountNavTag)
            {
                var currentUser = UserSession.CurrentUser;
                if (currentUser == null)
                {
                    NavigateToAuth();
                    return;
                }

                string membershipTier = string.IsNullOrWhiteSpace(currentUser.Membership?.Name)
                    ? "None"
                    : currentUser.Membership.Name;

                var dialog = new ContentDialog
                {
                    Title = "Account",
                    Content = $"Email: {currentUser.Email}\nUsername: {currentUser.Username}\nMembership tier: {membershipTier}",
                    PrimaryButtonText = "Sign out",
                    CloseButtonText = "Close",
                    XamlRoot = ContentFrame.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    authService.Logout();
                    UpdateNavigationAvailability();
                    NavigateToSearch();
                }

                return;
            }

            if (!string.IsNullOrEmpty(navItemTag))
            {
                Type? pageType = Type.GetType(navItemTag);
                if (pageType != null)
                {
                    if (pageType != typeof(View.FlightSearchPage) && UserSession.CurrentUser == null)
                    {
                        NavigateToAuth();
                        return;
                    }

                    NavigateTo(pageType);
                }
            }
        }

        private void TopNav_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }
    }
}
