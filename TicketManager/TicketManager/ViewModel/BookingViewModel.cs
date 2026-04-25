using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class BookingViewModel : ViewModelBase
    {
        private const int DefaultFlightCapacity = 180;
        private readonly IBookingService bookingService;
        private readonly IPricingService pricingService;
        private readonly INavigationService navigationService;
        private readonly RelayCommand confirmBookingCommand;
        private bool isSaving;
        private bool passengersValid;

        private Flight currentFlight = null!;
        public Flight CurrentFlight
        {
            get => currentFlight;
            set
            {
                currentFlight = value;
                OnPropertyChanged();
            }
        }

        private User currentUser = null!;
        public User CurrentUser
        {
            get => currentUser;
            set
            {
                currentUser = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<PassengerFormViewModel> passengersList = new ObservableCollection<PassengerFormViewModel>();
        public ObservableCollection<PassengerFormViewModel> Passengers
        {
            get => passengersList;
            set
            {
                passengersList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<AddOn> availableAddOns = new ObservableCollection<AddOn>();
        public ObservableCollection<AddOn> AvailableAddOns
        {
            get => availableAddOns;
            set
            {
                availableAddOns = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> occupiedSeats = new ObservableCollection<string>();
        public ObservableCollection<string> OccupiedSeats
        {
            get => occupiedSeats;
            set
            {
                occupiedSeats = value;
                OnPropertyChanged();
            }
        }

        private float basePriceTotal;
        public float BasePriceTotal
        {
            get => basePriceTotal;
            set
            {
                basePriceTotal = value;
                OnPropertyChanged();
            }
        }

        private float basePricePerPerson;
        public float BasePricePerPerson
        {
            get => basePricePerPerson;
            set
            {
                basePricePerPerson = value;
                OnPropertyChanged();
            }
        }

        private float finalTotalPrice;
        public float FinalTotalPrice
        {
            get => finalTotalPrice;
            set
            {
                finalTotalPrice = value;
                OnPropertyChanged();
            }
        }

        private float addOnsTotal;
        public float AddOnsTotal
        {
            get => addOnsTotal;
            set
            {
                addOnsTotal = value;
                OnPropertyChanged();
            }
        }

        private float membershipSavings;
        public float MembershipSavings
        {
            get => membershipSavings;
            set
            {
                membershipSavings = value;
                OnPropertyChanged();
            }
        }

        public string BasePricePerPersonDisplay => $"{BasePricePerPerson:0.00} €";
        public string BasePriceTotalDisplay => $"{BasePriceTotal:0.00} €";
        public string AddOnsTotalDisplay => $"{AddOnsTotal:0.00} €";
        public string MembershipSavingsDisplay => $"-{MembershipSavings:0.00} €";
        public string FinalTotalPriceDisplay => $"{FinalTotalPrice:0.00} €";

        private string validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => validationMessage;
            set
            {
                validationMessage = value;
                OnPropertyChanged();
            }
        }

        private int maxPassengers;
        public int MaxPassengers
        {
            get => maxPassengers;
            set
            {
                maxPassengers = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddPassenger));
            }
        }

        public bool CanAddPassenger => Passengers.Count < MaxPassengers;
        public bool CanRemovePassenger => Passengers.Count > 1;
        public bool CanConfirmBooking =>
            !isSaving &&
            CurrentUser != null &&
            CurrentFlight != null &&
            Passengers.Count > 0 &&
            passengersValid;

        public event EventHandler? BookingConfirmed;

        public BookingViewModel(IBookingService bookingService, IPricingService pricingService, INavigationService navigationService)
        {
            this.bookingService = bookingService;
            this.pricingService = pricingService;
            this.navigationService = navigationService;

            AddPassengerCommand = new RelayCommand(parameter => AddPassenger());
            RemovePassengerCommand = new RelayCommand(parameter => RemovePassenger(parameter as PassengerFormViewModel));
            confirmBookingCommand = new RelayCommand(async parameter => await ConfirmBookingAsync(), parameter => CanConfirmBooking);
            ConfirmBookingCommand = confirmBookingCommand;
        }

        public ICommand AddPassengerCommand { get; }
        public ICommand RemovePassengerCommand { get; }
        public ICommand ConfirmBookingCommand { get; }

        public async Task<bool> OnNavigatedToAsync(object parameter)
        {
            Flight? selectedFlight = null;
            User? localUser = null;
            int requestedPassengers = 0;

            if (parameter is object[] args && args.Length > 0)
            {
                selectedFlight = args[0] as Flight;

                if (args.Length >= 3)
                {
                    localUser = args[1] as User;
                    if (args[2] is int count)
                    {
                        requestedPassengers = count;
                    }
                }
                else if (args.Length >= 2)
                {
                    if (args[1] is int count)
                    {
                        requestedPassengers = count;
                    }
                    else
                    {
                        localUser = args[1] as User;
                    }
                }
            }

            localUser ??= UserSession.CurrentUser;

            if (selectedFlight == null)
            {
                return false;
            }

            if (localUser == null)
            {
                UserSession.PendingBookingParameters = new object[] { selectedFlight, requestedPassengers };
                navigationService.NavigateTo(typeof(View.AuthPage));
                return false;
            }

            await InitializeAsync(selectedFlight, localUser, requestedPassengers);
            return true;
        }

        public async Task InitializeAsync(Flight flight, User user, int requestedPassengerCount = 0)
        {
            CurrentFlight = flight;
            CurrentUser = user;

            var addons = await bookingService.GetAvailableAddOnsAsync();
            AvailableAddOns.Clear();
            foreach (var addon in addons)
            {
                AvailableAddOns.Add(addon);
            }

            var seats = await bookingService.GetOccupiedSeatsAsync(flight?.FlightId ?? 0);
            OccupiedSeats.Clear();
            foreach (var seat in seats)
            {
                OccupiedSeats.Add(seat);
            }

            int capacity = flight?.Route?.Capacity ?? DefaultFlightCapacity;
            MaxPassengers = bookingService.CalculateMaxPassengers(capacity, OccupiedSeats.Count, requestedPassengerCount);

            Passengers.Clear();
            int initialCount = requestedPassengerCount > 0 ? requestedPassengerCount : 1;
            if (initialCount > MaxPassengers)
            {
                initialCount = MaxPassengers;
            }

            for (int i = 0; i < initialCount; i++)
            {
                if (CanAddPassenger || Passengers.Count == 0)
                {
                    var passenger = new PassengerFormViewModel();
                    RegisterPassenger(passenger);
                    Passengers.Add(passenger);
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
            if (!CanAddPassenger)
            {
                return;
            }

            var passenger = new PassengerFormViewModel();
            RegisterPassenger(passenger);
            Passengers.Add(passenger);
            UpdatePassengerLabels();
            UpdatePrices();
            OnPropertyChanged(nameof(CanAddPassenger));
            OnPropertyChanged(nameof(CanRemovePassenger));
            RefreshBookingState();
        }

        private void RemovePassenger(PassengerFormViewModel? passenger)
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
            passenger.SelectedAddOns.CollectionChanged += (sender, eventArgs) => UpdatePrices();
            passenger.PropertyChanged += (sender, eventArgs) =>
            {
                if (eventArgs.PropertyName == nameof(passenger.SelectedSeat) ||
                    eventArgs.PropertyName == nameof(passenger.FirstName) ||
                    eventArgs.PropertyName == nameof(passenger.LastName) ||
                    eventArgs.PropertyName == nameof(passenger.Email))
                {
                    RefreshBookingState();
                }

                if (eventArgs.PropertyName == nameof(passenger.SelectedSeat))
                {
                    UpdatePrices();
                }
            };
        }

        private System.Collections.Generic.List<PassengerData> MapPassengersToData()
        {
            return Passengers.Select(passenger => new PassengerData
            {
                FirstName = passenger.FirstName,
                LastName = passenger.LastName,
                Email = passenger.Email,
                Phone = passenger.Phone,
                SelectedSeat = passenger.SelectedSeat,
                SelectedAddOns = passenger.SelectedAddOns.ToList()
            }).ToList();
        }

        private void RefreshBookingState()
        {
            ValidationMessage = string.Empty;

            if (CurrentUser == null)
            {
                ValidationMessage = "Please sign in to continue.";
                passengersValid = false;
            }
            else
            {
                var passengerData = MapPassengersToData();
                ValidationMessage = bookingService.ValidatePassengers(passengerData);
                passengersValid = string.IsNullOrEmpty(ValidationMessage);
            }

            OnPropertyChanged(nameof(CanConfirmBooking));
            confirmBookingCommand.RaiseCanExecuteChanged();
        }

        public void UpdatePrices()
        {
            if (CurrentFlight == null)
            {
                return;
            }

            float basePrice = pricingService.CalculateBasePrice(CurrentFlight);
            var passengerData = MapPassengersToData();
            var tickets = bookingService.CreateTickets(CurrentFlight, CurrentUser, passengerData, basePrice);
            var breakdown = pricingService.CalculatePriceBreakdown(CurrentFlight, CurrentUser, tickets);

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
            if (!CanConfirmBooking)
            {
                return;
            }

            float basePrice = pricingService.CalculateBasePrice(CurrentFlight);
            var passengerData = MapPassengersToData();
            var tickets = bookingService.CreateTickets(CurrentFlight, CurrentUser, passengerData, basePrice);
            foreach (var ticket in tickets)
            {
                ticket.Price = pricingService.CalculateTotalPrice(ticket);
            }

            isSaving = true;
            OnPropertyChanged(nameof(CanConfirmBooking));
            confirmBookingCommand.RaiseCanExecuteChanged();

            bool success = await bookingService.SaveTicketsAsync(tickets);

            isSaving = false;
            OnPropertyChanged(nameof(CanConfirmBooking));
            confirmBookingCommand.RaiseCanExecuteChanged();

            ValidationMessage = success ? "Booking confirmed successfully." : "Booking could not be saved. Please try again.";

            if (success)
            {
                BookingConfirmed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SelectSeat(PassengerFormViewModel targetPassenger, string seat)
        {
            var currentHolder = Passengers.FirstOrDefault(passenger => passenger.SelectedSeat == seat);
            if (currentHolder == targetPassenger)
            {
                targetPassenger.SelectedSeat = string.Empty;
            }
            else
            {
                if (currentHolder != null)
                {
                    currentHolder.SelectedSeat = string.Empty;
                }

                targetPassenger.SelectedSeat = seat;
            }
        }

        public void UpdatePassengerAddOns(PassengerFormViewModel passenger, IEnumerable<AddOn> addedAddOns, IEnumerable<AddOn> removedAddOns)
        {
            foreach (var addOn in addedAddOns)
            {
                if (!passenger.SelectedAddOns.Contains(addOn))
                {
                    passenger.SelectedAddOns.Add(addOn);
                }
            }

            foreach (var addOn in removedAddOns)
            {
                passenger.SelectedAddOns.Remove(addOn);
            }
        }
    }
}
