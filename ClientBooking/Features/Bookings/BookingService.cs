using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Extensions;
using ClientBooking.Shared.Mapping;
using ClientBooking.Shared.Models;

namespace ClientBooking.Features.Bookings;

public interface IBookingService
{
    Task<Result<List<BookingRequest>>> EnforceBookingSchedulingRules(BookingRequest request, Client client, User user, Settings settings);
    
    Task EnsureBookingRequestHasNoOverlaps(Dictionary<string, string[]> validationErrors, BookingRequest request, Client client, User user, Settings systemSettings);

    List<BookingRequest> PlotRecurringBookingRequests(BookingRequest request);
    
    void CheckRequestIsWithinUserSchedule(Dictionary<string, string[]> validationErrors, BookingRequest request, User user, Settings systemSettings);
    
    Task CheckOverlappingUserBookings(Dictionary<string, string[]> validationErrors, BookingRequest request, User user);
    
    Task CheckOverlappingClientBookings(Dictionary<string, string[]> validationErrors, BookingRequest request, Client client);
}

public class BookingService(DataContext dataContext) : IBookingService
{
    public async Task<Result<List<BookingRequest>>> EnforceBookingSchedulingRules(BookingRequest request, Client client, User user, Settings settings)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (request is { IsRecurring: true, RecurrencePattern: > 0 })
        {
            var allRecurringRequests = PlotRecurringBookingRequests(request);
            
            foreach (var recurringRequest in allRecurringRequests)
            {
                await EnsureBookingRequestHasNoOverlaps(validationErrors, recurringRequest, client, user, settings);
            }
            
            return validationErrors.Count != 0
                ? Result<List<BookingRequest>>.ValidationFailure(validationErrors)
                : Result<List<BookingRequest>>.Success(allRecurringRequests);
        }

        await EnsureBookingRequestHasNoOverlaps(validationErrors, request, client, user, settings);
            
        return validationErrors.Count != 0
            ? Result<List<BookingRequest>>.ValidationFailure(validationErrors)
            : Result<List<BookingRequest>>.Success([request]);
    }

    public async Task EnsureBookingRequestHasNoOverlaps(Dictionary<string, string[]> validationErrors, BookingRequest request, Client client, User user,
        Settings systemSettings)
    {
        CheckRequestIsWithinUserSchedule(validationErrors, request, user, systemSettings);

        await CheckOverlappingUserBookings(validationErrors, request, user);

        await CheckOverlappingClientBookings(validationErrors, request, client);
    }

    public void CheckRequestIsWithinUserSchedule(Dictionary<string, string[]> validationErrors, BookingRequest request, User user, Settings systemSettings)
    {
        var (userWorkingHoursStart, userWorkingHoursEnd, userBreakTimeStart, userBreakTimeEnd)
            = user.GetEffectiveWorkingHours(systemSettings);
        
        if (request.StartDateTime.TimeOfDay < userWorkingHoursStart || request.EndDateTime.TimeOfDay > userWorkingHoursEnd)
        {
            validationErrors.Add("WorkingHours", ["Booking must start and end within your time range."]);
        }

        if (request.StartDateTime.TimeOfDay < userBreakTimeEnd && request.EndDateTime.TimeOfDay > userBreakTimeStart)
        {
            validationErrors.Add("BreakTime", ["Booking must not overlap your break hours."]);
        }

        if (!user.DoesWorkWeekends && 
            request is {StartDateTime.DayOfWeek: DayOfWeek.Saturday} || request.StartDateTime.DayOfWeek == DayOfWeek.Sunday)
        {
            validationErrors.Add("WeekendWork", ["Cannot schedule a booking for the weekend when you do not work weekends."]);
        }
    }

    public async Task CheckOverlappingUserBookings(Dictionary<string, string[]> validationErrors, BookingRequest request, User user)
    {
        var overlappingUserBookings = await dataContext.UserBookings
            .Include(ub => ub.Booking)
            .ThenInclude(b => b.Client)
            .Where(x => x.UserId == user.Id)
            .Where(y => y.Booking.StartDateTime < request.EndDateTime && y.Booking.EndDateTime > request.StartDateTime)
            .AsSplitQuery()
            .ToListAsync();

        if (overlappingUserBookings.Count != 0)
        {
            var overlappingUserBookingErrorMessages = overlappingUserBookings
                .Select(x => $"Conflict with your booking on {x.Booking.StartDateTime} with {x.Booking.Client.Name}.");
            
            if (!validationErrors.TryGetValue("OverlappingUserBookings", out var value))
                validationErrors["OverlappingUserBookings"] = overlappingUserBookingErrorMessages.ToArray();
            else
                validationErrors["OverlappingUserBookings"] = value.Concat(overlappingUserBookingErrorMessages).ToArray();
        }
    }

    public async Task CheckOverlappingClientBookings(Dictionary<string, string[]> validationErrors, BookingRequest request, Client client)
    {
        var overlappingClientBookings = await dataContext.UserBookings
            .Include(ub => ub.Booking)
            .Include(ub => ub.User)
            .Where(x => x.Booking.ClientId == client.Id)
            .Where(x => x.Booking.StartDateTime < request.EndDateTime && x.Booking.EndDateTime > request.StartDateTime)
            .AsSplitQuery()
            .ToListAsync();

        if (overlappingClientBookings.Count != 0)
        {
            var overlappingClientBookingErrorMessages = overlappingClientBookings
                .Select(x => $"Conflicts with {client.Name}'s booking with {x.User.FullName} at {x.Booking.StartDateTime}");
            
            if (!validationErrors.TryGetValue("OverlappingClientBookings", out var value))
                validationErrors["OverlappingClientBookings"] = overlappingClientBookingErrorMessages.ToArray();
            else
                validationErrors["OverlappingClientBookings"] = value.Concat(overlappingClientBookingErrorMessages).ToArray();
        }
    }
    
    public List<BookingRequest> PlotRecurringBookingRequests(BookingRequest request)
    {
        var requestedRecurrencePattern = request.RecurrencePattern;
        var requestedRecurrences = request.NumberOfRecurrences;
        
        List<BookingRequest> allRecurrences = [request];
        
        for (var i = 1; i < requestedRecurrences; i++)
        {
            var nextBookingRequest = request.ToNewBookingRequest();

            nextBookingRequest.StartDateTime = requestedRecurrencePattern switch
            {
                BookingRecurrencePattern.Daily   => request.StartDateTime.AddDays(i),
                BookingRecurrencePattern.Weekly  => request.StartDateTime.AddDays(7 * i),
                BookingRecurrencePattern.Monthly => request.StartDateTime.AddMonths(i),
                _ => throw new ArgumentOutOfRangeException(nameof(BookingRequest.StartDateTime),
                    "Booking recurrences cannot be assigned if 'None' is selected.")
            };

            nextBookingRequest.EndDateTime = requestedRecurrencePattern switch
            {
                BookingRecurrencePattern.Daily   => request.EndDateTime.AddDays(i),
                BookingRecurrencePattern.Weekly  => request.EndDateTime.AddDays(7 * i),
                BookingRecurrencePattern.Monthly => request.EndDateTime.AddMonths(i),
                _ => throw new ArgumentOutOfRangeException(nameof(BookingRequest.EndDateTime),
                    "Booking recurrences cannot be assigned if 'None' is selected.")
            };

            allRecurrences.Add(nextBookingRequest);
        }

        return allRecurrences;
    }
}