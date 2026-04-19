using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ICancellationService _cancellationService;
        private readonly INavigationService _navigationService;

        public ObservableCollection<Ticket> MyTickets { get; set; }
        public ObservableCollection<string> TicketFilters { get; }

        private string _selectedTicketFilter;
        public string SelectedTicketFilter
        {
            get => _selectedTicketFilter;
            set
            {
                if (_selectedTicketFilter == value) return;
                _selectedTicketFilter = value;
                OnPropertyChanged();
                LoadUserTickets();
            }
        }

        // ── Cancellation result state (View observes these to show dialogs) ──
        private string _cancellationMessage;
        public string CancellationMessage
        {
            get => _cancellationMessage;
            set { _cancellationMessage = value; OnPropertyChanged(); }
        }

        private bool? _cancellationSucceeded;
        /// <summary>
        /// null = no cancellation attempted, true = cancelled, false = cannot cancel.
        /// </summary>
        public bool? CancellationSucceeded
        {
            get => _cancellationSucceeded;
            set { _cancellationSucceeded = value; OnPropertyChanged(); }
        }

        // ── The ticket that is pending confirmation from the user ──
        private Ticket _pendingCancelTicket;
        /// <summary>
        /// When set to a non-null ticket, the View should show a confirmation dialog.
        /// After the user responds, the View calls ConfirmCancellation() or clears this.
        /// </summary>
        public Ticket PendingCancelTicket
        {
            get => _pendingCancelTicket;
            set { _pendingCancelTicket = value; OnPropertyChanged(); }
        }

        public ICommand CancelTicketCommand { get; }
        public ICommand DownloadPdfCommand { get; }

        public DashboardViewModel(IDashboardService dashboardService, ICancellationService cancellationService, INavigationService navigationService)
        {
            _dashboardService = dashboardService;
            _cancellationService = cancellationService;
            _navigationService = navigationService;

            MyTickets = new ObservableCollection<Ticket>();
            TicketFilters = new ObservableCollection<string> { "Upcoming", "Past" };
            _selectedTicketFilter = "Upcoming";

            CancelTicketCommand = new RelayCommand(ExecuteCancelTicket);
            DownloadPdfCommand = new RelayCommand(ExecuteDownloadPdf);

            LoadUserTickets();
        }

        public void LoadUserTickets()
        {
            MyTickets.Clear();
            int? currentUserId = UserSession.CurrentUser?.UserId;
            if (!currentUserId.HasValue)
                return;

            var filteredTickets = _dashboardService.GetUserTickets(currentUserId.Value, SelectedTicketFilter);
            foreach (var ticket in filteredTickets)
                MyTickets.Add(ticket);
        }

        /// <summary>
        /// Handles the cancel command. Checks eligibility via the service, then
        /// either reports an error or sets PendingCancelTicket to request user confirmation.
        /// This replaces the code-behind's multi-step orchestration.
        /// </summary>
        private void ExecuteCancelTicket(object parameter)
        {
            CancellationSucceeded = null;
            CancellationMessage = string.Empty;

            if (parameter is not Ticket ticket)
                return;

            if (string.Equals(ticket.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                return;

            var (canCancel, reason) = _cancellationService.CanCancelTicket(ticket);
            if (!canCancel)
            {
                CancellationSucceeded = false;
                CancellationMessage = reason;
                return;
            }

            // Request confirmation from the user (View will show a dialog)
            PendingCancelTicket = ticket;
        }

        /// <summary>
        /// Called by the View after the user confirms they want to cancel.
        /// </summary>
        public void ConfirmCancellation()
        {
            if (PendingCancelTicket == null)
                return;

            _cancellationService.CancelTicket(PendingCancelTicket.TicketId);
            PendingCancelTicket = null;
            LoadUserTickets();

            CancellationSucceeded = true;
            CancellationMessage = "The ticket status was updated to Cancelled.";
        }

        /// <summary>
        /// Called by the View if the user declines the cancellation.
        /// </summary>
        public void DeclineCancellation()
        {
            PendingCancelTicket = null;
        }

        /// <summary>
        /// Called by the View when navigated to. Checks auth and refreshes data.
        /// </summary>
        public bool OnNavigatedTo()
        {
            if (UserSession.CurrentUser == null)
            {
                _navigationService.NavigateTo(typeof(View.AuthPage));
                return false;
            }

            LoadUserTickets();
            return true;
        }

        private void ExecuteDownloadPdf(object parameter)
        {
            if (parameter is Ticket ticket)
            {
                try
                {
                    string generatedFilePath = _dashboardService.GenerateTicketPdf(ticket);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = generatedFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to generate PDF: {ex.Message}");
                }
            }
        }
    }
}