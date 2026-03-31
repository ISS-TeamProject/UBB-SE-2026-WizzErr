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

        // Proprietăți legate la input-urile din UI
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

        // WinUI CalendarDatePicker folosește DateTimeOffset
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

        // Lista de zboruri care va fi afișată automat în DataGrid/ListView
        public ObservableCollection<FlightDisplayModel> AvailableFlights { get; set; }

        // Comanda pentru butonul Search
        public ICommand SearchCommand { get; }

        public FlightSearchViewModel(IFlightSearchService searchService)
        {
            _searchService = searchService;
            AvailableFlights = new ObservableCollection<FlightDisplayModel>();

            // Legăm butonul de metoda ExecuteSearch
            SearchCommand = new RelayCommand(_ => ExecuteSearch());
        }

        private void ExecuteSearch()
        {
            // Curățăm rezultatele vechi
            AvailableFlights.Clear();
            SearchResultMessage = string.Empty;

            // Validări minime
            if (string.IsNullOrWhiteSpace(Location))
                return;

            // Conform comentariilor din FlightRepository, tipul rutei este "DEP" sau "ARR"
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

            // Apelăm Serviciul creat la Task-ul 1
            var results = _searchService.SearchFlights(Location, routeType, date, requestedPassengers);
            bool hasResults = false;

            foreach (var flight in results)
            {
                // Împachetăm zborul brut în modelul de afișare (formatat)
                AvailableFlights.Add(new FlightDisplayModel(flight));
                hasResults = true;
            }

            if (!hasResults)
            {
                SearchResultMessage = "No flights found for the selected criteria.";
            }
        }
    }
}