using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Features;
using ClientBooking.Features.Bookings;
using ClientBooking.Features.Bookings.Create;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Models;
using ClientBooking.Tests.Setup;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class CreateBookingHandlerTests : UnitTestContext
{
    private readonly DataContext db;
    private readonly Mock<ISessionStateManager> sessionMock;
    private readonly Mock<IBookingService> bookingServiceMock;
    private readonly Mock<IValidator<BookingRequest>> validatorMock;

    public CreateBookingHandlerTests()
    {
        db = CreateInMemoryContext();
        sessionMock = new Mock<ISessionStateManager>();
        bookingServiceMock = new Mock<IBookingService>();
        validatorMock = new Mock<IValidator<BookingRequest>>();
    }

    private async Task<(User user, Client client, Settings settings)> SeedEntities()
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
        var client = new Client { Name = "Test Client", Description = "Test Client Description", Email = "Test.Client@Email.com"};
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
        db.Settings.Add(settings);
        await db.SaveChangesAsync();

        sessionMock.Setup(s => s.GetUserSessionId()).Returns(user.Id);

        return (user, client, settings);
    }

    [Fact]
    public async Task GetHandler_ReturnsBookingFormComponent()
    {
        var (user, client, _) = await SeedEntities();

        var result = await CreateBookingHandler.GetHandler(
            client.Id, db, sessionMock.Object, Mock.Of<ILogger<CreateBookingHandler>>());

        Assert.NotNull(result);
        Assert.IsType<RazorComponentResult<BookingFormComponent>>(result);

        var parameters = result.Parameters;
        Assert.NotNull(parameters["bookingRequest"]);
        Assert.NotNull(parameters["BookingFormData"]);
    }

    [Fact]
    public async Task PostHandler_ValidRequest_CreatesBooking()
    {
        var (user, client, settings) = await SeedEntities();

        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11),
            IsRecurring = false
        };

        validatorMock.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new ValidationResult());

        bookingServiceMock.Setup(s => s.EnforceBookingSchedulingRules(request, client, user, settings, null))
            .ReturnsAsync(Result<List<BookingRequest>>.Success(new List<BookingRequest> { request }));

        var result = await CreateBookingHandler.PostHandler(
            request, client.Id, validatorMock.Object, db, sessionMock.Object,
            bookingServiceMock.Object, Mock.Of<ILogger<CreateBookingHandler>>());

        Assert.NotNull(result);
        var redirect = result.Result as HtmxRedirectResult;
        Assert.NotNull(redirect);
        Assert.Equal("/", redirect.Url);

        bookingServiceMock.Verify(s => s.CreateBooking(It.IsAny<List<BookingRequest>>(), client, user.Id), Times.Once);
    }

    [Fact]
    public async Task PostHandler_InvalidRequest_ReturnsValidationErrors()
    {
        var (user, client, settings) = await SeedEntities();

        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11),
        };

        var validationResult = new ValidationResult(new[] { new ValidationFailure("StartDateTime", "Required") });

        validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(validationResult);

        var result = await CreateBookingHandler.PostHandler(
            request, client.Id, validatorMock.Object, db, sessionMock.Object,
            bookingServiceMock.Object, Mock.Of<ILogger<CreateBookingHandler>>());

        Assert.NotNull(result);
        var razorResult = result.Result as RazorComponentResult<BookingFormComponent>;
        Assert.NotNull(razorResult);

        var validationErrors = razorResult.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(validationErrors);
        Assert.True(validationErrors.ContainsKey("StartDateTime"));
        Assert.Contains("Required", validationErrors["StartDateTime"]);

        bookingServiceMock.Verify(s => s.CreateBooking(It.IsAny<List<BookingRequest>>(), client, user.Id), Times.Never);
    }

    [Fact]
    public async Task ToggleRecurringSection_ReturnsRecurringSectionData()
    {
        var (user, client, _) = await SeedEntities();

        var request = new BookingRequest
        {
            StartDateTime = DateTime.Today.AddHours(10),
            EndDateTime = DateTime.Today.AddHours(11)
        };

        var result = await CreateBookingHandler.ToggleRecurringSection(
            request, client.Id, db, sessionMock.Object,
            Mock.Of<ILogger<CreateBookingHandler>>());

        Assert.NotNull(result);
        var razorResult = result;
        Assert.NotNull(razorResult);

        Assert.Equal("recurring", razorResult.Parameters["Section"]);
        Assert.Equal(request, razorResult.Parameters["bookingRequest"]);
    }
}
