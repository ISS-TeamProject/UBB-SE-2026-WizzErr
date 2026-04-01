using Microsoft.UI.Xaml.Controls;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();

            // 1. Inițializăm baza de date și serviciul
            var dbFactory = new DatabaseConnectionFactory();
            var ticketRepository = new TicketRepository(dbFactory);
            var dashboardService = new DashboardService(ticketRepository);

            // 2. Creăm ViewModel-ul
            var viewModel = new DashboardViewModel(dashboardService);

            // 3. CRITIC: Spunem interfeței (XAML) să folosească acest ViewModel pentru {Binding}
            this.DataContext = viewModel;
        }
    }
}