using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    public sealed partial class FlightSearchPage : Page
    {
        public FlightSearchViewModel ViewModel { get; }

        public FlightSearchPage()
        {
            this.InitializeComponent();

            ViewModel = new FlightSearchViewModel(App.FlightSearchService, App.NavigationService, App.PricingService);
            this.DataContext = ViewModel;
        }

        private void PassengersInput_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            if (args.NewText.Any(character => !char.IsDigit(character)))
            {
                args.Cancel = true;
            }
        }

        private void DatePicker_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (args.Item.Date.Date < DateTimeOffset.Now.Date)
            {
                args.Item.IsBlackout = true;
            }
        }

        private void ClearDateButton_Click(object? sender, RoutedEventArgs e)
        {
            ViewModel.FlightDate = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.OnNavigatedTo(e.Parameter);
        }
    }
}
