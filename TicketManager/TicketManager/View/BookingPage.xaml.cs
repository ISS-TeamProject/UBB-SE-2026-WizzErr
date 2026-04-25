using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TicketManager.Domain;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    public sealed partial class BookingPage : Page
    {
        private const byte ColorAlphaFull = 255;
        private const byte OccupiedSeatColorR = 156;
        private const byte OccupiedSeatColorG = 163;
        private const byte OccupiedSeatColorB = 175;
        private const byte SelectedSeatColorR = 43;
        private const byte SelectedSeatColorG = 184;
        private const byte SelectedSeatColorB = 192;
        private const byte AvailableSeatColorR = 229;
        private const byte AvailableSeatColorG = 231;
        private const byte AvailableSeatColorB = 235;
        private const int SeatColumnsCount = 6;
        private const int SeatColumnWidth = 58;
        private const int AisleColumnIndex = 3;
        private const int AisleColumnWidth = 24;
        private const int DefaultSeatCapacity = 40;
        private const int SeatRowHeight = 54;
        private const int SeatButtonWidth = 50;
        private const int SeatButtonHeight = 44;
        private const int SeatButtonFontSize = 13;
        private const int SeatCColumnIndex = 2;
        private const int SeatDColumnIndex = 4;
        private const int SeatEColumnIndex = 5;

        public BookingViewModel ViewModel { get; }
        private PassengerFormViewModel? seatTargetPassenger;
        private readonly SolidColorBrush occupiedSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(ColorAlphaFull, OccupiedSeatColorR, OccupiedSeatColorG, OccupiedSeatColorB));
        private readonly SolidColorBrush selectedSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(ColorAlphaFull, SelectedSeatColorR, SelectedSeatColorG, SelectedSeatColorB));
        private readonly SolidColorBrush availableSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(ColorAlphaFull, AvailableSeatColorR, AvailableSeatColorG, AvailableSeatColorB));

        public BookingPage()
        {
            this.InitializeComponent();

            ViewModel = new BookingViewModel(App.BookingService, App.PricingService, App.NavigationService);
            ViewModel.Passengers.CollectionChanged += Passengers_CollectionChanged;
            ViewModel.BookingConfirmed += ViewModel_BookingConfirmed;

            this.DataContext = ViewModel;
        }

        private async void ViewModel_BookingConfirmed(object? sender, EventArgs e)
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

            bool initialized = await ViewModel.OnNavigatedToAsync(e.Parameter);

            if (initialized)
            {
                GenerateSeatMap();
                EnsureSeatTargetPassenger();
                RefreshSeatMapVisuals();
            }
        }

        private void GenerateSeatMap()
        {
            seatMapGrid.Children.Clear();
            seatMapGrid.RowDefinitions.Clear();
            seatMapGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < SeatColumnsCount; i++)
            {
                seatMapGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(SeatColumnWidth) });
            }

            seatMapGrid.ColumnDefinitions.Insert(AisleColumnIndex, new ColumnDefinition() { Width = new GridLength(AisleColumnWidth) });

            int capacity = ViewModel.CurrentFlight?.Route?.Capacity ?? DefaultSeatCapacity;
            int rows = (capacity + SeatColumnsCount - 1) / SeatColumnsCount;

            for (int r = 0; r < rows; r++)
            {
                seatMapGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(SeatRowHeight) });
                CreateSeatButton(r, 0, $"{r + 1}A");
                CreateSeatButton(r, 1, $"{r + 1}B");
                CreateSeatButton(r, SeatCColumnIndex, $"{r + 1}C");
                CreateSeatButton(r, SeatDColumnIndex, $"{r + 1}D");
                CreateSeatButton(r, SeatEColumnIndex, $"{r + 1}E");
                CreateSeatButton(r, SeatColumnsCount, $"{r + 1}F");
            }

            RefreshSeatMapVisuals();
        }

        private void CreateSeatButton(int row, int col, string seatNumber)
        {
            Button btn = new Button
            {
                Content = seatNumber,
                Width = SeatButtonWidth,
                Height = SeatButtonHeight,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
                FontSize = SeatButtonFontSize,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(btn, row);
            Grid.SetColumn(btn, col);

            if (ViewModel.OccupiedSeats.Contains(seatNumber))
            {
                btn.IsHitTestVisible = false;
                btn.Background = occupiedSeatBrush;
                btn.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                btn.Background = availableSeatBrush;
                btn.Click += Seat_Click;
            }

            seatMapGrid.Children.Add(btn);
        }

        private void Seat_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string seat)
            {
                EnsureSeatTargetPassenger();
                if (seatTargetPassenger == null)
                {
                    return;
                }

                ViewModel.SelectSeat(seatTargetPassenger, seat);
                RefreshSeatMapVisuals();
            }
        }

        private void Passengers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            EnsureSeatTargetPassenger();
            RefreshSeatMapVisuals();
        }

        private void SeatPassengerSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            seatTargetPassenger = seatPassengerSelector.SelectedItem as PassengerFormViewModel;
            RefreshSeatMapVisuals();
        }

        private void EnsureSeatTargetPassenger()
        {
            if (seatTargetPassenger != null && ViewModel.Passengers.Contains(seatTargetPassenger))
            {
                return;
            }

            seatTargetPassenger = ViewModel.Passengers.FirstOrDefault();
            seatPassengerSelector.SelectedItem = seatTargetPassenger;
        }

        private void RefreshSeatMapVisuals()
        {
            foreach (var btn in seatMapGrid.Children.OfType<Button>())
            {
                if (btn.Content is not string seatNumber)
                {
                    continue;
                }

                if (ViewModel.OccupiedSeats.Contains(seatNumber))
                {
                    btn.IsEnabled = true;
                    btn.IsHitTestVisible = false;
                    btn.Background = occupiedSeatBrush;
                    btn.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    btn.IsEnabled = true;
                    btn.IsHitTestVisible = true;
                    bool isSelectedByPassenger = ViewModel.Passengers.Any(passenger => passenger.SelectedSeat == seatNumber);
                    btn.Background = isSelectedByPassenger ? selectedSeatBrush : availableSeatBrush;
                    btn.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        private void AddOnList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView && listView.Tag is PassengerFormViewModel passenger)
            {
                ViewModel.UpdatePassengerAddOns(
                    passenger,
                    System.Linq.Enumerable.OfType<AddOn>(e.AddedItems),
                    System.Linq.Enumerable.OfType<AddOn>(e.RemovedItems));
            }
        }
    }
}
