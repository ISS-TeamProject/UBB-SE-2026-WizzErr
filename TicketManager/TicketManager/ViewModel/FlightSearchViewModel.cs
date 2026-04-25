using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class FlightSearchViewModel : ViewModelBase
    {
        private readonly IFlightSearchService searchService;
        private readonly INavigationService navigationService;
        private readonly IPricingService pricingService;

        private string location = string.Empty;
        public string Location
        {
            get => location;
            set
            {
                location = value;
                OnPropertyChanged();
            }
        }

        private bool isDeparture = true;
        public bool IsDeparture
        {
            get => isDeparture;
            set
            {
                isDeparture = value;
                OnPropertyChanged();
            }
        }

        private DateTimeOffset? flightDate;
        public DateTimeOffset? FlightDate
        {
            get => flightDate;
            set
            {
                flightDate = value;
                OnPropertyChanged();
            }
        }

        private string passengers = string.Empty;
        public string Passengers
        {
            get => passengers;
            set
            {
                passengers = value;
                OnPropertyChanged();
            }
        }

        private string searchResultMessage = string.Empty;
        public string SearchResultMessage
        {
            get => searchResultMessage;
            set
            {
                searchResultMessage = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FlightDisplayModel> AvailableFlights { get; set; }

        public ICommand SearchCommand { get; }
        public ICommand BookFlightCommand { get; }

        public FlightSearchViewModel(IFlightSearchService searchService, INavigationService navigationService, IPricingService pricingService)
        {
            this.searchService = searchService;
            this.navigationService = navigationService;
            this.pricingService = pricingService;
            AvailableFlights = new ObservableCollection<FlightDisplayModel>();

            SearchCommand = new RelayCommand(parameter => ExecuteSearch());
            BookFlightCommand = new RelayCommand(parameter => ExecuteBookFlight(parameter as FlightDisplayModel));
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
            {
                return;
            }

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

            var results = searchService.SearchFlights(Location, IsDeparture, date, requestedPassengers);
            bool hasResults = false;

            foreach (var flight in results)
            {
                AvailableFlights.Add(new FlightDisplayModel(flight, pricingService.CalculateBasePrice(flight)));
                hasResults = true;
            }

            if (!hasResults)
            {
                SearchResultMessage = "No flights found for the selected criteria.";
            }
        }

        private void ExecuteBookFlight(FlightDisplayModel? selectedFlightDisplay)
        {
            if (selectedFlightDisplay?.Flight == null)
            {
                return;
            }

            int passengerCount = ParsePassengerCount();
            var bookingParameters = new object[] { selectedFlightDisplay.Flight, passengerCount };

            if (UserSession.CurrentUser == null)
            {
                UserSession.PendingBookingParameters = bookingParameters;
                navigationService.NavigateTo(typeof(View.AuthPage));
                return;
            }

            navigationService.NavigateTo(typeof(View.BookingPage), bookingParameters);
        }

        private int ParsePassengerCount()
        {
            if (string.IsNullOrWhiteSpace(Passengers))
            {
                return 0;
            }

            if (int.TryParse(Passengers, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            Passengers = "1";
            return 1;
        }
    }
}
