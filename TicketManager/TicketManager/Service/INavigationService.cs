using System;

namespace TicketManager.Service
{
    /// <summary>
    /// Allows ViewModels to request page navigation without depending on WinUI Frame directly.
    /// This keeps ViewModels testable and free of UI framework references.
    /// </summary>
    public interface INavigationService
    {
        void NavigateTo(Type pageType, object parameter = null);
        void GoBack();
        bool CanGoBack { get; }
    }
}
