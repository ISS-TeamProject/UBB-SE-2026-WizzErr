using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;
using System;
using System.Linq;
using System.Collections.Specialized;
using TicketManager.ViewModel;
using TicketManager.Domain;

namespace TicketManager.View
{
    /// <summary>
    /// Code-behind handles only pure-UI concerns: seat map rendering, seat selection visuals,
    /// dialog display, and add-on list management. All parameter parsing, auth checking,
    /// and business logic lives in BookingViewModel.
    /// </summary>
    public sealed partial class BookingPage : Page
    {
        public BookingViewModel ViewModel { get; }
        private PassengerFormViewModel _seatTargetPassenger;
        private readonly SolidColorBrush _occupiedSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 156, 163, 175));
        private readonly SolidColorBrush _selectedSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 184, 192));
        private readonly SolidColorBrush _availableSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 229, 231, 235));

        public BookingPage()
        {
            this.InitializeComponent();

            // ViewModel is built with services from the composition root.
            // This View no longer knows about DatabaseConnectionFactory or repositories.
            ViewModel = new BookingViewModel(App.BookingService, App.PricingService, App.NavigationService);
            ViewModel.Passengers.CollectionChanged += Passengers_CollectionChanged;
            ViewModel.BookingConfirmed += ViewModel_BookingConfirmed;

            this.DataContext = ViewModel;
        }

        private async void ViewModel_BookingConfirmed(object sender, EventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Booking confirmed",
                Content = "Your tickets were successfully booked.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
            App.NavigationService.NavigateTo(typeof(FlightSearchPage));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Delegate all parameter parsing and auth checking to the ViewModel
            bool initialized = await ViewModel.OnNavigatedToAsync(e.Parameter);

            if (initialized)
            {
                // These are pure-UI operations (generating WinUI Button controls,
                // coloring them, managing grid layout). They belong in the View.
                GenerateSeatMap();
                EnsureSeatTargetPassenger();
                RefreshSeatMapVisuals();
            }
        }

        // a”€a”€ Seat Map (pure UI: creating buttons, setting colors) a”€a”€a”€a”€a”€a”€a”€a”€

        private void GenerateSeatMap()
        {
            seatMapGrid.Children.Clear();
            seatMapGrid.RowDefinitions.Clear();
            seatMapGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < 6; i++)
                seatMapGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(58) });

            seatMapGrid.ColumnDefinitions.Insert(3, new ColumnDefinition() { Width = new GridLength(24) });

            int capacity = ViewModel.CurrentFlight?.Route?.Capacity ?? 40;
            int rows = (capacity + 5) / 6;

            for (int r = 0; r < rows; r++)
            {
                seatMapGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(54) });
                CreateSeatButton(r, 0, $"{r + 1}A");
                CreateSeatButton(r, 1, $"{r + 1}B");
                CreateSeatButton(r, 2, $"{r + 1}C");
                CreateSeatButton(r, 4, $"{r + 1}D");
                CreateSeatButton(r, 5, $"{r + 1}E");
                CreateSeatButton(r, 6, $"{r + 1}F");
            }

            RefreshSeatMapVisuals();
        }

        private void CreateSeatButton(int row, int col, string seatNumber)
        {
            Button btn = new Button
            {
                Content = seatNumber,
                Width = 50,
                Height = 44,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                FontSize = 13,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(btn, row);
            Grid.SetColumn(btn, col);

            if (ViewModel.OccupiedSeats.Contains(seatNumber))
            {
                btn.IsHitTestVisible = false;
                btn.Background = _occupiedSeatBrush;
                btn.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                btn.Background = _availableSeatBrush;
                btn.Click += Seat_Click;
            }
            seatMapGrid.Children.Add(btn);
        }

        private void Seat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string seat)
            {
                EnsureSeatTargetPassenger();
                if (_seatTargetPassenger == null)
                    return;

                var currentHolder = ViewModel.Passengers.FirstOrDefault(p => p.SelectedSeat == seat);
                if (currentHolder == _seatTargetPassenger)
                {
                    _seatTargetPassenger.SelectedSeat = string.Empty;
                }
                else
                {
                    if (currentHolder != null)
                        currentHolder.SelectedSeat = string.Empty;

                    _seatTargetPassenger.SelectedSeat = seat;
                    RefreshSeatMapVisuals();
                }

                RefreshSeatMapVisuals();
            }
        }

        private void Passengers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            EnsureSeatTargetPassenger();
            RefreshSeatMapVisuals();
        }

        private void SeatPassengerSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _seatTargetPassenger = seatPassengerSelector.SelectedItem as PassengerFormViewModel;
            RefreshSeatMapVisuals();
        }

        private void EnsureSeatTargetPassenger()
        {
            if (_seatTargetPassenger != null && ViewModel.Passengers.Contains(_seatTargetPassenger))
                return;

            _seatTargetPassenger = ViewModel.Passengers.FirstOrDefault();
            seatPassengerSelector.SelectedItem = _seatTargetPassenger;
        }

        private void RefreshSeatMapVisuals()
        {
            foreach (var btn in seatMapGrid.Children.OfType<Button>())
            {
                if (btn.Content is not string seatNumber)
                    continue;

                if (ViewModel.OccupiedSeats.Contains(seatNumber))
                {
                    btn.IsEnabled = true;
                    btn.IsHitTestVisible = false;
                    btn.Background = _occupiedSeatBrush;
                    btn.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    btn.IsEnabled = true;
                    btn.IsHitTestVisible = true;
                    bool isSelectedByPassenger = ViewModel.Passengers.Any(p => p.SelectedSeat == seatNumber);
                    btn.Background = isSelectedByPassenger ? _selectedSeatBrush : _availableSeatBrush;
                    btn.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void AddOnList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv && lv.Tag is PassengerFormViewModel pass)
            {
                foreach (var added in System.Linq.Enumerable.OfType<AddOn>(e.AddedItems))
                {
                    if (!pass.SelectedAddOns.Contains(added)) pass.SelectedAddOns.Add(added);
                }
                foreach (var removed in System.Linq.Enumerable.OfType<AddOn>(e.RemovedItems))
                {
                    pass.SelectedAddOns.Remove(removed);
                }
            }
        }
    }
}