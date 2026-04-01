using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;
using System;

namespace TicketManager.ViewModel
{
    public partial class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DashboardService _dashboardService;

        public ObservableCollection<Ticket> MyTickets { get; set; }

        public ICommand CancelTicketCommand { get; }
        public ICommand DownloadPdfCommand { get; }

        public DashboardViewModel(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
            MyTickets = new ObservableCollection<Ticket>();

            CancelTicketCommand = new RelayCommand(ExecuteCancelTicket);
            DownloadPdfCommand = new RelayCommand(ExecuteDownloadPdf);

            LoadUserTickets();
        }

        private void LoadUserTickets()
        {
            MyTickets.Clear();
            int currentUserId = UserSession.CurrentUser?.UserId ?? 1;
            var userTickets = _dashboardService.GetUserTickets(currentUserId);
            foreach (var ticket in userTickets) MyTickets.Add(ticket);
        }

        private void ExecuteCancelTicket(object parameter)
        {
            if (parameter is Ticket ticket && ticket.Status != "Cancelled")
            {
                _dashboardService.CancelUserTicket(ticket.TicketId);
                LoadUserTickets();
            }
        }

        private void ExecuteDownloadPdf(object parameter)
        {
            if (parameter is Ticket ticket)
            {
                try
                {
                    // Apelăm serviciul (Separation of Concerns)
                    string generatedFilePath = _dashboardService.GenerateTicketPdf(ticket);

                    // Interacțiunea cu sistemul (deschiderea fișierului pe ecran) rămâne în ViewModel
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