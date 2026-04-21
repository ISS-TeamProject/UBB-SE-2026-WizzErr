using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager
{
    public sealed partial class MainWindow : Window
    {
        private const string AccountNavTag = "Account";
        private readonly IAuthService _authService;

        public MainWindow()
        {
            this.InitializeComponent();

            // Get the auth service from the centralized composition root a�� 
            // no more constructing DatabaseConnectionFactory/repos here.
            _authService = App.AuthService;

            // Give the NavigationService the Frame it needs to work with.
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
            // NavigateTo(typeof(View.DashboardPage));
        }

        public void NavigateToMemberships()
        {
            // NavigateTo(typeof(View.MembershipsPage));
        }

        // 2. Funcția care pune liniuța pe butonul corect automat
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (TopNav == null || TopNav.MenuItems == null) return;

            UpdateNavigationAvailability();

            bool itemFound = false;

            // Luăm doar numele paginii (ex: "FlightSearchPage" în loc de "TicketManager.view.FlightSearchPage")
            string pageName = e.SourcePageType.Name;

            foreach (NavigationViewItem item in TopNav.MenuItems)
            {
                // Acum verificăm doar dacă Tag-ul SE TERMINĂ cu numele paginii. 
                // Așa ignorăm complet problemele cu litere mari/mici de la folderul "view".
                if (item.Tag != null && item.Tag.ToString().EndsWith(pageName, StringComparison.OrdinalIgnoreCase))
                {
                    TopNav.SelectedItem = item;
                    itemFound = true;
                    break;
                }
            }

            // Dacă suntem la Login sau Register, scoatem selecția de pe meniu
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
                if (UserSession.CurrentUser == null)
                {
                    NavigateToAuth();
                    return;
                }

                string membershipTier = string.IsNullOrWhiteSpace(UserSession.CurrentUser.Membership?.Name)
                    ? "None"
                    : UserSession.CurrentUser.Membership.Name;

                var dialog = new ContentDialog
                {
                    Title = "Account",
                    Content = $"Email: {UserSession.CurrentUser.Email}\nUsername: {UserSession.CurrentUser.Username}\nMembership tier: {membershipTier}",
                    PrimaryButtonText = "Sign out",
                    CloseButtonText = "Close",
                    XamlRoot = ContentFrame.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _authService.Logout();
                    UpdateNavigationAvailability();
                    NavigateToSearch();
                }
                return;
            }

            if (!string.IsNullOrEmpty(navItemTag))
            {
                Type pageType = Type.GetType(navItemTag);
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