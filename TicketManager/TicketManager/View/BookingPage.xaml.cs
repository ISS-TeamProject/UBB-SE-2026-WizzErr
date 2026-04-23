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
        public BookingViewModel ViewModel { get; }
        private PassengerFormViewModel? seatTargetPassenger;
        private readonly SolidColorBrush occupiedSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 156, 163, 175));
        private readonly SolidColorBrush selectedSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 184, 192));
        private readonly SolidColorBrush availableSeatBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 229, 231, 235));

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

            for (int i = 0; i < 6; i++)
            {
                seatMapGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(58) });
            }

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

                var currentHolder = ViewModel.Passengers.FirstOrDefault(passenger => passenger.SelectedSeat == seat);
                if (currentHolder == seatTargetPassenger)
                {
                    seatTargetPassenger.SelectedSeat = string.Empty;
                }
                else
                {
                    if (currentHolder != null)
                    {
                        currentHolder.SelectedSeat = string.Empty;
                    }

                    seatTargetPassenger.SelectedSeat = seat;
                    RefreshSeatMapVisuals();
                }

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
                foreach (var added in System.Linq.Enumerable.OfType<AddOn>(e.AddedItems))
                {
                    if (!passenger.SelectedAddOns.Contains(added))
                    {
                        passenger.SelectedAddOns.Add(added);
                    }
                }

                foreach (var removed in System.Linq.Enumerable.OfType<AddOn>(e.RemovedItems))
                {
                    passenger.SelectedAddOns.Remove(removed);
                }
            }
        }
    }
}
