using ClientBooking.Authentication;
using ClientBooking.Components.Generic;
using ClientBooking.Data;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Mapping;

namespace ClientBooking.Features.Home;

public class HomePageHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/get", GetHandler);
    }

    private static async Task<RazorComponentResult> 
        GetHandler(ISessionStateManager sessionStateManager, DataContext dataContext)
    {
        try
        {
            var userId = sessionStateManager.GetUserSessionId();
            
            var user = await dataContext.Users
                .Include(u => u.UserBookings)
                    .ThenInclude(ub => ub.Booking)
                    .ThenInclude(b => b.Client)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (userId == null || user == null)
            {
                return new RazorComponentResult<ErrorMessageComponent>(new { ErrorMessage = "User not found." });
            }
            
            var systemSettings = await dataContext.Settings
                .OrderByDescending(s => s.Version)
                .FirstAsync();
            
            var userProfile = user.MapToUserProfile(systemSettings);
            var weeklyStats = await GetWeeklyStats(dataContext, userId.Value);
            var todayBookings = await GetTodayBookings(dataContext, userId.Value);
            var upcomingBookings = await GetUpcomingBookings(dataContext, userId.Value);
            var weeklyHours = await GetWeeklyHours(dataContext, userId.Value);

            return new RazorComponentResult<HomePageComponent>(new 
            { 
                UserProfile = userProfile,
                WeeklyStats = weeklyStats,
                TodayBookings = todayBookings,
                UpcomingBookings = upcomingBookings,
                WeeklyHours = weeklyHours
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<ErrorMessageComponent>(new
            {
                ErrorMessage = $"Critical error occurred: {ex.Message}."
            });
        }
    }

    private static async Task<WeeklyStats> GetWeeklyStats(DataContext dataContext, int userId)
    {
        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);

        var weeklyData = await dataContext.UserBookings
            .Where(ub => ub.UserId == userId && 
                        ub.Booking.StartDateTime >= startOfWeek && 
                        ub.Booking.StartDateTime < endOfWeek)
            .Include(ub => ub.Booking)
            .ToListAsync();

        return new WeeklyStats
        {
            TotalBookings = weeklyData.Count,
            CompletedBookings = weeklyData.Count(b => b.Booking.Status == BookingStatus.Confirmed && b.Booking.EndDateTime <= endOfWeek),
            TotalHours = weeklyData.Sum(b => (b.Booking.EndDateTime - b.Booking.StartDateTime).TotalHours)
        };
    }

    private static async Task<List<BookingDto>> GetTodayBookings(DataContext dataContext, int userId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await dataContext.UserBookings
            .Where(ub => ub.UserId == userId && 
                        ub.Booking.StartDateTime >= today && 
                        ub.Booking.StartDateTime < tomorrow)
            .Include(ub => ub.Booking)
                .ThenInclude(b => b.Client)
            .OrderBy(ub => ub.Booking.StartDateTime)
            .Select(ub => new BookingDto
            {
                Id = ub.Booking.Id,
                ClientName = ub.Booking.Client.Name,
                StartDateTime = ub.Booking.StartDateTime,
                EndDateTime = ub.Booking.EndDateTime,
                Status = ub.Booking.Status,
                Notes = ub.Booking.Notes
            })
            .ToListAsync();
    }

    private static async Task<List<BookingDto>> GetUpcomingBookings(DataContext dataContext, int userId)
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var nextWeek = tomorrow.AddDays(7);

        return await dataContext.UserBookings
            .Where(ub => ub.UserId == userId && 
                        ub.Booking.StartDateTime >= tomorrow && 
                        ub.Booking.StartDateTime < nextWeek &&
                        ub.Booking.Status == BookingStatus.Scheduled)
            .Include(ub => ub.Booking)
                .ThenInclude(b => b.Client)
            .OrderBy(ub => ub.Booking.StartDateTime)
            .Take(5)
            .Select(ub => new BookingDto
            {
                Id = ub.Booking.Id,
                ClientName = ub.Booking.Client.Name,
                StartDateTime = ub.Booking.StartDateTime,
                EndDateTime = ub.Booking.EndDateTime,
                Status = ub.Booking.Status,
                Notes = ub.Booking.Notes
            })
            .ToListAsync();
    }

    private static async Task<WeeklyHours> GetWeeklyHours(DataContext dataContext, int userId)
    {
        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.Date.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);

        var dailyHours = await dataContext.UserBookings
            .Where(ub => ub.UserId == userId 
                         && ub.Booking.Status != BookingStatus.Cancelled 
                         && ub.Booking.StartDateTime >= startOfWeek && ub.Booking.StartDateTime < endOfWeek)
            .Include(ub => ub.Booking)
            .GroupBy(ub => ub.Booking.StartDateTime.Date)
            .Select(g => new DailyHours
            {
                Date = g.Key,
                HoursWorked = g.Sum(b => (b.Booking.EndDateTime - b.Booking.StartDateTime).TotalHours),
                BookingCount = g.Count()
            })
            .ToListAsync();

        return new WeeklyHours
        {
            DailyHours = dailyHours,
            TotalHours = dailyHours.Sum(d => d.HoursWorked)
        };
    }
}