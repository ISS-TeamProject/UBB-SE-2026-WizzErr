using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public partial class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IDashboardService _dashboardService;
        private readonly ICancellationService _cancellationService;

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

        public ICommand CancelTicketCommand { get; }
        public ICommand DownloadPdfCommand { get; }

        public DashboardViewModel(IDashboardService dashboardService, ICancellationService cancellationService)
        {
            _dashboardService = dashboardService;
            _cancellationService = cancellationService;
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

        public (bool CanCancel, string Reason) CanCancelTicket(Ticket ticket)
        {
            return _cancellationService.CanCancelTicket(ticket);
        }

        public void CancelTicket(Ticket ticket)
        {
            var (canCancel, _) = _cancellationService.CanCancelTicket(ticket);
            if (!canCancel)
            {
                return;

            _cancellationService.CancelTicket(ticket.TicketId);
            LoadUserTickets();
        }

        private void ExecuteCancelTicket(object parameter)
        {
            if (parameter is Ticket ticket)
                CancelTicket(ticket);
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}