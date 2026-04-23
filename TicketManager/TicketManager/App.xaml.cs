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
        private Window window = null!;

        private static DatabaseConnectionFactory dbFactory = null!;

        private static IFlightRepository flightRepository = null!;
        private static ITicketRepository ticketRepository = null!;
        private static IAddOnRepository addOnRepository = null!;
        private static IMembershipRepository membershipRepository = null!;
        private static IUserRepository userRepository = null!;

        public static IAuthService AuthService { get; private set; } = null!;
        public static IFlightSearchService FlightSearchService { get; private set; } = null!;
        public static IBookingService BookingService { get; private set; } = null!;
        public static IPricingService PricingService { get; private set; } = null!;
        public static IDashboardService DashboardService { get; private set; } = null!;
        public static ICancellationService CancellationService { get; private set; } = null!;
        public static IMembershipService MembershipService { get; private set; } = null!;
        public static NavigationService NavigationService { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            ConfigureServices();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            window = new MainWindow();
            window.Activate();
        }

        /// <summary>
        /// Wires up every concrete dependency once. Views and ViewModels never
        /// need to know about DatabaseConnectionFactory or concrete repository types.
        /// </summary>
        private static void ConfigureServices()
        {
            dbFactory = new DatabaseConnectionFactory();

            flightRepository = new FlightRepository(dbFactory);
            ticketRepository = new TicketRepository(dbFactory);
            addOnRepository = new AddOnRepository(dbFactory);
            membershipRepository = new MembershipRepository(dbFactory);
            userRepository = new UserRepository(dbFactory, membershipRepository);

            AuthService = new AuthService(userRepository);
            FlightSearchService = new FlightSearchService(flightRepository);
            BookingService = new BookingService(ticketRepository, addOnRepository);
            PricingService = new PricingService();
            DashboardService = new DashboardService(ticketRepository);
            CancellationService = new CancellationService(ticketRepository);
            MembershipService = new MembershipService(userRepository, membershipRepository);
            NavigationService = new NavigationService();
        }
    }
}
