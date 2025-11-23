using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Bookings.Cancel;

public class CancelBookingHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/booking/{bookingId:int}/cancel", GetHandler).RequireAuthorization();
        app.MapPost("/booking/{bookingId:int}/cancel", PostHandler).RequireAuthorization();
    }

    private static RazorComponentResult<CancelBookingComponent> GetHandler([FromRoute] int bookingId, ISessionStateManager sessionStateManager)
    {
        try
        {
            var userId = sessionStateManager.GetUserSessionId();

            return new RazorComponentResult<CancelBookingComponent>(new
            {
                userId,
                bookingId
            });
        }
        catch (Exception e)
        {
            return new RazorComponentResult<CancelBookingComponent>(new
            {
                ErrorMessage = e.Message
            });
        }
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<CancelBookingComponent>>>
        PostHandler([FromRoute] int bookingId, ISessionStateManager sessionStateManager, DataContext dataContext)
    {
        try
        {
            var userId = sessionStateManager.GetUserSessionId();
            var booking = await dataContext.Bookings
                .Include(b => b.Client)
                .Include(b => b.UserBookings)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            
            var bookingErrors = CheckBookingForErrors(booking, userId);
            
            if (!string.IsNullOrEmpty(bookingErrors))
            {
                
                return new RazorComponentResult<CancelBookingComponent>(new
                {
                    ErrorMessage = "Cannot cancel a booking you did not schedule.",
                });
            }

            booking!.RecurrenceSeriesId = null;
            booking!.Status = BookingStatus.Cancelled;
            await dataContext.SaveChangesAsync();

            return new HtmxRedirectResult("/bookings");
        }
        catch (Exception e)
        {
            return new RazorComponentResult<CancelBookingComponent>(new
            {
                ErrorMessage = e.Message
            });
        }
    }

    private static string? CheckBookingForErrors(Booking? booking, int? userId)
    {
        if (booking == null || userId == null)
        {
            return "Booking not found.";
        }
        
        if (booking.StartDateTime <= DateTime.UtcNow)
        {
            return "Cannot cancel a booking that has already begun.";
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return "This booking has already been cancelled.";
        }

        if (booking.UserBookings.All(x => x.UserId != userId))
        {
            return "Cannot cancel a booking you did not schedule.";
        }

        return null;
    }
}