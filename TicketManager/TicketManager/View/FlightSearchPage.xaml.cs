using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    /// <summary>
    /// Code-behind is now minimal: it only handles pure-UI concerns
    /// (input filtering, calendar blackout dates) that have no business logic.
    /// All decision-making has been moved to FlightSearchViewModel.
    /// </summary>
    public sealed partial class FlightSearchPage : Page
    {
        public FlightSearchViewModel ViewModel { get; }

        public FlightSearchPage()
        {
            this.InitializeComponent();

            // ViewModel is built with services from the composition root.
            // This View no longer knows about DatabaseConnectionFactory or repositories.
            ViewModel = new FlightSearchViewModel(App.FlightSearchService, App.NavigationService);
            this.DataContext = ViewModel;
        }

        // ── Pure-UI handlers (no logic, just visual behaviour) ──────

        /// <summary>
        /// Restricts the passengers TextBox to digits only. This is a UI input filter,
        /// not business validation — it belongs in the View.
        /// </summary>
        private void PassengersInput_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (args.NewText.Any(c => !char.IsDigit(c)))
            {
                args.Cancel = true;
            }
        }

        /// <summary>
        /// Greys out past dates in the calendar picker. Pure visual concern.
        /// </summary>
        private void DatePicker_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (args.Item.Date.Date < DateTimeOffset.Now.Date)
            {
                args.Item.IsBlackout = true;
            }
        }

        private void ClearDateButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.FlightDate = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Delegate parameter processing to the ViewModel
            ViewModel.OnNavigatedTo(e.Parameter);
        }
    }
}
