using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Bookings;
using ClientBooking.Shared.Enums;
using ClientBooking.Tests.Setup;

namespace ClientBooking.Tests.Services;

public class BookingServiceTests : UnitTestContext
{
    private readonly BookingService bookingService;
    private readonly DataContext db;

    public BookingServiceTests()
    {
        db = CreateInMemoryContext();
        bookingService = new BookingService(db);
    }

    private Settings GetDefaultSettings() =>
        new()
        {
            DefaultWorkingHoursStart = new TimeSpan(9, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(17, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
            DefaultUserRole = RoleName.User,
            Version = 1
        };

    [Fact]
    public void CheckRequestIsWithinUserSchedule_OutsideWorkingHours_AddsError()
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@user.com",
            HashedPassword = "",
            DoesWorkWeekends = false,
            WorkingHoursStart = new TimeSpan(9, 0, 0),
            WorkingHoursEnd = new TimeSpan(17, 0, 0),
            BreakTimeStart = new TimeSpan(12, 0, 0),
            BreakTimeEnd = new TimeSpan(13, 0, 0)
        };

        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(8),
            EndDateTime = DateTime.Today.AddHours(18)
        };

        var errors = new Dictionary<string, string[]>();
        bookingService.CheckRequestIsWithinUserSchedule(errors, request, user, GetDefaultSettings());

        Assert.True(errors.ContainsKey("WorkingHours"));
    }

    [Fact]
    public void CheckRequestIsWithinUserSchedule_OverlapsBreak_AddsError()
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@user.com",
            HashedPassword = "",
            BreakTimeStart = new TimeSpan(12, 0, 0),
            BreakTimeEnd = new TimeSpan(13, 0, 0),
            WorkingHoursStart = new TimeSpan(9, 0, 0),
            WorkingHoursEnd = new TimeSpan(17, 0, 0)
        };

        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(12),
            EndDateTime = DateTime.Today.AddHours(12).AddMinutes(30)
        };

        var errors = new Dictionary<string, string[]>();
        bookingService.CheckRequestIsWithinUserSchedule(errors, request, user, GetDefaultSettings());

        Assert.True(errors.ContainsKey("BreakTime"));
    }

    [Fact]
    public void CheckRequestIsWithinUserSchedule_WeekendNotAllowed_AddsError()
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@user.com",
            HashedPassword = "",
            DoesWorkWeekends = false,
            WorkingHoursStart = new TimeSpan(9, 0, 0),
            WorkingHoursEnd = new TimeSpan(17, 0, 0),
            BreakTimeStart = new TimeSpan(12, 0, 0),
            BreakTimeEnd = new TimeSpan(13, 0, 0)
        };

        var saturday = DateTime.Today.AddDays(DayOfWeek.Saturday - DateTime.Today.DayOfWeek);
        var request = new BookingRequest
        {
            StartDateTime = saturday,
            EndDateTime = saturday.AddHours(1)
        };

        var errors = new Dictionary<string, string[]>();
        bookingService.CheckRequestIsWithinUserSchedule(errors, request, user, GetDefaultSettings());

        Assert.True(errors.ContainsKey("WeekendWork"));
    }

    [Fact]
    public void PlotRecurringBookingRequests_Weekly_ReturnsCorrectDates()
    {
        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11),
            IsRecurring = true,
            NumberOfRecurrences = 3,
            RecurrencePattern = BookingRecurrencePattern.Weekly
        };

        var recurrences = bookingService.PlotRecurringBookingRequests(request);

        Assert.Equal(3, recurrences.Count);
        Assert.Equal(request.StartDateTime, recurrences[0].StartDateTime);
        Assert.Equal(request.StartDateTime.AddDays(7), recurrences[1].StartDateTime);
        Assert.Equal(request.StartDateTime.AddDays(14), recurrences[2].StartDateTime);
    }

    [Fact]
    public void PlotRecurringBookingRequests_Monthly_ReturnsCorrectDates()
    {
        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11),
            IsRecurring = true,
            NumberOfRecurrences = 2,
            RecurrencePattern = BookingRecurrencePattern.Monthly
        };

        var recurrences = bookingService.PlotRecurringBookingRequests(request);

        Assert.Equal(2, recurrences.Count);
        Assert.Equal(request.StartDateTime.AddMonths(1), recurrences[1].StartDateTime);
    }

    [Fact]
    public async Task EnforceBookingSchedulingRules_NonRecurring_ReturnsSuccess()
    {
        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11),
            IsRecurring = false
        };

        var client = new Client
        {
         
            Name = "Test Client",
            Description = "Test Client Description",
            Email = "Test@Client.com",
        };
        db.Clients.Add(client);

        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@user.com",
            HashedPassword = "",
            DoesWorkWeekends = true,
            WorkingHoursStart = new TimeSpan(16, 0, 0),
            WorkingHoursEnd = new TimeSpan(17, 0, 0),
            BreakTimeStart = new TimeSpan(12, 0, 0),
            BreakTimeEnd = new TimeSpan(13, 0, 0)
        };
        db.Users.Add(user);

        var settings = GetDefaultSettings();

        await db.SaveChangesAsync();

        var result = await bookingService.EnforceBookingSchedulingRules(request, client, user, settings);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }
}
