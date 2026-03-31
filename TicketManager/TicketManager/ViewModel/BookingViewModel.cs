using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class BookingViewModel : INotifyPropertyChanged
    {
        private readonly BookingService _bookingService;
        private readonly RelayCommand _confirmBookingCommand;
        private bool _isSaving;

        private Flight _currentFlight;
        public Flight CurrentFlight
        {
            get => _currentFlight;
            set { _currentFlight = value; OnPropertyChanged(); }
        }

        private User _currentUser;
        public User CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        private ObservableCollection<PassengerFormViewModel> _passengers = new ObservableCollection<PassengerFormViewModel>();
        public ObservableCollection<PassengerFormViewModel> Passengers
        {
            get => _passengers;
            set { _passengers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<AddOn> _availableAddOns = new ObservableCollection<AddOn>();
        public ObservableCollection<AddOn> AvailableAddOns
        {
            get => _availableAddOns;
            set { _availableAddOns = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _occupiedSeats = new ObservableCollection<string>();
        public ObservableCollection<string> OccupiedSeats
        {
            get => _occupiedSeats;
            set { _occupiedSeats = value; OnPropertyChanged(); }
        }

        private float _basePriceTotal;
        public float BasePriceTotal
        {
            get => _basePriceTotal;
            set { _basePriceTotal = value; OnPropertyChanged(); }
        }

        private float _basePricePerPerson;
        public float BasePricePerPerson
        {
            get => _basePricePerPerson;
            set { _basePricePerPerson = value; OnPropertyChanged(); }
        }

        private float _finalTotalPrice;
        public float FinalTotalPrice
        {
            get => _finalTotalPrice;
            set { _finalTotalPrice = value; OnPropertyChanged(); }
        }

        private float _addOnsTotal;
        public float AddOnsTotal
        {
            get => _addOnsTotal;
            set { _addOnsTotal = value; OnPropertyChanged(); }
        }

        private float _membershipSavings;
        public float MembershipSavings
        {
            get => _membershipSavings;
            set { _membershipSavings = value; OnPropertyChanged(); }
        }

        public string BasePricePerPersonDisplay => $"{BasePricePerPerson:0.00} €";
        public string BasePriceTotalDisplay => $"{BasePriceTotal:0.00} €";
        public string AddOnsTotalDisplay => $"{AddOnsTotal:0.00} €";
        public string MembershipSavingsDisplay => $"-{MembershipSavings:0.00} €";
        public string FinalTotalPriceDisplay => $"{FinalTotalPrice:0.00} €";

        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set { _validationMessage = value; OnPropertyChanged(); }
        }

        private int _maxPassengers;
        public int MaxPassengers
        {
            get => _maxPassengers;
            set { _maxPassengers = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddPassenger)); }
        }

        public bool CanAddPassenger => Passengers.Count < MaxPassengers;
        public bool CanRemovePassenger => Passengers.Count > 1;
        public bool CanConfirmBooking =>
            !_isSaving &&
            CurrentUser != null &&
            CurrentFlight != null &&
            Passengers.Count > 0 &&
            Passengers.All(p =>
                !string.IsNullOrWhiteSpace(p.FirstName) &&
                !string.IsNullOrWhiteSpace(p.LastName) &&
                !string.IsNullOrWhiteSpace(p.SelectedSeat) &&
                IsValidEmail(p.Email));

        public event EventHandler BookingConfirmed;

        public BookingViewModel(BookingService bookingService)
        {
            _bookingService = bookingService;
            AddPassengerCommand = new RelayCommand(_ => AddPassenger());
            RemovePassengerCommand = new RelayCommand(param => RemovePassenger(param as PassengerFormViewModel));
            _confirmBookingCommand = new RelayCommand(async _ => await ConfirmBookingAsync(), _ => CanConfirmBooking);
            ConfirmBookingCommand = _confirmBookingCommand;
        }

        public ICommand AddPassengerCommand { get; }
        public ICommand RemovePassengerCommand { get; }
        public ICommand ConfirmBookingCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task InitializeAsync(Flight flight, User user, int requestedPassengerCount = 0)
        {
            CurrentFlight = flight;
            CurrentUser = user;

            // Load AddOns
            var addons = await _bookingService.GetAvailableAddOnsAsync();
            AvailableAddOns.Clear();
            foreach (var addon in addons)
            {
                AvailableAddOns.Add(addon);
            }

            // Load Occupied Seats
            var seats = await _bookingService.GetOccupiedSeatsAsync(flight?.FlightId ?? 0);
            OccupiedSeats.Clear();
            foreach (var seat in seats)
            {
                OccupiedSeats.Add(seat);
            }

            int capacity = flight?.Route?.Capacity ?? 180;
            int remainingCapacity = capacity - OccupiedSeats.Count;

            int allowedMax = requestedPassengerCount > 0 ? requestedPassengerCount : remainingCapacity;
            MaxPassengers = allowedMax > remainingCapacity ? remainingCapacity : allowedMax;

            Passengers.Clear();
            int initialCount = requestedPassengerCount > 0 ? requestedPassengerCount : 1;
            if (initialCount > MaxPassengers) initialCount = MaxPassengers;

            for(int i = 0; i < initialCount; i++)
            {
                if (CanAddPassenger || Passengers.Count == 0)
                {
                    var p = new PassengerFormViewModel();
                    RegisterPassenger(p);
                    Passengers.Add(p);
                }
            }

            UpdatePassengerLabels();
            UpdatePrices();
            OnPropertyChanged(nameof(CanAddPassenger));
            OnPropertyChanged(nameof(CanRemovePassenger));
            RefreshBookingState();
        }

        private void AddPassenger()
        {
            if (!CanAddPassenger) return;

            var p = new PassengerFormViewModel();
            RegisterPassenger(p);
            Passengers.Add(p);
            UpdatePassengerLabels();
            UpdatePrices();
            OnPropertyChanged(nameof(CanAddPassenger));
            OnPropertyChanged(nameof(CanRemovePassenger));
            RefreshBookingState();
        }

        private void RemovePassenger(PassengerFormViewModel passenger)
        {
            if (passenger != null && Passengers.Count > 1)
            {
                Passengers.Remove(passenger);
                UpdatePassengerLabels();
                UpdatePrices();
                OnPropertyChanged(nameof(CanAddPassenger));
                OnPropertyChanged(nameof(CanRemovePassenger));
                RefreshBookingState();
            }
        }

        private void UpdatePassengerLabels()
        {
            for (int i = 0; i < Passengers.Count; i++)
            {
                Passengers[i].PassengerLabel = $"Passenger {i + 1}";
            }
        }

        private void RegisterPassenger(PassengerFormViewModel passenger)
        {
            passenger.SelectedAddOns.CollectionChanged += (s, e) => UpdatePrices();
            passenger.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(passenger.SelectedSeat) ||
                    e.PropertyName == nameof(passenger.FirstName) ||
                    e.PropertyName == nameof(passenger.LastName) ||
                    e.PropertyName == nameof(passenger.Email))
                {
                    RefreshBookingState();
                }

                if (e.PropertyName == nameof(passenger.SelectedSeat))
                {
                    UpdatePrices();
                }
            };
        }

        private void RefreshBookingState()
        {
            ValidationMessage = string.Empty;

            if (CurrentUser == null)
            {
                ValidationMessage = "Please sign in to continue.";
            }
            else
            {
                for (int i = 0; i < Passengers.Count; i++)
                {
                    var passenger = Passengers[i];
                    int passengerNumber = i + 1;

                    if (string.IsNullOrWhiteSpace(passenger.FirstName))
                    {
                        ValidationMessage = $"Passenger {passengerNumber}: first name is required.";
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(passenger.LastName))
                    {
                        ValidationMessage = $"Passenger {passengerNumber}: last name is required.";
                        break;
                    }

                    if (!IsValidEmail(passenger.Email))
                    {
                        ValidationMessage = $"Passenger {passengerNumber}: email format is invalid.";
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(passenger.SelectedSeat))
                    {
                        ValidationMessage = $"Passenger {passengerNumber}: please select a seat.";
                        break;
                    }
                }
            }

            OnPropertyChanged(nameof(CanConfirmBooking));
            _confirmBookingCommand.RaiseCanExecuteChanged();
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            int atIndex = email.IndexOf('@');
            int dotIndex = email.LastIndexOf('.');
            return atIndex > 0 && dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
        }

        public void UpdatePrices()
        {
            if (CurrentFlight == null) return;

            float basePrice = CurrentFlight.GetBasePrice();
            BasePricePerPerson = basePrice;

            BasePriceTotal = basePrice * Passengers.Count;

            var tickets = _bookingService.CreateTickets(CurrentFlight, CurrentUser, Passengers.ToList(), basePrice);

            float totalWithoutMembership = tickets.Sum(t => t.Price + t.SelectedAddOns.Sum(a => a.GetBasePrice()));
            FinalTotalPrice = _bookingService.CalculateFinalPrice(tickets, CurrentUser);
            AddOnsTotal = Math.Max(0, FinalTotalPrice - BasePriceTotal);
            MembershipSavings = Math.Max(0, totalWithoutMembership - FinalTotalPrice);

            OnPropertyChanged(nameof(BasePricePerPersonDisplay));
            OnPropertyChanged(nameof(BasePriceTotalDisplay));
            OnPropertyChanged(nameof(AddOnsTotalDisplay));
            OnPropertyChanged(nameof(MembershipSavingsDisplay));
            OnPropertyChanged(nameof(FinalTotalPriceDisplay));

            RefreshBookingState();
        }

        private async Task ConfirmBookingAsync()
        {
            if (!CanConfirmBooking) return;

            float basePrice = CurrentFlight.GetBasePrice();
            var tickets = _bookingService.CreateTickets(CurrentFlight, CurrentUser, Passengers.ToList(), basePrice);

            _isSaving = true;
            OnPropertyChanged(nameof(CanConfirmBooking));
            _confirmBookingCommand.RaiseCanExecuteChanged();

            bool success = await _bookingService.SaveTicketsAsync(tickets);

            _isSaving = false;
            OnPropertyChanged(nameof(CanConfirmBooking));
            _confirmBookingCommand.RaiseCanExecuteChanged();

            ValidationMessage = success ? "Booking confirmed successfully." : "Booking could not be saved. Please try again.";

            if (success)
            {
                BookingConfirmed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
