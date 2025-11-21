using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientBooking.Features.Bookings.Create;

public class CreateBookingHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/booking/create/get", GetHandler).RequireAuthorization();
        app.MapPost("/booking/create", PostHandler).RequireAuthorization();
    }

    private static async Task<RazorComponentResult<CreateBookingComponent>> GetHandler(
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            if (userId == null)
            {
                return new RazorComponentResult<CreateBookingComponent>(new 
                { 
                    ErrorMessage = "User not found." 
                });
            }

            // Get user with their schedule
            var user = await dataContext.Users.FindAsync(userId);
            if (user == null)
            {
                return new RazorComponentResult<CreateBookingComponent>(new 
                { 
                    ErrorMessage = "User not found." 
                });
            }

            // Get system settings
            var systemSettings = await dataContext.Settings
                .OrderByDescending(s => s.Version)
                .FirstAsync();

            // Determine user's working hours and break times
            var workingHoursStart = user.UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursStart : user.WorkingHoursStart ?? systemSettings.DefaultWorkingHoursStart;
            var workingHoursEnd = user.UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursEnd : user.WorkingHoursEnd ?? systemSettings.DefaultWorkingHoursEnd;
            var breakTimeStart = user.UseSystemBreakTime ? systemSettings.DefaultBreakTimeStart : user.BreakTimeStart ?? systemSettings.DefaultBreakTimeStart;
            var breakTimeEnd = user.UseSystemBreakTime ? systemSettings.DefaultBreakTimeEnd : user.BreakTimeEnd ?? systemSettings.DefaultBreakTimeEnd;

            // Get available clients
            var clients = await dataContext.Clients
                .Where(c => c.Bookings.All(b => b.Status != BookingStatus.Cancelled)) // Only active clients
                .Select(c => new ClientResponse 
                { 
                    Id = c.Id, 
                    Name = c.Name, 
                    Email = c.Email 
                })
                .ToListAsync();

            var bookingFormData = new BookingFormData
            {
                Clients = clients,
                WorkingHoursStart = workingHoursStart,
                WorkingHoursEnd = workingHoursEnd,
                BreakTimeStart = breakTimeStart,
                BreakTimeEnd = breakTimeEnd,
                DoesWorkWeekends = user.DoesWorkWeekends
            };

            var createBookingRequest = new BookingRequest
            {
                // Set default start time to next available slot
                StartDateTime = DateTime.Today.AddDays(1).Add(workingHoursStart),
                EndDateTime = DateTime.Today.AddDays(1).Add(workingHoursStart.Add(TimeSpan.FromHours(1))) // Default 1 hour booking
            };

            return new RazorComponentResult<CreateBookingComponent>(new 
            { 
                CreateBookingRequest = createBookingRequest,
                BookingFormData = bookingFormData
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<CreateBookingComponent>(new
            {
                ErrorMessage = "An error occurred while loading the booking form."
            });
        }
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<CreateBookingComponent>>> PostHandler(
        [FromForm] BookingRequest bookingRequest,
        [FromServices] IValidator<BookingRequest> validator,
        [FromServices] DataContext dataContext,
        [FromServices] ISessionStateManager sessionManager)
    {
        try
        {
            var userId = sessionManager.GetUserSessionId();
            if (userId == null)
            {
                return new RazorComponentResult<CreateBookingComponent>(new 
                { 
                    ErrorMessage = "User not found." 
                });
            }

            // Basic validation
            var validationResult = await validator.ValidateAsync(bookingRequest);
            if (!validationResult.IsValid)
            {
                return await ReturnFormWithErrors(bookingRequest, dataContext, sessionManager, validationResult.ToDictionary());
            }

            // Complex business rule validation
            var validationErrors = await ValidateBookingRules(bookingRequest, userId.Value, dataContext);
            if (validationErrors.Any())
            {
                return await ReturnFormWithErrors(bookingRequest, dataContext, sessionManager, validationErrors);
            }

            // Create the booking
            var booking = new Booking
            {
                ClientId = createBookingRequest.ClientId,
                Notes = createBookingRequest.Notes,
                StartDateTime = createBookingRequest.StartDateTime,
                EndDateTime = createBookingRequest.EndDateTime,
                Status = BookingStatus.Scheduled,
                IsRecurring = createBookingRequest.IsRecurring,
                NumberOfRecurrences = createBookingRequest.IsRecurring ? createBookingRequest.NumberOfRecurrences : 0,
                RecurrencePattern = createBookingRequest.IsRecurring ? createBookingRequest.RecurrencePattern : BookingRecurrencePattern.None
            };

            // Add user booking relationship
            var userBooking = new UserBooking
            {
                UserId = userId.Value,
                Booking = booking
            };

            await dataContext.Bookings.AddAsync(booking);
            await dataContext.UserBookings.AddAsync(userBooking);
            await dataContext.SaveChangesAsync();

            return new HtmxRedirectResult("/bookings");
        }
        catch (Exception ex)
        {
            return await ReturnFormWithErrors(createBookingRequest, await GetFormData(dataContext, sessionManager), 
                "An error occurred while creating the booking.");
        }
    }

    private static async Task<Dictionary<string, string[]>> ValidateBookingRules(
        CreateBookingRequest request, int userId, DataContext dataContext)
    {
        var errors = new Dictionary<string, string[]>();

        // Get user schedule
        var user = await dataContext.Users.FindAsync(userId);
        var systemSettings = await dataContext.Settings.OrderByDescending(s => s.Version).FirstAsync();
        
        var workingHoursStart = user.UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursStart : user.WorkingHoursStart ?? systemSettings.DefaultWorkingHoursStart;
        var workingHoursEnd = user.UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursEnd : user.WorkingHoursEnd ?? systemSettings.DefaultWorkingHoursEnd;
        var breakTimeStart = user.UseSystemBreakTime ? systemSettings.DefaultBreakTimeStart : user.BreakTimeStart ?? systemSettings.DefaultBreakTimeStart;
        var breakTimeEnd = user.UseSystemBreakTime ? systemSettings.DefaultBreakTimeEnd : user.BreakTimeEnd ?? systemSettings.DefaultBreakTimeEnd;

        // 1. Check if within working hours
        var startTime = request.StartDateTime.TimeOfDay;
        var endTime = request.EndDateTime.TimeOfDay;
        
        if (startTime < workingHoursStart || endTime > workingHoursEnd)
        {
            errors.Add("StartDateTime", new[] { $"Booking must be within working hours ({workingHoursStart:hh\\:mm} - {workingHoursEnd:hh\\:mm})" });
        }

        // 2. Check if overlaps with break time
        if ((startTime < breakTimeEnd && endTime > breakTimeStart) ||
            (startTime >= breakTimeStart && startTime < breakTimeEnd) ||
            (endTime > breakTimeStart && endTime <= breakTimeEnd))
        {
            errors.Add("StartDateTime", new[] { $"Booking cannot overlap with break time ({breakTimeStart:hh\\:mm} - {breakTimeEnd:hh\\:mm})" });
        }

        // 3. Check if on weekend when user doesn't work weekends
        if (!user.DoesWorkWeekends && (request.StartDateTime.DayOfWeek == DayOfWeek.Saturday || request.StartDateTime.DayOfWeek == DayOfWeek.Sunday))
        {
            errors.Add("StartDateTime", new[] { "Booking cannot be scheduled on weekends as you don't work weekends" });
        }

        // 4. Check for user booking overlaps
        var userOverlap = await dataContext.UserBookings
            .Include(ub => ub.Booking)
            .Where(ub => ub.UserId == userId)
            .AnyAsync(ub => ub.Booking.Status != BookingStatus.Cancelled &&
                           ub.Booking.StartDateTime < request.EndDateTime &&
                           ub.Booking.EndDateTime > request.StartDateTime);

        if (userOverlap)
        {
            errors.Add("StartDateTime", new[] { "You already have a booking scheduled during this time" });
        }

        // 5. Check for client booking overlaps
        var clientOverlap = await dataContext.Bookings
            .AnyAsync(b => b.ClientId == request.ClientId &&
                          b.Status != BookingStatus.Cancelled &&
                          b.StartDateTime < request.EndDateTime &&
                          b.EndDateTime > request.StartDateTime);

        if (clientOverlap)
        {
            errors.Add("ClientId", new[] { "This client already has a booking scheduled during this time" });
        }

        // 6. Validate end time is after start time
        if (request.EndDateTime <= request.StartDateTime)
        {
            errors.Add("EndDateTime", new[] { "End time must be after start time" });
        }

        return errors;
    }

    private static async Task<RazorComponentResult<CreateBookingComponent>> ReturnFormWithErrors(
        CreateBookingRequest request, DataContext dataContext, ISessionStateManager sessionManager, 
        Dictionary<string, string[]> validationErrors)
    {
        var formData = await GetFormData(dataContext, sessionManager);
        return new RazorComponentResult<CreateBookingComponent>(new
        {
            CreateBookingRequest = request,
            BookingFormData = formData,
            ValidationErrors = validationErrors
        });
    }

    private static async Task<RazorComponentResult<CreateBookingComponent>> ReturnFormWithErrors(
        CreateBookingRequest request, BookingFormData formData, string errorMessage)
    {
        return new RazorComponentResult<CreateBookingComponent>(new
        {
            CreateBookingRequest = request,
            BookingFormData = formData,
            ErrorMessage = errorMessage
        });
    }

    private static async Task<BookingFormData> GetFormData(DataContext dataContext, ISessionStateManager sessionManager)
    {
        var userId = sessionManager.GetUserSessionId();
        var user = await dataContext.Users.FindAsync(userId);
        var systemSettings = await dataContext.Settings.OrderByDescending(s => s.Version).FirstAsync();

        var workingHoursStart = user.UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursStart : user.WorkingHoursStart ?? systemSettings.DefaultWorkingHoursStart;
        var workingHoursEnd = user.UseSystemWorkingHours ? systemSettings.DefaultWorkingHoursEnd : user.WorkingHoursEnd ?? systemSettings.DefaultWorkingHoursEnd;
        var breakTimeStart = user.UseSystemBreakTime ? systemSettings.DefaultBreakTimeStart : user.BreakTimeStart ?? systemSettings.DefaultBreakTimeStart;
        var breakTimeEnd = user.UseSystemBreakTime ? systemSettings.DefaultBreakTimeEnd : user.BreakTimeEnd ?? systemSettings.DefaultBreakTimeEnd;

        var clients = await dataContext.Clients
            .Where(c => c.Bookings.All(b => b.Status != BookingStatus.Cancelled))
            .Select(c => new ClientOption { Id = c.Id, Name = c.Name, Email = c.Email })
            .ToListAsync();

        return new BookingFormData
        {
            Clients = clients,
            WorkingHoursStart = workingHoursStart,
            WorkingHoursEnd = workingHoursEnd,
            BreakTimeStart = breakTimeStart,
            BreakTimeEnd = breakTimeEnd,
            DoesWorkWeekends = user.DoesWorkWeekends
        };
    }
}