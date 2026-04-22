using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class DashboardService : IDashboardService
    {
        private const string CancelledStatus = "Cancelled";

        private readonly ITicketRepository ticketRepository;

        public DashboardService(ITicketRepository ticketRepository)
        {
            this.ticketRepository = ticketRepository;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public IEnumerable<Ticket> GetUserTickets(int userId, string ticketFilter)
        {
            var now = DateTime.Now;
            var tickets = this.ticketRepository.GetTicketsByUserId(userId)
                .Where(ticket => ticket.Flight != null);

            return string.Equals(ticketFilter, "Past", StringComparison.OrdinalIgnoreCase)
                ? tickets.Where(ticket => ticket.Flight!.Date < now).OrderByDescending(ticket => ticket.Flight!.Date)
                : tickets.Where(ticket => ticket.Flight!.Date >= now).OrderBy(ticket => ticket.Flight!.Date);
        }

        public string GenerateTicketPdf(Ticket ticket)
        {
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string filePath = Path.Combine(downloadsFolder, $"WizzErr_Ticket_{ticket.TicketId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("WizzErr Boarding Pass")
                        .SemiBold().FontSize(28).FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Text($"Ticket ID: {ticket.TicketId}").FontSize(14).SemiBold();
                        col.Item().Text($"Status: {ticket.Status}").FontColor(ticket.Status == CancelledStatus ? Colors.Red.Medium : Colors.Green.Darken1);
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(10).Text("Flight Details").FontSize(16).SemiBold();
                        col.Item().Text($"Flight Number: {ticket.Flight?.FlightNr ?? "N/A"}");
                        col.Item().Text($"Date: {ticket.Flight?.Date:dd MMM yyyy HH:mm}");
                        col.Item().Text($"Route: {ticket.Flight?.Route?.Airport?.City ?? "N/A"} ({ticket.Flight?.Route?.RouteType ?? "N/A"})");
                        col.Item().Text($"Departure: {ticket.Flight?.Route?.DepartureTime:HH:mm}");
                        col.Item().Text($"Arrival: {ticket.Flight?.Route?.ArrivalTime:HH:mm}");
                        col.Item().Text($"Gate: {ticket.Flight?.Gate?.GateName ?? "N/A"}");
                        col.Item().Text($"Seat: {ticket.Seat ?? "Unassigned"}");

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(10).Text("Passenger Information").FontSize(16).SemiBold();
                        col.Item().Text($"Name: {ticket.PassengerFirstName} {ticket.PassengerLastName}");
                        col.Item().Text($"Email: {ticket.PassengerEmail}");
                        col.Item().Text($"Phone: {ticket.PassengerPhone}");

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(10).Text("Selected Add-Ons").FontSize(16).SemiBold();
                        if (ticket.SelectedAddOns != null && ticket.SelectedAddOns.Count > 0)
                        {
                            foreach (var addOn in ticket.SelectedAddOns)
                            {
                                col.Item().Text($"• {addOn.Name}");
                            }
                        }
                        else
                        {
                            col.Item().Text("No add-ons selected");
                        }

                        col.Item().PaddingTop(15).Text($"Total Price: {ticket.Price} EUR").FontSize(16).SemiBold();
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(filePath);

            return filePath;
        }
    }
}