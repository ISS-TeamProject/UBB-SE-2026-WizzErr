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
        private readonly IBookingService _bookingService;
        private readonly IPricingService _pricingService;
        private readonly RelayCommand _confirmBookingCommand;
        private bool _isSaving;
        private bool _passengersValid;

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
            _passengersValid;

        public event EventHandler BookingConfirmed;

        public BookingViewModel(IBookingService bookingService, IPricingService pricingService)
        {
            _bookingService = bookingService;
            _pricingService = pricingService;
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

            // Delegate capacity calculation to service
            int capacity = flight?.Route?.Capacity ?? 180;
            MaxPassengers = _bookingService.CalculateMaxPassengers(capacity, OccupiedSeats.Count, requestedPassengerCount);

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

        private System.Collections.Generic.List<PassengerData> MapPassengersToData()
        {
            return Passengers.Select(p => new PassengerData
            {
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                SelectedSeat = p.SelectedSeat,
                SelectedAddOns = p.SelectedAddOns.ToList()
            }).ToList();
        }

        private void RefreshBookingState()
        {
            ValidationMessage = string.Empty;

            if (CurrentUser == null)
            {
                ValidationMessage = "Please sign in to continue.";
                _passengersValid = false;
            }
            else
            {
                // Delegate validation to service
                var passengerData = MapPassengersToData();
                ValidationMessage = _bookingService.ValidatePassengers(passengerData);
                _passengersValid = string.IsNullOrEmpty(ValidationMessage);
            }

            OnPropertyChanged(nameof(CanConfirmBooking));
            _confirmBookingCommand.RaiseCanExecuteChanged();
        }

        public void UpdatePrices()
        {
            if (CurrentFlight == null) return;

            float basePrice = CurrentFlight.GetBasePrice();
            var passengerData = MapPassengersToData();
            var tickets = _bookingService.CreateTickets(CurrentFlight, CurrentUser, passengerData, basePrice);

            // Delegate price calculation to PricingService
            var breakdown = _pricingService.CalculatePriceBreakdown(CurrentFlight, CurrentUser, tickets);

            BasePricePerPerson = breakdown.BasePricePerPerson;
            BasePriceTotal = breakdown.BasePriceTotal;
            AddOnsTotal = breakdown.AddOnsTotal;
            MembershipSavings = breakdown.MembershipSavings;
            FinalTotalPrice = breakdown.FinalTotal;

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
            var passengerData = MapPassengersToData();
            var tickets = _bookingService.CreateTickets(CurrentFlight, CurrentUser, passengerData, basePrice);

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
