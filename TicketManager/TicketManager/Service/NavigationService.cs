using System;
using Microsoft.UI.Xaml.Controls;

namespace TicketManager.Service
{
    /// <summary>
    /// Concrete navigation service that wraps the WinUI Frame.
    /// Lives in the Service layer but depends on WinUI — this is acceptable because
    /// it's the single place where the UI framework is referenced for navigation.
    /// ViewModels only see the INavigationService interface.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private Frame _frame;

        public void Initialize(Frame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void NavigateTo(Type pageType, object parameter = null)
        {
            if (_frame == null)
                throw new InvalidOperationException("NavigationService has not been initialized with a Frame.");

            _frame.Navigate(pageType, parameter);
        }

        public void GoBack()
        {
            if (_frame != null && _frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }

        public bool CanGoBack => _frame?.CanGoBack ?? false;
    }
}
