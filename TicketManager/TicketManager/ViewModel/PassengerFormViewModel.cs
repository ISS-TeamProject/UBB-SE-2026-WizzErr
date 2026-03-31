using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TicketManager.ViewModel
{
    public class PassengerFormViewModel : INotifyPropertyChanged
    {
        private string _passengerLabel = "Passenger";
        public string PassengerLabel
        {
            get => _passengerLabel;
            set { _passengerLabel = value; OnPropertyChanged(); }
        }

        private string _firstName = string.Empty;
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); }
        }

        private string _lastName = string.Empty;
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        private string _selectedSeat = string.Empty;
        public string SelectedSeat
        {
            get => _selectedSeat;
            set { _selectedSeat = value; OnPropertyChanged(); }
        }

        // AddOns selected for this specific passenger
        public ObservableCollection<Domain.AddOn> SelectedAddOns { get; set; } = new ObservableCollection<Domain.AddOn>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
