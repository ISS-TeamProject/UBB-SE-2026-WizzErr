using System;
using Microsoft.UI.Xaml.Controls;

namespace TicketManager.Service
{
    /// <summary>
    /// Concrete navigation service that wraps the WinUI Frame.
    /// Lives in the Service layer but depends on WinUI a€” this is acceptable because
    /// it's the single place where the UI framework is referenced for navigation.
    /// ViewModels only see the INavigationService interface.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private Frame frame;

        public void Initialize(Frame frame)
        {
            this.frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void NavigateTo(Type pageType, object parameter = null)
        {
            if (frame == null)
            {
                throw new InvalidOperationException("NavigationService has not been initialized with a Frame.");
            }

            frame.Navigate(pageType, parameter);
        }

        public void GoBack()
        {
            if (frame != null && frame.CanGoBack)
            {
                frame.GoBack();
            }
        }

        public bool CanGoBack => frame?.CanGoBack ?? false;
    }
}
