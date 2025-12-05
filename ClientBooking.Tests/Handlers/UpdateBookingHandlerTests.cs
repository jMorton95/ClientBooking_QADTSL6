using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Features;
using ClientBooking.Features.Bookings;
using ClientBooking.Features.Bookings.Update;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Models;
using ClientBooking.Tests.Setup;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class UpdateBookingHandlerTests : UnitTestContext
{
    private readonly DataContext db;
    private readonly Mock<ISessionStateManager> sessionMock;
    private readonly Mock<IBookingService> bookingServiceMock;
    private readonly Mock<IValidator<BookingRequest>> validatorMock;

    public UpdateBookingHandlerTests()
    {
        db = CreateInMemoryContext();
        sessionMock = new Mock<ISessionStateManager>();
        bookingServiceMock = new Mock<IBookingService>();
        validatorMock = new Mock<IValidator<BookingRequest>>();
    }

    private async Task<(User user, Client client, Booking booking, Settings settings)> SeedEntities()
    {
        var user = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@user.com",
            HashedPassword = "",
            DoesWorkWeekends = true,
            WorkingHoursStart = new TimeSpan(9, 0, 0),
            WorkingHoursEnd = new TimeSpan(17, 0, 0),
            BreakTimeStart = new TimeSpan(12, 0, 0),
            BreakTimeEnd = new TimeSpan(13, 0, 0)
        };

        var client = new Client
        {
            Name = "Test Client",
            Description = "Test Client Description",
            Email = "Test.Client@Email.com"
        };

        var booking = new Booking
        {
            Client = client,
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11),
            Notes = "Initial notes",
            IsRecurring = false,
            UserBookings = new List<UserBooking>()
        };

        var userBooking = new UserBooking { Booking = booking, User = user };
        booking.UserBookings.Add(userBooking);

        var settings = new Settings
        {
            DefaultUserRole = RoleName.User,
            DefaultWorkingHoursStart = new TimeSpan(9, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(17, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
            Version = 1
        };

        db.Users.Add(user);
        db.Clients.Add(client);
        db.Bookings.Add(booking);
        db.Settings.Add(settings);
        await db.SaveChangesAsync();

        sessionMock.Setup(s => s.GetUserSessionId()).Returns(user.Id);

        return (user, client, booking, settings);
    }

    // ======================= GET HANDLER =======================

    [Fact]
    public async Task GetHandler_ReturnsBookingFormComponent_WhenBookingExistsAndUserHasPermission()
    {
        var (user, _, booking, _) = await SeedEntities();

        var result = await UpdateBookingHandler.GetHandler(
            booking.Id, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result);
        Assert.IsType<RazorComponentResult<BookingFormComponent>>(result);
        Assert.Equal(booking.Id, result.Parameters["BookingId"]);
        Assert.True((bool)result.Parameters["IsEditMode"]);
        Assert.NotNull(result.Parameters["BookingRequest"]);
        Assert.NotNull(result.Parameters["BookingFormData"]);
    }

    [Fact]
    public async Task GetHandler_ReturnsError_WhenUserNotInSession()
    {
        sessionMock.Setup(s => s.GetUserSessionId()).Returns((int?)null);

        var result = await UpdateBookingHandler.GetHandler(1, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result.Parameters["ErrorMessage"]);
    }

    [Fact]
    public async Task GetHandler_ReturnsError_WhenBookingNotFound()
    {
        var result = await UpdateBookingHandler.GetHandler(999, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result.Parameters["ErrorMessage"]);
    }

    [Fact]
    public async Task GetHandler_ReturnsError_WhenUserHasNoPermission()
    {
        var (_, _, booking, _) = await SeedEntities();
        sessionMock.Setup(s => s.GetUserSessionId()).Returns(999); // Different user

        var result = await UpdateBookingHandler.GetHandler(booking.Id, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result.Parameters["ErrorMessage"]);
    }

    // ======================= POST HANDLER =======================

    [Fact]
    public async Task PostHandler_ValidRequest_UpdatesBooking()
    {
        var (user, _, booking, settings) = await SeedEntities();

        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(11),
            EndDateTime = DateTime.Today.AddHours(12),
            Notes = "Updated notes"
        };

        validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        bookingServiceMock.Setup(s => s.EnforceBookingSchedulingRules(request, booking.Client, user, settings, booking.Id, booking.RecurrenceSeriesId))
            .ReturnsAsync(Result<List<BookingRequest>>.Success(new List<BookingRequest> { request }));

        var result = await UpdateBookingHandler.PostHandler(
            request, booking.Id, validatorMock.Object, db, sessionMock.Object, bookingServiceMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.IsType<HtmxRedirectResult>(result.Result);
        bookingServiceMock.Verify(s => s.UpdateBooking(booking, It.IsAny<List<BookingRequest>>(), booking.Client, user.Id), Times.Once);
    }

    [Fact]
    public async Task PostHandler_InvalidRequest_ReturnsValidationErrors()
    {
        var (_, _, booking, _) = await SeedEntities();

        var request = new BookingRequest();
        var validationResult = new ValidationResult(new[] { new ValidationFailure("StartDateTime", "Required") });

        validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(validationResult);

        var result = await UpdateBookingHandler.PostHandler(
            request, booking.Id, validatorMock.Object, db, sessionMock.Object, bookingServiceMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        var razorResult = Assert.IsType<RazorComponentResult<BookingFormComponent>>(result.Result);
        Assert.NotNull(razorResult.Parameters["ValidationErrors"]);
        bookingServiceMock.Verify(s => s.UpdateBooking(It.IsAny<Booking>(), It.IsAny<List<BookingRequest>>(), It.IsAny<Client>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PostHandler_EnforcementFailure_ReturnsErrors()
    {
        var (user, _, booking, settings) = await SeedEntities();

        var request = new BookingRequest();

        validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        bookingServiceMock.Setup(s => s.EnforceBookingSchedulingRules(request, booking.Client, user, settings, booking.Id, booking.RecurrenceSeriesId))
            .ReturnsAsync(Result<List<BookingRequest>>.ValidationFailure(new Dictionary<string, string[]> { { "StartDateTime", ["Conflict"] } }));

        var result = await UpdateBookingHandler.PostHandler(
            request, booking.Id, validatorMock.Object, db, sessionMock.Object, bookingServiceMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        var razorResult = Assert.IsType<RazorComponentResult<BookingFormComponent>>(result.Result);
        Assert.NotNull(razorResult.Parameters["ValidationErrors"] ?? razorResult.Parameters["analysedBookingRequestResult.ValidationErrors"]);
        bookingServiceMock.Verify(s => s.UpdateBooking(It.IsAny<Booking>(), It.IsAny<List<BookingRequest>>(), It.IsAny<Client>(), It.IsAny<int>()), Times.Never);
    }

    // ======================= TOGGLE RECURRING =======================

    [Fact]
    public async Task ToggleRecurringSection_ReturnsRecurringSectionData_WhenUserHasPermission()
    {
        var (user, _, booking, _) = await SeedEntities();
        var request = new BookingRequest();

        var result = await UpdateBookingHandler.ToggleRecurringSection(
            request, booking.Id, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result);
        Assert.True((bool)result.Parameters["IsEditMode"]);
        Assert.Equal("recurring", result.Parameters["Section"]);
    }

    [Fact]
    public async Task ToggleRecurringSection_ReturnsError_WhenUserNotInSession()
    {
        sessionMock.Setup(s => s.GetUserSessionId()).Returns((int?)null);

        var result = await UpdateBookingHandler.ToggleRecurringSection(new BookingRequest(), 1, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result.Parameters["ErrorMessage"]);
    }

    [Fact]
    public async Task ToggleRecurringSection_ReturnsError_WhenUserHasNoPermission()
    {
        var (_, _, booking, _) = await SeedEntities();
        sessionMock.Setup(s => s.GetUserSessionId()).Returns(999);

        var result = await UpdateBookingHandler.ToggleRecurringSection(new BookingRequest(), booking.Id, db, sessionMock.Object, Mock.Of<ILogger<UpdateBookingHandler>>());

        Assert.NotNull(result.Parameters["ErrorMessage"]);
    }
}
