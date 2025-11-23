using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Features.Bookings;
using ClientBooking.Shared.Enums;

namespace ClientBooking.Shared.Mapping;

public static class BookingMapping
{
    extension(BookingRequest bookingRequest)
    {
        public BookingRequest ToNewBookingRequest()
        {
            return new BookingRequest
            {
                Notes = bookingRequest.Notes,
                StartDateTime = bookingRequest.StartDateTime,
                EndDateTime = bookingRequest.EndDateTime,
                IsRecurring = bookingRequest.IsRecurring,
                NumberOfRecurrences = bookingRequest.NumberOfRecurrences,
                RecurrencePattern =bookingRequest.RecurrencePattern
            };
        }
    }

    extension(List<BookingRequest> bookingRequests)
    {
        public List<UserBooking> ToNewBookings(Client client, int userId, Guid? seriesId)
        {
            return bookingRequests.Select(x => new UserBooking
            {
                Booking = new Booking
                {
                    ClientId = client.Id,
                    Client = client,
                    Notes = x.Notes,
                    StartDateTime = x.StartDateTime,
                    EndDateTime = x.EndDateTime,
                    Status = BookingStatus.Scheduled,
                    IsRecurring = x.IsRecurring,
                    NumberOfRecurrences = x.NumberOfRecurrences,
                    RecurrencePattern = x.RecurrencePattern,
                    RecurrenceSeriesId =  seriesId
                },
                UserId = userId
            }).ToList();
           
        }
    }
}