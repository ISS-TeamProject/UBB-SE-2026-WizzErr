using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class FlightSearchViewModel : ViewModelBase
    {
        private readonly IFlightSearchService _searchService;
        private readonly INavigationService _navigationService;

        private string _location;
        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        private bool _isDeparture = true;
        public bool IsDeparture
        {
            get => _isDeparture;
            set { _isDeparture = value; OnPropertyChanged(); }
        }

        private DateTimeOffset? _flightDate;
        public DateTimeOffset? FlightDate
        {
            get => _flightDate;
            set { _flightDate = value; OnPropertyChanged(); }
        }

        private string _passengers;
        public string Passengers
        {
            get => _passengers;
            set { _passengers = value; OnPropertyChanged(); }
        }

        private string _searchResultMessage;
        public string SearchResultMessage
        {
            get => _searchResultMessage;
            set { _searchResultMessage = value; OnPropertyChanged(); }
        }

        public ObservableCollection<FlightDisplayModel> AvailableFlights { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand BookFlightCommand { get; }

        public FlightSearchViewModel(IFlightSearchService searchService, INavigationService navigationService)
        {
            _searchService = searchService;
            _navigationService = navigationService;
            AvailableFlights = new ObservableCollection<FlightDisplayModel>();
            _location = string.Empty;
            _passengers = string.Empty;
            _searchResultMessage = string.Empty;

            SearchCommand = new RelayCommand(_ => ExecuteSearch());
            BookFlightCommand = new RelayCommand(param => ExecuteBookFlight(param as FlightDisplayModel));
        }



        public void OnNavigatedTo(object parameter)
        {
            if (parameter is User user)
            {
                UserSession.CurrentUser = user;
            }
        }

        private void ExecuteSearch()
        {

            AvailableFlights.Clear();
            SearchResultMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Location))
                return;

            string routeType = IsDeparture ? "DEP" : "ARR";
            DateTime? date = FlightDate?.Date;

            int? requestedPassengers = null;
            if (!string.IsNullOrWhiteSpace(Passengers))
            {
                if (int.TryParse(Passengers, out var parsedPassengers) && parsedPassengers > 0)
                {
                    requestedPassengers = parsedPassengers;
                }
                else
                {
                    Passengers = "1";
                    requestedPassengers = 1;
                }
            }

            var results = _searchService.SearchFlights(Location, routeType, date, requestedPassengers);
            bool hasResults = false;

            foreach (var flight in results)
            {

                AvailableFlights.Add(new FlightDisplayModel(flight));
                hasResults = true;
            }

            if (!hasResults)
            {
                SearchResultMessage = "No flights found for the selected criteria.";
            }
        }





        private void ExecuteBookFlight(FlightDisplayModel selectedFlightDisplay)
        {
            if (selectedFlightDisplay?.Flight == null)
                return;

            int passengerCount = ParsePassengerCount();

            var bookingParameters = new object[] { selectedFlightDisplay.Flight, passengerCount };

            if (UserSession.CurrentUser == null)
            {
                UserSession.PendingBookingParameters = bookingParameters;
                _navigationService.NavigateTo(typeof(View.AuthPage));
                return;
            }

            _navigationService.NavigateTo(typeof(View.BookingPage), bookingParameters);
        }

        private int ParsePassengerCount()
        {
            if (string.IsNullOrWhiteSpace(Passengers))
                return 0;

            if (int.TryParse(Passengers, out var parsed) && parsed > 0)
                return parsed;

            Passengers = "1";
            return 1;
        }
    }
}