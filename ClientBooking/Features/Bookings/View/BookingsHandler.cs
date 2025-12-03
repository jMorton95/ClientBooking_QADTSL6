using ClientBooking.Authentication;
using ClientBooking.Data;

namespace ClientBooking.Features.Bookings.View;

public class BookingsHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/bookings/get", Handler).RequireAuthorization();
    }

    private static async Task<RazorComponentResult<BookingsComponent>> Handler(
        DataContext dataContext, 
        ISessionStateManager sessionStateManager,
        ILogger<BookingsHandler> logger)
    {
        try
        {
            var userId = sessionStateManager.GetUserSessionId();
            var user = await dataContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (userId == null || user == null)
            {
                logger.LogError("User Session or User not found when trying to load bookings.");
                return new RazorComponentResult<BookingsComponent>(new 
                { 
                    IsCriticalError = true,
                    ErrorMessage = "User not found." 
                });
            }
            
            var userBookings = await dataContext.UserBookings
                .Where(ub => ub.UserId == userId)
                .Include(ub => ub.Booking)
                .ThenInclude(b => b.Client)
                .Select(ub => ub.Booking)
                .ToListAsync();

            var upcomingBookings = userBookings
                .Where(x => x.StartDateTime > DateTime.UtcNow && x.Status != Shared.Enums.BookingStatus.Cancelled)
                .OrderBy(x => x.StartDateTime)
                .ToList();

            var pastBookings = userBookings
                .Where(x => x.StartDateTime <= DateTime.UtcNow || x.Status == Shared.Enums.BookingStatus.Cancelled)
                .OrderByDescending(x => x.StartDateTime)
                .ToList();

            return new RazorComponentResult<BookingsComponent>(new
            {
                UpcomingBookings = upcomingBookings,
                PastBookings = pastBookings,
                User = user
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while loading bookings.");
            return new RazorComponentResult<BookingsComponent>(new
            {
                IsCriticalError = true,
                ErrorMessage = ex.Message,
            });
        }
    }
}