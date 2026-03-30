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
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.ViewModel;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TicketManager.View
{
    public sealed partial class FlightSearchPage : Page
    {
        // Această proprietate va fi expusă către XAML
        public FlightSearchViewModel ViewModel { get; }

        public FlightSearchPage()
        {
            this.InitializeComponent();

            // Instanțiem "lanțul" de dependențe manual:
            var dbFactory = new DatabaseConnectionFactory();
            var repository = new FlightRepository(dbFactory);
            var service = new FlightSearchService(repository);

            // Creăm ViewModel-ul dându-i serviciul
            ViewModel = new FlightSearchViewModel(service);
        }

        private void PassengersInput_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // Only allow digits
            if (args.NewText.Any(c => !char.IsDigit(c)))
            {
                args.Cancel = true;
            }
        }

        private void DatePicker_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            // Grey out past dates
            if (args.Item.Date.Date < DateTimeOffset.Now.Date)
            {
                args.Item.IsBlackout = true;
            }
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(BookingPage));
        }
    }
}
