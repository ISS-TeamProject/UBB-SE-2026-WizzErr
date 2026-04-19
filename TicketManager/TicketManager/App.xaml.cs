using Microsoft.UI.Xaml;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// Also serves as the Composition Root: the single place where all concrete
    /// dependencies are wired together. Views retrieve what they need from here
    /// instead of constructing dependency chains themselves.
    /// </summary>
    public partial class App : Application
    {
        private Window _window;

        // ── Shared infrastructure ──────────────────────────────────
        private static DatabaseConnectionFactory _dbFactory;

        // ── Repositories (all behind interfaces) ───────────────────
        private static IFlightRepository _flightRepository;
        private static ITicketRepository _ticketRepository;
        private static IAddOnRepository _addOnRepository;
        private static IMembershipRepository _membershipRepository;
        private static IUserRepository _userRepository;

        // ── Services (all behind interfaces) ───────────────────────
        public static IAuthService AuthService { get; private set; }
        public static IFlightSearchService FlightSearchService { get; private set; }
        public static IBookingService BookingService { get; private set; }
        public static IPricingService PricingService { get; private set; }
        public static IDashboardService DashboardService { get; private set; }
        public static ICancellationService CancellationService { get; private set; }
        public static IMembershipService MembershipService { get; private set; }
        public static NavigationService NavigationService { get; private set; }

        public App()
        {
            InitializeComponent();
            ConfigureServices();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
        }

        /// <summary>
        /// Wires up every concrete dependency once. Views and ViewModels never
        /// need to know about DatabaseConnectionFactory or concrete repository types.
        /// </summary>
        private static void ConfigureServices()
        {
            // Infrastructure
            _dbFactory = new DatabaseConnectionFactory();

            // Repositories
            _flightRepository = new FlightRepository(_dbFactory);
            _ticketRepository = new TicketRepository(_dbFactory);
            _addOnRepository = new AddOnRepository(_dbFactory);
            _membershipRepository = new MembershipRepository(_dbFactory);
            _userRepository = new UserRepository(_dbFactory, _membershipRepository);

            // Services
            AuthService = new AuthService(_userRepository);
            FlightSearchService = new FlightSearchService(_flightRepository);
            BookingService = new BookingService(_ticketRepository, _addOnRepository);
            PricingService = new PricingService();
            DashboardService = new DashboardService(_ticketRepository);
            CancellationService = new CancellationService(_ticketRepository);
            MembershipService = new MembershipService(_userRepository, _membershipRepository);
            NavigationService = new NavigationService();
        }
    }
}
