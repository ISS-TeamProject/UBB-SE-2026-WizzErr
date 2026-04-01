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

        public DashboardViewModel(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
            MyTickets = new ObservableCollection<Ticket>();

            // Setăm comanda de Cancel
            CancelTicketCommand = new RelayCommand(ExecuteCancelTicket);

            LoadUserTickets();
        }

        private void LoadUserTickets()
        {
            MyTickets.Clear();

            // Preluăm ID-ul logat (folosim 1 ca fallback dacă nu e logat)
            int currentUserId = UserSession.CurrentUser?.UserId ?? 1;

            var userTickets = _dashboardService.GetUserTickets(currentUserId);

            foreach (var ticket in userTickets)
            {
                MyTickets.Add(ticket);
            }
        }

        private void ExecuteCancelTicket(object parameter)
        {
            // Verificăm dacă paramaterul este un Ticket valid
            if (parameter is Ticket ticket && ticket.Status != "Cancelled")
            {
                // Apelăm baza de date pentru a face update-ul
                _dashboardService.CancelUserTicket(ticket.TicketId);

                // Reîncărcăm lista ca să vedem status-ul actualizat
                LoadUserTickets();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}